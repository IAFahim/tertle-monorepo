// <copyright file="AudioSourceOneShotPoolSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>Assigns pooled one-shot AudioSources and syncs their transforms.</summary>
    [UpdateInGroup(typeof(BridgeSimulationSystemGroup))]
    public partial struct AudioSourceOneShotPoolSystem : ISystem
    {
        private long frameStamp;
        private EntityQuery listenerQuery;
        private NativeReference<float3> listenerPosition;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.listenerQuery = SystemAPI.QueryBuilder().WithAll<CameraMain, LocalTransform>().Build();
            this.listenerPosition = new NativeReference<float3>(Allocator.Persistent);

            state.RequireForUpdate(this.listenerQuery);
            state.RequireForUpdate<AudioSourcePool>();
            state.AddDependency<AudioSourcePool>(); // add write
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
            this.listenerPosition.Dispose();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pool = SystemAPI.GetSingleton<AudioSourcePool>();

            this.frameStamp++;

            var audioSourceQuery = SystemAPI.QueryBuilder()
                .WithAll<AudioSourceOneShot, AudioSourceDataExtended, AudioSourceAudibleRange, AudioSourceEnabled, LocalToWorld>()
                .WithPresent<AudioSourceIndex>()
                .Build();

            var oneShotEntities = audioSourceQuery.ToEntityListAsync(state.WorldUpdateAllocator, out var dependency1);
            var oneShotLocalToWorlds = audioSourceQuery.ToComponentDataListAsync<LocalToWorld>(state.WorldUpdateAllocator, out var dependency2);
            var oneShotAudibleRanges = audioSourceQuery.ToComponentDataListAsync<AudioSourceAudibleRange>(state.WorldUpdateAllocator, out var dependency3);
            var oneShotDataExtended = audioSourceQuery.ToComponentDataListAsync<AudioSourceDataExtended>(state.WorldUpdateAllocator, out var dependency4);

            var combined = JobHandle.CombineDependencies(state.Dependency, dependency1, dependency2);
            state.Dependency = JobHandle.CombineDependencies(combined, dependency3, dependency4);
            var closests = CollectionHelper.CreateNativeArray<Entity>(pool.OneShotPool.Length, state.WorldUpdateAllocator);

            state.Dependency = new GetListenerPositionJob { ListenerPosition = this.listenerPosition }.Schedule(state.Dependency);

            state.Dependency = new AudioSourcePrioritySortJob
            {
                Entities = oneShotEntities.AsDeferredJobArray(),
                LocalToWorlds = oneShotLocalToWorlds.AsDeferredJobArray(),
                AudibleRanges = oneShotAudibleRanges.AsDeferredJobArray(),
                AudioSourceDataExtended = oneShotDataExtended.AsDeferredJobArray(),
                Closests = closests,
                ListenerPosition = this.listenerPosition,
            }.Schedule(state.Dependency);

            state.Dependency = new AssignOneShotIndicesJob
            {
                Pool = pool.OneShotPool,
                Entities = oneShotEntities.AsDeferredJobArray(),
                Closest = closests,
                AudioSourceIndices = SystemAPI.GetComponentLookup<AudioSourceIndex>(),
                AudioSourceEnableds = SystemAPI.GetComponentLookup<AudioSourceEnabled>(),
                Order = pool.OneShotOrder,
                FrameStamp = this.frameStamp,
                StartPoolIndex = pool.OneShotStartIndex,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private struct AssignOneShotIndicesJob : IJob
        {
            public TrackedIndexPool Pool;

            [ReadOnly]
            public NativeArray<Entity> Entities;

            [ReadOnly]
            public NativeArray<Entity> Closest;

            public ComponentLookup<AudioSourceIndex> AudioSourceIndices;
            public ComponentLookup<AudioSourceEnabled> AudioSourceEnableds;
            public NativeArray<long> Order;
            public long FrameStamp;
            public int StartPoolIndex;

            public void Execute()
            {
                // Disable all sources
                foreach (var entity in this.Entities)
                {
                    this.AudioSourceEnableds.SetComponentEnabled(entity, false);
                }

                foreach (var entity in this.Closest)
                {
                    if (entity == Entity.Null)
                    {
                        return;
                    }

                    var index = this.GetPoolIndex();
                    if (index < 0)
                    {
                        return;
                    }

                    var poolIndex = this.StartPoolIndex + index;

                    this.AudioSourceIndices.GetRefRW(entity).ValueRW.PoolIndex = poolIndex;
                    this.AudioSourceIndices.SetComponentEnabled(entity, true);
                    this.Order[index] = this.FrameStamp;
                }
            }

            private int GetPoolIndex()
            {
                if (this.Pool.Available.Count == 0 && this.Pool.Returned.Count == 0)
                {
                    var oldestIndex = this.FindOldestIndex();
                    if (oldestIndex != -1)
                    {
                        this.Order[oldestIndex] = 0;
                        this.Pool.Return(oldestIndex);
                    }
                    else
                    {
                        return -1;
                    }
                }

                return this.Pool.Get();
            }

            private int FindOldestIndex()
            {
                var oldestIndex = -1;
                var oldestOrder = long.MaxValue;

                for (var i = 0; i < this.Order.Length; i++)
                {
                    var order = this.Order[i];
                    if (order == 0 || order >= this.FrameStamp)
                    {
                        continue;
                    }

                    if (order >= oldestOrder)
                    {
                        continue;
                    }

                    oldestOrder = order;
                    oldestIndex = i;
                }

                return oldestIndex;
            }
        }
    }
}
#endif
