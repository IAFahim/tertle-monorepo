// <copyright file="ServiceJoinGameStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States.Service
{
    using BovineLabs.Core;
    using BovineLabs.Core.States;
    using BovineLabs.Nerve.Data.States;
    using Unity.Entities;

    [UpdateInGroup(typeof(ServiceStateSystemGroup))]
    public partial class ServiceJoinGameStateSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnCreate()
        {
            StateAPI.Register<ServiceState, StateHostGame, ServiceStates>(ref this.CheckedStateRef, ServiceStates.JoinGame);
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            BovineLabsBootstrap.Instance.CreateClientWorld();
        }

        /// <inheritdoc />
        protected override void OnStopRunning()
        {
            BovineLabsBootstrap.Instance.DestroyClientWorld();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
        }

        private struct StateHostGame : IComponentData
        {
        }
    }
}
