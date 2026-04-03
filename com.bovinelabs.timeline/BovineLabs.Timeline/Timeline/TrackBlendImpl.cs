// <copyright file="TrackBlendImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using BovineLabs.Timeline.Data;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Helper that schedules clip blending jobs for a specific animated component type.
    /// </summary>
    /// <typeparam name="T">The value type being blended.</typeparam>
    /// <typeparam name="TC">The animated component type.</typeparam>
    public unsafe struct TrackBlendImpl<T, TC>
        where T : unmanaged
        where TC : unmanaged, IAnimatedComponent<T>
    {
        private NativeParallelHashMap<Entity, MixData<T>> blendResults;

        private EntityQuery unblendedQuery;
        private EntityQuery blendedQuery;

        private ComponentTypeHandle<TC> animatedHandle;
        private ComponentTypeHandle<TrackBinding> trackBindingHandle;
        private ComponentTypeHandle<ClipWeight> clipWeightHandle;

        /// <summary>
        /// Initializes internal queries and state.
        /// </summary>
        /// <param name="state">The system state.</param>
        public void OnCreate(ref SystemState state)
        {
            this.blendResults = new NativeParallelHashMap<Entity, MixData<T>>(64, Allocator.Persistent);

            this.unblendedQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<TC>()
                .WithAll<TimelineActive, TrackBinding>()
                .WithAll<ClipActive>()
                .WithNone<ClipWeight>()
                .Build(ref state);

            this.blendedQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<TC>()
                .WithAll<TimelineActive, TrackBinding, ClipWeight>()
                .WithAll<ClipActive>()
                .Build(ref state);

            this.animatedHandle = state.GetComponentTypeHandle<TC>();
            this.trackBindingHandle = state.GetComponentTypeHandle<TrackBinding>(true);
            this.clipWeightHandle = state.GetComponentTypeHandle<ClipWeight>(true);
        }

        /// <summary>
        /// Disposes of internal allocations.
        /// </summary>
        /// <param name="state">The system state.</param>
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
            this.clipWeightHandle.Update(ref state);

            resizeJob.BlendData = this.blendResults;
            resizeJob.UnblendedCount = this.unblendedQuery.CalculateEntityCountWithoutFiltering(); // TODO use filtering?
            resizeJob.BlendedCount = this.blendedQuery.CalculateEntityCountWithoutFiltering();

            animateUnblendedJob.BlendData = this.blendResults.AsParallelWriter();
            animateUnblendedJob.AnimatedHandle = this.animatedHandle;
            animateUnblendedJob.TrackBindingHandle = this.trackBindingHandle;

            accumulateWeightedAnimationJob.BlendData = this.blendResults;
            accumulateWeightedAnimationJob.AnimatedHandle = this.animatedHandle;
            accumulateWeightedAnimationJob.TrackBindingHandle = this.trackBindingHandle;
            accumulateWeightedAnimationJob.ClipWeightHandle = this.clipWeightHandle;

            state.Dependency = resizeJob.Schedule(state.Dependency);
            state.Dependency = animateUnblendedJob.ScheduleParallel(this.unblendedQuery, state.Dependency);
            state.Dependency = accumulateWeightedAnimationJob.Schedule(this.blendedQuery, state.Dependency);

            return this.blendResults.AsReadOnly();
        }

        /// <summary>
        /// Job that resizes and clears the blend data map.
        /// </summary>
        [BurstCompile]
        public struct ResizeJob : IJob
        {
            /// <summary>The map used to store blend results.</summary>
            public NativeParallelHashMap<Entity, MixData<T>> BlendData;

            /// <summary>The number of unblended entities.</summary>
            public int UnblendedCount;

            /// <summary>The number of blended entities.</summary>
            public int BlendedCount;

            /// <inheritdoc />
            public void Execute()
            {
                this.BlendData.Clear();
                if (this.BlendData.Capacity < this.UnblendedCount + this.BlendedCount)
                {
                    this.BlendData.Capacity = this.UnblendedCount + this.BlendedCount;
                }
            }
        }

        /// <summary>
        /// Job that writes unblended values for clips without weights.
        /// </summary>
        [BurstCompile]
        public struct AnimateUnblendedJob : IJobChunk
        {
            /// <summary>The blend data map to update.</summary>
            public NativeParallelHashMap<Entity, MixData<T>>.ParallelWriter BlendData;

            /// <summary>The animated component handle.</summary>
            public ComponentTypeHandle<TC> AnimatedHandle;

            /// <summary>The track binding handle.</summary>
            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (TC*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    JobHelpers.AnimateUnblend(trackBinding, ref animated, this.BlendData);
                }
            }
        }

        /// <summary>
        /// Job that accumulates weighted clip values.
        /// </summary>
        [BurstCompile]
        public struct AccumulateWeightedAnimationJob : IJobChunk
        {
            /// <summary>The blend data map to update.</summary>
            public NativeParallelHashMap<Entity, MixData<T>> BlendData;

            /// <summary>The animated component handle.</summary>
            public ComponentTypeHandle<TC> AnimatedHandle;

            /// <summary>The track binding handle.</summary>
            [ReadOnly]
            public ComponentTypeHandle<TrackBinding> TrackBindingHandle;

            /// <summary>The clip weight handle.</summary>
            [ReadOnly]
            public ComponentTypeHandle<ClipWeight> ClipWeightHandle;

            public void Execute(in ArchetypeChunk chunk, int chunkIndexInQuery, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var animateds = (TC*)chunk.GetRequiredComponentDataPtrRW(ref this.AnimatedHandle);
                var trackBindings = (TrackBinding*)chunk.GetRequiredComponentDataPtrRO(ref this.TrackBindingHandle);
                var clipWeights = (ClipWeight*)chunk.GetRequiredComponentDataPtrRO(ref this.ClipWeightHandle);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var entityIndexInChunk))
                {
                    ref var animated = ref animateds[entityIndexInChunk];
                    ref readonly var trackBinding = ref trackBindings[entityIndexInChunk];
                    ref readonly var clipWeight = ref clipWeights[entityIndexInChunk];

                    JobHelpers.AccumulateWeighted(trackBinding, ref animated, clipWeight, this.BlendData);
                }
            }
        }
    }
}
