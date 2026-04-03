// <copyright file="ServiceGameStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States.Service
{
    using BovineLabs.Core;
    using BovineLabs.Core.States;
    using BovineLabs.Nerve.Data.States;
    using Unity.Entities;

    /// <summary> State that creates and destroys a single player game world. </summary>
    [UpdateInGroup(typeof(ServiceStateSystemGroup))]
    public partial class ServiceGameStateSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnCreate()
        {
            StateAPI.Register<ServiceState, StateGame, ServiceStates>(ref this.CheckedStateRef, ServiceStates.Game);
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            BovineLabsBootstrap.Instance.CreateGameWorld();
        }

        /// <inheritdoc />
        protected override void OnStopRunning()
        {
            BovineLabsBootstrap.Instance.DestroyGameWorld();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
        }

        private struct StateGame : IComponentData
        {
        }
    }
}
