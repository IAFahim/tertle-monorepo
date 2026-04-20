// <copyright file="AudioSourceOneShotResetSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(BridgeSyncSystemGroup), OrderLast = true)]
    public partial struct AudioSourceOneShotResetSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ResetSourceIndexJob().Schedule();
        }

        [BurstCompile]
        [WithAll(typeof(AudioSourceOneShot))]
        private partial struct ResetSourceIndexJob : IJobEntity
        {
            private static void Execute(EnabledRefRW<AudioSourceIndex> index, ref AudioSourceIndex audioSourceIndex)
            {
                index.ValueRW = false;
                audioSourceIndex.PoolIndex = -1;
            }
        }
    }
}
#endif
