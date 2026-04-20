// <copyright file="AudioSourceTransformSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;
    using UnityEngine.Jobs;

    [UpdateInGroup(typeof(BridgeTransformSyncSystemGroup))]
    public partial struct AudioSourceTransformSyncSystem : ISystem, ISystemStartStop
    {
        private TransformAccessArray transformAccessArray;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AudioSourcePool>();
        }

        /// <inheritdoc/>
        public void OnStartRunning(ref SystemState state)
        {
            var pool = SystemAPI.GetSingleton<AudioSourcePool>();

            var poolSize = pool.AudioSources.Length;

            this.transformAccessArray = new TransformAccessArray(poolSize);

            for (var i = 0; i < pool.AudioSources.Length; i++)
            {
                this.transformAccessArray.Add(pool.AudioSources[i].AudioSource.Value.transform);
            }
        }

        /// <inheritdoc/>
        public void OnStopRunning(ref SystemState state)
        {
            this.transformAccessArray.Dispose();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<LocalToWorld, AudioSourceIndex>().Build();
            var localToWorlds = query.ToComponentDataListAsync<LocalToWorld>(state.WorldUpdateAllocator, out var dep1);
            var audioSourceIndices = query.ToComponentDataListAsync<AudioSourceIndex>(state.WorldUpdateAllocator, out var dep2);

            state.Dependency = JobHandle.CombineDependencies(state.Dependency, dep1, dep2);

            state.Dependency = new CopyTransformJob
            {
                LocalToWorlds = localToWorlds,
                AudioSourceIndexes = audioSourceIndices,
            }.Schedule(this.transformAccessArray, state.Dependency);
        }

        [BurstCompile]
        private struct CopyTransformJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeList<LocalToWorld> LocalToWorlds;

            [ReadOnly]
            public NativeList<AudioSourceIndex> AudioSourceIndexes;

            public void Execute(int poolIndex, TransformAccess transform)
            {
                // Find the index that matches the audio source
                for (var index = 0; index < this.AudioSourceIndexes.Length; index++)
                {
                    if (this.AudioSourceIndexes[index].PoolIndex != poolIndex)
                    {
                        continue;
                    }

                    var ltw = this.LocalToWorlds[index];
                    TransformUtil.Set(transform, ltw);
                    return;
                }
            }
        }
    }
}
#endif
