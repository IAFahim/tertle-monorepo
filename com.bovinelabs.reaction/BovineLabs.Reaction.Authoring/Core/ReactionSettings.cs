// <copyright file="ReactionSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Core
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using BovineLabs.Reaction.Authoring.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Settings configuration for the Reaction system.
    /// Contains global configuration for condition events and enableable component types.
    /// </summary>
    [SettingsGroup("Reaction")]
    [SettingsWorld("Server")]
    public class ReactionSettings : SettingsBase
    {
        [SerializeField]
        [NonReorderable]
        private List<ConditionEventObject> conditionEvents = new();

        [SerializeField]
        private EnableableComponentAsset[] enableables = Array.Empty<EnableableComponentAsset>();

        public IReadOnlyList<ConditionSchemaObject> ConditionEvents => this.conditionEvents;

        public IReadOnlyList<EnableableComponentAsset> Enableables => this.enableables;

        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);

            var set = baker.AddBuffer<ReactionEnableables>(entity).Initialize().AsMap();
            foreach (var e in this.Enableables)
            {
                if (e != null)
                {
                    set.Add(e.GetStableTypeHash());
                }
            }
        }
    }
}
