// <copyright file="AudioSourceStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(BridgeSystemGroup), OrderLast = true)]
    public partial struct AudioSourceStateSystem : ISystem
    {
        private SyncEnableStateUtil<AudioSourceEnabled, AudioSourceEnabledPrevious> enableUtil;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.enableUtil.OnCreate(ref state, true);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.enableUtil.OnUpdate(ref state);
        }
    }
}
