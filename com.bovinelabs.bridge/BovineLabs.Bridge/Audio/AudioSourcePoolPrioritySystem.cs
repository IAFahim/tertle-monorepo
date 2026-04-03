// <copyright file="AudioSourcePoolPrioritySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Audio
{
    using System;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Bridge.Util;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Camera;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary> Calculates priority for pooled audio sources based on distance from camera. </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(AfterTransformSystemGroup))]
    public partial struct AudioSourcePoolPrioritySystem : ISystem
    {
        private NativeHashMap<Entity, int> active;
        private EntityQuery query;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.active = new NativeHashMap<Entity, int>(0, Allocator.Persistent);
            this.query = SystemAPI.QueryBuilder().WithAll<CameraMain, LocalTransform>().Build();
            state.RequireForUpdate(this.query);
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
            this.active.Dispose();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<AudioSourcePool>(out var pool))
            {
                return;
            }

            var config = SystemAPI.GetSingleton<AudioSourcePoolConfig>();
            var audioSourceQuery = SystemAPI.QueryBuilder().WithAll<AudioSourceData, AudioSourceEnabled, LocalToWorld>().WithPresent<AudioSourceIndex>().Build();

            var activeEntities = audioSourceQuery.ToEntityListAsync(state.WorldUpdateAllocator, out var dependency1);
            var activeLocalToWorlds = audioSourceQuery.ToComponentDataListAsync<LocalToWorld>(state.WorldUpdateAllocator, out var dependency2);
            var dependencyChain1 = this.ReturnDisabledAudio(ref state, pool, JobHandle.CombineDependencies(state.Dependency, dependency1, dependency2));
            var dependencyChain2 = new ClearNativeHashMapJob<Entity, int> { HashMap = this.active }.Schedule(state.Dependency);

            state.Dependency = JobHandle.CombineDependencies(dependencyChain2, dependencyChain1);
            state.Dependency = new ActiveAudioJob { Active = this.active }.Schedule(state.Dependency);

            this.ActivateAudio(ref state, pool, activeEntities, activeLocalToWorlds, config);
        }

        private JobHandle ReturnDisabledAudio(ref SystemState state, AudioSourcePool pool, JobHandle dependency)
        {
            var returnDisabledAudioQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<AudioSourceIndex>()
                .WithAll<AudioSourceEnabledPrevious>()
                .WithDisabled<AudioSourceEnabled>()
                .WithOptions(EntityQueryOptions.IncludeDisabledEntities)
                .Build();

            return new ReturnDisabledAudioJob
            {
                Pool = pool.Pool,
                AudioSourceIndexHandle = SystemAPI.GetComponentTypeHandle<AudioSourceIndex>(),
            }.Schedule(returnDisabledAudioQuery, dependency);
        }

        private void ActivateAudio(
            ref SystemState state, AudioSourcePool pool, NativeList<Entity> activeEntities, NativeList<LocalToWorld> activeLocalToWorlds,
            AudioSourcePoolConfig config)
        {
            var listenerPosition = this.GetListenerPosition();
            var closests = CollectionHelper.CreateNativeArray<Entity>(pool.Pool.Length, state.WorldUpdateAllocator);

            state.Dependency = new SortByDistancesJob
            {
                Entities = activeEntities.AsDeferredJobArray(),
                LocalToWorlds = activeLocalToWorlds.AsDeferredJobArray(),
                Closests = closests,
                ListenerPosition = listenerPosition,
                MaxListenDistanceSq = config.MaxListenDistanceSq,
            }.Schedule(state.Dependency);

            state.Dependency = new AssignPoolIndicesJob
            {
                Active = this.active,
                Closest = closests,
                AudioSourceIndices = SystemAPI.GetComponentLookup<AudioSourceIndex>(),
                Pool = pool.Pool,
            }.Schedule(state.Dependency);
        }

        private float3 GetListenerPosition()
        {
            this.query.CompleteDependency();
            return this.query.GetSingleton<LocalTransform>().Position;
        }

        [BurstCompile]
        private unsafe struct ReturnDisabledAudioJob : IJobChunk
        {
            public TrackedIndexPool Pool;

            public ComponentTypeHandle<AudioSourceIndex> AudioSourceIndexHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var indexes = (AudioSourceIndex*)chunk.GetRequiredComponentDataPtrRW(ref this.AudioSourceIndexHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var i))
                {
                    this.Pool.Return(indexes[i].PoolIndex);
                    indexes[i].PoolIndex = -1;
                    chunk.SetComponentEnabled(ref this.AudioSourceIndexHandle, i, false);
                }
            }
        }

        [BurstCompile]
        private partial struct ActiveAudioJob : IJobEntity
        {
            public NativeHashMap<Entity, int> Active;

            private void Execute(Entity entity, in AudioSourceIndex audioSourceIndex)
            {
                this.Active.Add(entity, audioSourceIndex.PoolIndex);
            }
        }

        [BurstCompile]
        private unsafe struct SortByDistancesJob : IJob
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public NativeArray<LocalToWorld> LocalToWorlds;

            public NativeArray<Entity> Closests;

            public float3 ListenerPosition;
            public float MaxListenDistanceSq;

            public void Execute()
            {
                using var allDistancesList = PooledNativeList<float>.Make();
                allDistancesList.List.ResizeUninitialized(this.Entities.Length);
                var allDistances = allDistancesList.List.AsArray();

                this.CalculateAllDistances(allDistances);
                this.GetClosest(allDistances);
            }

            private void CalculateAllDistances(NativeArray<float> allDistances)
            {
                for (var index = 0; index < this.Entities.Length; index++)
                {
                    var localToWorlds = this.LocalToWorlds[index];
                    allDistances[index] = math.distancesq(this.ListenerPosition, localToWorlds.Position);
                }
            }

            private void GetClosest(NativeArray<float> allDistances)
            {
                Span<float> distances = stackalloc float[this.Closests.Length];
                distances.Fill(float.PositiveInfinity);

                var maxDistanceSq = this.MaxListenDistanceSq;

                for (var index = 0; index < this.Entities.Length; index++)
                {
                    var distanceSq = allDistances[index];
                    if (distanceSq >= maxDistanceSq)
                    {
                        continue;
                    }

                    for (var k = 0; k < this.Closests.Length; k++)
                    {
                        if (distanceSq >= distances[k])
                        {
                            continue;
                        }

                        var itemsToMove = this.Closests.Length - 1 - k;

                        // Move the remaining items one step in the arrays
                        if (itemsToMove > 0)
                        {
                            var neighbourPtr = (Entity*)this.Closests.GetUnsafePtr();
                            UnsafeUtility.MemMove(neighbourPtr + k + 1, neighbourPtr + k, itemsToMove * sizeof(Entity));

                            var distancePtr = (float*)UnsafeUtility.AddressOf(ref distances.GetPinnableReference());
                            UnsafeUtility.MemMove(distancePtr + k + 1, distancePtr + k, itemsToMove * sizeof(float));
                        }

                        this.Closests[k] = this.Entities[index];
                        distances[k] = distanceSq;

                        if (k == this.Closests.Length - 1)
                        {
                            // We reached the end of the array. This means that we just updated the largest distance.
                            // We can use this to restrict the future search. We know that no other agent distance we find can be larger than this value.
                            maxDistanceSq = distanceSq;
                        }

                        break;
                    }
                }
            }
        }

        [BurstCompile]
        private struct AssignPoolIndicesJob : IJob
        {
            public NativeHashMap<Entity, int> Active;
            public NativeArray<Entity> Closest;
            public ComponentLookup<AudioSourceIndex> AudioSourceIndices;
            public TrackedIndexPool Pool;

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

                    this.AudioSourceIndices.GetRefRW(entity).ValueRW.PoolIndex = this.Pool.Get();
                    this.AudioSourceIndices.SetComponentEnabled(entity, true);
                }
            }
        }
    }
}
