// <copyright file="ClientNetworkToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Debug.Systems
{
    using BovineLabs.Anchor.Toolbar;
    using BovineLabs.Nerve.Debug.ViewModels;
    using BovineLabs.Nerve.Debug.Views;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.NetCode;
    using UnityEngine;

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    public partial struct ClientNetworkToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<ClientNetworkToolbarView, ClientNetworkToolbarViewModel, ClientNetworkToolbarViewModel.Data> helper;
        private float timeToTriggerUpdatesPassed;

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.helper = new ToolbarHelper<ClientNetworkToolbarView, ClientNetworkToolbarViewModel, ClientNetworkToolbarViewModel.Data>(ref state, "Network");
            state.RequireForUpdate<NetworkSnapshotAck>();
        }

        /// <inheritdoc />
        public void OnStartRunning(ref SystemState state)
        {
            this.helper.Load();
        }

        /// <inheritdoc />
        public void OnStopRunning(ref SystemState state)
        {
            this.helper.Unload();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.timeToTriggerUpdatesPassed += Time.unscaledDeltaTime;
            if (this.timeToTriggerUpdatesPassed < ToolbarView.DefaultUpdateRate)
            {
                return;
            }

            this.timeToTriggerUpdatesPassed -= ToolbarView.DefaultUpdateRate;

            ref var binding = ref this.helper.Binding;

            var snapshotActComponent = SystemAPI.GetSingleton<NetworkSnapshotAck>();

            binding.Ping = new PingData(snapshotActComponent.EstimatedRTT, snapshotActComponent.DeviationRTT);
        }

        public struct StructWithAByte
        {
            public byte Value;
        }

        public struct SomeStruct
        {
        }
    }
}
