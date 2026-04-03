// <copyright file="ServiceInitStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States.Service
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Pause;
    using BovineLabs.Core.States;
    using BovineLabs.Core.Utility;
    using BovineLabs.Nerve.Data;
    using BovineLabs.Nerve.Data.States;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    [Configurable]
    [UpdateInGroup(typeof(ServiceStateSystemGroup))]
    public partial class ServiceInitStateSystem : SystemBase
    {
        [ConfigVar("states.override.enable", false, "Enable skipping to a specific service state", true)]
        private static readonly SharedStatic<bool> OverrideEnable = SharedStatic<bool>.GetOrCreate<OverrideEnableType>();

        [ConfigVar("states.override.service.state", "game", "The service state to skip to", true)]
        private static readonly SharedStatic<FixedString32Bytes> OverrideState = SharedStatic<FixedString32Bytes>.GetOrCreate<OverrideStateType>();

        private readonly CancellationTokenSource cancellationTokenSource = new();
        private Task initializationTasks;

        /// <inheritdoc />
        protected override void OnCreate()
        {
            StateAPI.Register<ServiceState, StateInit, ServiceStates>(ref this.CheckedStateRef, ServiceStates.Init);

            Physics.simulationMode = SimulationMode.Script;

            // TODO results
            var list = new List<Task<bool>>();

            foreach (var t in ReflectionUtility.GetAllImplementations<IInitTask>())
            {
                var init = (IInitTask)Activator.CreateInstance(t);
                list.Add(init.Initialize(this.World, this.cancellationTokenSource.Token));
            }

            this.initializationTasks = Task.WhenAll(list);
        }

        protected override void OnDestroy()
        {
            this.cancellationTokenSource.Dispose();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            if (!this.initializationTasks.IsCompleted)
            {
                return;
            }

            if (!SystemAPI.QueryBuilder().WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build().IsEmptyIgnoreFilter)
            {
                return;
            }

            var init = ServiceStates.NameToKey(ServiceStates.Init);

            if (OverrideEnable.Data && ServiceStates.TryNameToKey(OverrideState.Data, out var overrideKey) && overrideKey != init)
            {
                AppAPI.StateSet<ServiceState, BitArray256, ServiceStates>(ref this.CheckedStateRef, OverrideState.Data.ToString());
            }
            else
            {
                var serviceInit = SystemAPI.GetSingleton<ServiceInitStateConfig>();
                var defaultState = serviceInit.DefaultState;
                if (defaultState != init)
                {
                    var stateName = ServiceStates.KeyToName(defaultState);
                    AppAPI.StateSet<ServiceState, BitArray256, ServiceStates>(ref this.CheckedStateRef, stateName);
                }
                else
                {
                    SystemAPI.GetSingleton<BLLogger>().LogWarning("No default state set");
                    this.Enabled = false;
                }
            }
        }

        internal struct OverrideEnableType
        {
        }

        private struct StateInit : IComponentData
        {
        }

        private struct OverrideStateType
        {
        }
    }
}
