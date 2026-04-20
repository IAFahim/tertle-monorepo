// <copyright file="AudioSourceAmbiancePoolSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary> Calculates priority for pooled audio sources based on distance from camera and then syncs their transforms </summary>
    [UpdateInGroup(typeof(BridgeSimulationSystemGroup))]
    public partial struct AudioSourceAmbiancePoolSystem : ISystem
    {
        private NativeHashMap<Entity, int> active;
        private EntityQuery listenerQuery;
        private NativeReference<float3> listenerPosition;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.active = new NativeHashMap<Entity, int>(0, Allocator.Persistent);
            this.listenerQuery = SystemAPI.QueryBuilder().WithAll<CameraMain, LocalTransform>().Build();
            this.listenerPosition = new NativeReference<float3>(Allocator.Persistent);

            state.RequireForUpdate(this.listenerQuery);

            state.RequireForUpdate<AudioSourcePool>();
            state.AddDependency<AudioSourcePool>(); // add write
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
            this.active.Dispose();
            this.listenerPosition.Dispose();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pool = SystemAPI.GetSingleton<AudioSourcePool>();

            this.ReturnDisabledAndTrackActive(ref state, pool);
            this.ActivateAudio(ref state, pool);
        }

        private void ReturnDisabledAndTrackActive(ref SystemState state, AudioSourcePool pool)
        {
            var returnDisabledAudioQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<AudioSourceIndex>()
                .WithAll<AudioSourceEnabledPrevious>()
                .WithNone<AudioSourceOneShot>()
                .WithNone<AudioSourceMusic>()
                .WithDisabled<AudioSourceEnabled>()
                .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
                .Build();

            var returnDisabledDependency = new ReturnDisabledAudioJob
            {
                Pool = pool.LoopedPool,
                StartPoolIndex = pool.LoopedStartIndex,
                AudioSourceIndexHandle = SystemAPI.GetComponentTypeHandle<AudioSourceIndex>(),
            }.Schedule(returnDisabledAudioQuery, state.Dependency);
            var clearActiveDependency = new ClearNativeHashMapJob<Entity, int> { HashMap = this.active }.Schedule(state.Dependency);

            state.Dependency = JobHandle.CombineDependencies(clearActiveDependency, returnDisabledDependency);
            state.Dependency = new ActiveAudioJob { Active = this.active, StartPoolIndex = pool.LoopedStartIndex }.Schedule(state.Dependency);
        }

        private void ActivateAudio(ref SystemState state, AudioSourcePool pool)
        {
            var audioSourceQuery = SystemAPI.QueryBuilder()
                .WithAll<AudioSourceData, AudioSourceDataExtended, AudioSourceAudibleRange, AudioSourceEnabled, LocalToWorld>()
                .WithNone<AudioSourceOneShot>()
                .WithNone<AudioSourceMusic>()
                .WithPresent<AudioSourceIndex>()
                .Build();

            var activeEntities = audioSourceQuery.ToEntityListAsync(state.WorldUpdateAllocator, out var dependency1);
            var activeLocalToWorlds = audioSourceQuery.ToComponentDataListAsync<LocalToWorld>(state.WorldUpdateAllocator, out var dependency2);
            var activeAudibleRanges = audioSourceQuery.ToComponentDataListAsync<AudioSourceAudibleRange>(state.WorldUpdateAllocator, out var dependency3);
            var activeAudioSourceDataExtended = audioSourceQuery.ToComponentDataListAsync<AudioSourceDataExtended>(state.WorldUpdateAllocator, out var dependency4);

            var combined = JobHandle.CombineDependencies(state.Dependency, dependency1, dependency2);
            state.Dependency = JobHandle.CombineDependencies(combined, dependency3, dependency4);

            var closests = CollectionHelper.CreateNativeArray<Entity>(pool.LoopedPool.Length, state.WorldUpdateAllocator);

            state.Dependency = new GetListenerPositionJob { ListenerPosition = this.listenerPosition }.Schedule(state.Dependency);

            state.Dependency = new AudioSourcePrioritySortJob
            {
                Entities = activeEntities.AsDeferredJobArray(),
                LocalToWorlds = activeLocalToWorlds.AsDeferredJobArray(),
                AudibleRanges = activeAudibleRanges.AsDeferredJobArray(),
                AudioSourceDataExtended = activeAudioSourceDataExtended.AsDeferredJobArray(),
                Closests = closests,
                ListenerPosition = this.listenerPosition,
            }.Schedule(state.Dependency);

            state.Dependency = new AssignPoolIndicesJob
            {
                Active = this.active,
                Closest = closests,
                AudioSourceIndices = SystemAPI.GetComponentLookup<AudioSourceIndex>(),
                Pool = pool.LoopedPool,
                StartPoolIndex = pool.LoopedStartIndex,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private unsafe struct ReturnDisabledAudioJob : IJobChunk
        {
            public TrackedIndexPool Pool;
            public int StartPoolIndex;

            public ComponentTypeHandle<AudioSourceIndex> AudioSourceIndexHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var indexes = (AudioSourceIndex*)chunk.GetRequiredComponentDataPtrRW(ref this.AudioSourceIndexHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    this.Pool.Return(indexes[i].PoolIndex - this.StartPoolIndex);
                    indexes[i].PoolIndex = -1;
                    chunk.SetComponentEnabled(ref this.AudioSourceIndexHandle, i, false);
                }
            }
        }

        [BurstCompile]
        [WithNone(typeof(AudioSourceOneShot), typeof(AudioSourceMusic))]
        private partial struct ActiveAudioJob : IJobEntity
        {
            public NativeHashMap<Entity, int> Active;
            public int StartPoolIndex;

            private void Execute(Entity entity, in AudioSourceIndex audioSourceIndex)
            {
                this.Active.Add(entity, audioSourceIndex.PoolIndex - this.StartPoolIndex);
            }
        }

        [BurstCompile]
        private struct AssignPoolIndicesJob : IJob
        {
            public NativeHashMap<Entity, int> Active;
            public NativeArray<Entity> Closest;
            public ComponentLookup<AudioSourceIndex> AudioSourceIndices;
            public TrackedIndexPool Pool;
            public int StartPoolIndex;

            public void Execute()
            {
                var missingCheck = this.GetMissingMap();
                var activeLength = this.ProcessActive(missingCheck);
                this.DoMissingCheck(missingCheck);
                this.AssignIndices(activeLength);
            }

            private NativeHashSet<int> GetMissingMap()
            {
                var allIndices = new NativeHashSet<int>(this.Closest.Length, Allocator.Temp);

                for (var i = 0; i < this.Closest.Length; i++)
                {
                    allIndices.Add(i);
                }

                return allIndices;
            }

            private int ProcessActive(NativeHashSet<int> missingCheck)
            {
                int closetIndex;

                for (closetIndex = 0; closetIndex < this.Closest.Length; closetIndex++)
                {
                    ref var entity = ref this.Closest.ElementAt(closetIndex);

                    // Reached end
                    if (entity == Entity.Null)
                    {
                        break;
                    }

                    // Already active, just let it continue
                    if (this.Active.Remove(entity, out var poolIndex))
                    {
                        entity = Entity.Null;
                        missingCheck.Remove(poolIndex);
                    }
                }

                // Anything left in Active has been disabled, free the indices
                using var e = this.Active.GetEnumerator();
                while (e.MoveNext())
                {
                    var pooledIndex = e.Current.Value;
                    this.Pool.Return(pooledIndex);
                    this.AudioSourceIndices.GetRefRW(e.Current.Key).ValueRW.PoolIndex = -1;
                    this.AudioSourceIndices.SetComponentEnabled(e.Current.Key, false);
                }

                return closetIndex;
            }

            private void DoMissingCheck(NativeHashSet<int> missingCheck)
            {
                // We already removed active in ProcessActive, so remove any remaining tracked indices in the pool
                Check.Assume(this.Pool.Requests.Count == 0, "Repurposed hasn't been cleaned");

                foreach (var index in this.Pool.Available)
                {
                    var result = missingCheck.Remove(index);
                    Check.Assume(result, "Invalid state");
                }

                foreach (var index in this.Pool.Returned)
                {
                    var result = missingCheck.Remove(index);
                    Check.Assume(result, "Invalid state");
                }

                // Anything remaining in the missingCheck has been destroyed so return it
                foreach (var index in missingCheck)
                {
                    this.Pool.Return(index);
                }
            }

            private void AssignIndices(int activeLength)
            {
                // Anything left in Closest is a new source we need to assign
                for (var index = 0; index < activeLength; index++)
                {
                    var entity = this.Closest[index];
                    if (entity == Entity.Null)
                    {
                        // Was already active
                        continue;
                    }

                    this.AudioSourceIndices.GetRefRW(entity).ValueRW.PoolIndex = this.StartPoolIndex + this.Pool.Get();
                    this.AudioSourceIndices.SetComponentEnabled(entity, true);
                }
            }
        }
    }
}
#endif
