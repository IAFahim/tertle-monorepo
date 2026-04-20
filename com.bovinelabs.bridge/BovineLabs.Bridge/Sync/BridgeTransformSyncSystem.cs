// <copyright file="BridgeTransformSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge
{
    using BovineLabs.Bridge.Data;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Profiling;
    using Unity.Transforms;
    using UnityEngine;
    using UnityEngine.Jobs;

    [UpdateInGroup(typeof(BridgeTransformSyncSystemGroup))]
    public partial struct BridgeTransformSyncSystem : ISystem
    {
        private NativeList<TransformHandle> transforms;
        private EntityQuery transformQuery;
        private TransformAccessArray transformAccessArray;
        private int orderVersion;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.transforms = new NativeList<TransformHandle>(Allocator.Persistent);
            this.transformAccessArray = new TransformAccessArray(64);
            this.transformQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld>().WithAllRW<BridgeObject>().Build();
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
            this.transforms.Dispose();
            this.transformAccessArray.Dispose();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.UpdateTransformAccessArray(ref state);
            var localToWorlds = this.transformQuery.ToComponentDataListAsync<LocalToWorld>(state.WorldUpdateAllocator, out var handle);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, handle);
            state.Dependency = new CopyTransformJob
            {
                LocalToWorlds = localToWorlds,
            }.Schedule(this.transformAccessArray, state.Dependency);
        }

        private unsafe void UpdateTransformAccessArray(ref SystemState state)
        {
            var version = this.transformQuery._GetImpl()->_Access->EntityComponentStore->GetComponentTypeOrderVersion(TypeManager.GetTypeIndex<LocalToWorld>());
            if (version == this.orderVersion)
            {
                return;
            }

            this.orderVersion = version;

            using (new ProfilerMarker("DirtyTransformAccessArrayUpdate").Auto())
            {
                var bridgeObjectHandle = SystemAPI.GetComponentTypeHandle<BridgeObject>(true);

                foreach (var chunk in this.transformQuery.ToArchetypeChunkArray(state.WorldUpdateAllocator))
                {
                    var bridgeObjects = (BridgeObject*)chunk.GetRequiredComponentDataPtrRO(ref bridgeObjectHandle);
                    for (var i = 0; i < chunk.Count; i++)
                    {
                        this.transforms.Add(bridgeObjects[i].Transform);
                    }
                }

                // TODO at some point explore not rebuilding and instead add/remove
                this.transformAccessArray.SetTransformHandles(this.transforms.AsArray());

                this.transforms.Clear();
            }
        }

        [BurstCompile]
        private struct CopyTransformJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeList<LocalToWorld> LocalToWorlds;

            public void Execute(int index, TransformAccess transform)
            {
                var ltw = this.LocalToWorlds[index];
                TransformUtil.Set(transform, ltw);
            }
        }
    }
}
