// <copyright file="PauseClientSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Pause
{
    using BovineLabs.Core;
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Pause;
    using BovineLabs.Nerve.Data.Pause;
    using Unity.Burst;
    using Unity.Entities;

    [WorldSystemFilter(Worlds.ClientLocal)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    public partial struct PauseClientSystem : ISystem, IUpdateWhilePaused
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var isPaused = PauseGame.IsPaused(ref state);
            var serverPaused = SystemAPI.HasSingleton<ClientPaused>();

            if (isPaused)
            {
                if (!serverPaused)
                {
                    PauseGame.Unpause(ref state);
                }
            }
            else
            {
                if (serverPaused)
                {
                    PauseGame.Pause(ref state);
                }
            }
        }
    }
}
