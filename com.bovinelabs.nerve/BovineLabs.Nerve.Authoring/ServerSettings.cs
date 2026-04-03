// <copyright file="ServerSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Authoring
{
    using System;
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using BovineLabs.Nerve.Data.App;
    using Unity.Entities;
    using UnityEngine;

    [SettingsGroup("Core")]
    [SettingsWorld("Server")]
    public class ServerSettings : SettingsBase
    {
        // [SerializeField]
        // private ObjectDefinition pause;

        [SerializeField]
        private ObjectDefinition playerController;

        [SerializeField]
        private ObjectDefinition playerCharacter;

        [SerializeField]
        private RelevancySettings relevancy = new();

        /// <inheritdoc/>
        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);

            baker.AddComponent(entity, new ServerPrefabs
            {
                // PauseGhost = this.pause,
                PlayerController = this.playerController,
                PlayerCharacter = this.playerCharacter,
            });

            baker.AddComponent(entity, new RelevanceConfig
            {
                ClampExtents = this.relevancy.ClampExtents,
                ExpandExtents = this.relevancy.ExpandExtents,
            });
        }

        [Serializable]
        private class RelevancySettings
        {
            public float ClampExtents = 150 / 2f; // Half size
            public float ExpandExtents = 8;
        }
    }
}
