// <copyright file="ClientInitStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States.Client
{
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Pause;
    using BovineLabs.Core.States;
    using BovineLabs.Nerve.Data.States;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(ClientStateSystemGroup))]
    public partial struct ClientInitStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<ClientState, StateInit, ClientStates>(ref state, ClientStates.Init);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.QueryBuilder().WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build().IsEmptyIgnoreFilter)
            {
                return;
            }

            var initState = ClientStates.NameToKey(ClientStates.Init);

            var clientInit = SystemAPI.GetSingleton<ClientInitStateConfig>();
            var defaultState = clientInit.DefaultState;
            if (defaultState != initState)
            {
                var stateName = ClientStates.KeyToName(defaultState);
                AppAPI.StateSet<ClientState, BitArray256, ClientStates>(ref state, stateName);
            }
            else
            {
                SystemAPI.GetSingleton<BLLogger>().LogWarning("No default state set on ClientInitStateSystem");
                state.Enabled = false;
            }
        }

        private struct StateInit : IComponentData
        {
        }
    }
}
