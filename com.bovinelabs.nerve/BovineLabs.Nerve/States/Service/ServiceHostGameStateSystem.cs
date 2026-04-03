// <copyright file="ServiceHostGameStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States.Service
{
    using BovineLabs.Core;
    using BovineLabs.Core.States;
    using BovineLabs.Nerve.Data.States;
    using Unity.Entities;

    [UpdateInGroup(typeof(ServiceStateSystemGroup))]
    public partial class ServiceHostGameStateSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnCreate()
        {
            StateAPI.Register<ServiceState, StateHostGame, ServiceStates>(ref this.CheckedStateRef, ServiceStates.HostGame);
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            BovineLabsBootstrap.Instance.CreateClientServerWorlds(false);
        }

        /// <inheritdoc />
        protected override void OnStopRunning()
        {
            BovineLabsBootstrap.Instance.DestroyClientServerWorlds();
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
