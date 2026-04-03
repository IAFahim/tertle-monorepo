// <copyright file="ServiceSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Authoring
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;
    using BovineLabs.Nerve.Data.States;
    using Unity.Entities;
    using UnityEngine;

    [SettingsGroup("Core")]
    [SettingsWorld("service")]
    public class ServiceSettings : SettingsBase
    {
        [SerializeField]
        [K(nameof(ServiceStates))]
        private byte defaultState;

        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);
            var gameState = new ServiceState { Value = new BitArray256 { [0] = true } };

            // States
            baker.AddComponent(entity, gameState);
            baker.AddComponent<ServiceStatePrevious>(entity);

            baker.AddComponent(entity, new ServiceInitStateConfig
            {
                DefaultState = this.defaultState,
            });
        }
    }
}
