// <copyright file="AudioSourceOneShotReturnSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data;
    using Unity.Entities;

    [WorldSystemFilter(BridgeWorlds.All)]
    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    [UpdateAfter(typeof(AudioSyncSystem))]
    [UpdateAfter(typeof(AudioSourcePoolSyncSystem))]
    public partial class AudioSourceOneShotReturnSystem : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var poolQuery = SystemAPI.QueryBuilder().WithAllRW<AudioSourcePool>().Build();
            poolQuery.CompleteDependency();
            ref var pool = ref poolQuery.GetSingletonRW<AudioSourcePool>().ValueRW;

            var sources = pool.AudioSources;
            var order = pool.OneShotOrder;
            var startIndex = pool.OneShotStartIndex;

            for (var i = 0; i < order.Length; i++)
            {
                if (order[i] == 0)
                {
                    continue;
                }

                if (sources[startIndex + i].AudioSource.Value.isPlaying)
                {
                    continue;
                }

                order[i] = 0;
                pool.OneShotPool.Return(i);
            }
        }
    }
}
#endif
