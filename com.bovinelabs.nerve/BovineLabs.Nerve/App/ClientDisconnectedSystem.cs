// <copyright file="ClientDisconnectedSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.App
{
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.States;
    using BovineLabs.Nerve.Data.States;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.NetCode;

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    public partial struct ClientDisconnectedSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var connectionEventsForClient = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
            if (connectionEventsForClient.Length == 0)
            {
                return;
            }

            var debug = SystemAPI.GetSingleton<BLLogger>();

            foreach (var evt in connectionEventsForClient)
            {
                debug.LogDebug($"Connection Event | {evt.State.ToFixedString()}");
                if (evt.State == ConnectionState.State.Disconnected)
                {
                    AppAPI.StateSet<ClientState, BitArray256, ClientStates>(ref state, "quit");
                }
            }
        }
    }
}
