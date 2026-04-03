// <copyright file="TrackBlendImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Timeline.Data;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public unsafe struct TrackBlendImpl<T, TC>
        where T : unmanaged
        where TC : unmanaged, IAnimatedComponent<T>
    {
        private NativeParallelHashMap<Entity, MixData<T>> blendResults;

        private EntityQuery unblendedQuery;
        private EntityQuery blendedQuery;

        private ComponentTypeHandle<TC> animatedHandle;
        private ComponentTypeHandle<TrackBinding> trackBindingHandle;
        private ComponentTypeHandle<LocalTime> localTimeHandle;
        private ComponentTypeHandle<ClipWeight> clipWeightHandle;

        public void OnCreate(ref SystemState state)
        {
            this.blendResults = new NativeParallelHashMap<Entity, MixData<T>>(64, Allocator.Persistent);

            this.unblendedQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<TC>()
                .WithAll<TimelineActive, TrackBinding, LocalTime>()
                .WithAll<ClipActive>()
                .WithNone<ClipWeight>()
                .Build(ref state);

            this.blendedQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<TC>()
                .WithAll<TimelineActive, TrackBinding, LocalTime, ClipWeight>()
                .WithAll<ClipActive>()
                .Build(ref state);

            this.animatedHandle = state.GetComponentTypeHandle<TC>();
            this.trackBindingHandle = state.GetComponentTypeHandle<TrackBinding>(true);
            this.localTimeHandle = state.GetComponentTypeHandle<LocalTime>(true);
            this.clipWeightHandle = state.GetComponentTypeHandle<ClipWeight>(true);
        }

        public void OnDestroy(ref SystemState state)
        {
            this.blendResults.Dispose();
        }

        /// <summary>
        /// Schedule the jobs for blending. This should be done after any clip updating jobs and before the final writing job.
        /// </summary>
        /// <remarks> The job parameters should be ignored, it's just a sneaky trick to allow bursted generic jobs with registration. </remarks>
        /// <param name="state"> The system state. </param>
        /// <param name="resizeJob"> Ignore resizeJob. </param>
        /// <param name="animateUnblendedJob"> Ignore animateUnblendedJob. </param>
        /// <param name="accumulateWeightedAnimationJob"> Ignore accumulateWeightedAnimationJob. </param>
        /// <returns> The hashmap of the blended results. See samples on how to use this to write results. </returns>
        public NativeParallelHashMap<Entity, MixData<T>>.ReadOnly Update(
            ref SystemState state, ResizeJob resizeJob = default, AnimateUnblendedJob animateUnblendedJob = default,
            AccumulateWeightedAnimationJob accumulateWeightedAnimationJob = default)
        {
            this.animatedHandle.Update(ref state);
            this.trackBindingHandle.Update(ref state);
            this.localTimeHandle.Update(ref state);
            this.clipWeightHandle.Update(ref state);

            resizeJob.BlendData = this.blendResults;
            resizeJob.UnblendedCount = this.unblendedQuery.CalculateEntityCountWithoutFiltering(); // TODO use filtering?
            resizeJob.BlendedCount = this.blendedQuery.CalculateEntityCountWithoutFiltering();

            animateUnblendedJob.BlendData = this.blendResults.AsParallelWriter();
            animateUnblendedJob.AnimatedHandle = this.animatedHandle;
            animateUnblendedJob.TrackBindingHandle = this.trackBindingHandle;
            animateUnblendedJob.LocalTimeHandle = this.localTimeHandle;

            accumulateWeightedAnimationJob.BlendData = this.blendResults;
            accumulateWeightedAnimationJob.AnimatedHandle = this.animatedHandle;
            accumulateWeightedAnimationJob.TrackBindingHandle = this.trackBindingHandle;
            accumulateWeightedAnimationJob.LocalTimeHandle = this.localTimeHandle;
            accumulateWeightedAnimationJob.ClipWeightHandle = this.clipWeightHandle;

            state.Dependency = resizeJob.Schedule(state.Dependency);
            state.Dependency = animateUnblendedJob.ScheduleParallel(this.unblendedQuery, state.Dependency);
            state.Dependency = accumulateWeightedAnimationJob.Schedule(this.blendedQuery, state.Dependency);

            return this.blendResults.AsReadOnly();
        }

        [BurstCompile]
        public struct ResizeJob : IJob
        {
            public NativeParallelHashMap<Entity, MixData<T>> BlendData;

            public int UnblendedCount;
            public int BlendedCount;

            public void Execute()
            {
                this.BlendData.Clear();
                if (this.BlendData.Capacity < this.UnblendedCount + this.BlendedCount)
                {
                    this.BlendData.Capacity = this.UnblendedCount + this.BlendedCount;
                }
            }
        }

        [BurstCompile]
        public struct AnimateUnblendedJob : IJobChunk
        {
            public NativeParallelHashMap<Entity, MixData<T>>.ParallelWriter BlendData;

            public ComponentTypeHandle<TC> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (TC*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];
                    JobHelpers.AnimateUnblend<T, TC>(trackBinding, ref animated, this.BlendData);
                }
            }
        }

        [BurstCompile]
        public struct AccumulateWeightedAnimationJob : IJobChunk
        {
            public NativeParallelHashMap<Entity, MixData<T>> BlendData;

            public ComponentTypeHandle<TC> AnimatedHandle;

            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalTime> LocalTimeHandle;

            [ReadOnly]
            public ComponentTypeHandle<ClipWeight> ClipWeightHandle;

            [CompilerGenerated]
            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (TC*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var localTimes = (LocalTime*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalTimeHandle);
                var clipWeights = (ClipWeight*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipWeightHandle);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var localTime = ref localTimes[entityIndexInChunk];
                    ref readonly var clipWeight = ref clipWeights[entityIndexInChunk];

                    JobHelpers.AccumulateWeighted<T, TC>(trackBinding, ref animated, clipWeight, this.BlendData);
                }
            }
        }
    }
}
