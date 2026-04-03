// <copyright file="ActionEnableableAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Actions
{
    using System;
    using System.Linq;
    using BovineLabs.Core;
    using BovineLabs.Core.Authoring.EntityCommands;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Reaction.Authoring;
    using BovineLabs.Reaction.Authoring.Core;
    using BovineLabs.Reaction.Data.Builders;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring component for enabling/disabling components on entities when a reaction is triggered.
    /// Works with components that implement enableable interfaces.
    /// </summary>
    [ReactionAuthoring]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReactionAuthoring))]
    public class ActionEnableableAuthoring : MonoBehaviour
    {
        public Data[] Enableables = Array.Empty<Data>();

        /// <summary>
        /// Configuration data for a single component enable/disable action.
        /// </summary>
        [Serializable]
        public class Data
        {
            public Target Target = Target.Target;

            public EnableableComponentAsset Enableable;
        }

        private class Baker : Baker<ActionEnableableAuthoring>
        {
            public override void Bake(ActionEnableableAuthoring authoring)
            {
                var builder = new ActionEnableableBuilder(Allocator.Temp);

                foreach (var e in authoring.Enableables)
                {
                    if (!e.Enableable)
                    {
                        continue;
                    }

                    if (!ValidateStableHashForEnableable(e.Enableable))
                    {
                        Debug.LogError($"Type {e.Enableable} has not been assigned to ReactionSettings");
                        continue;
                    }

                    builder.WithEnableable(e.Target, e.Enableable.GetStableTypeHash());
                }

                var commands = new BakerCommands(this, this.GetEntity(TransformUsageFlags.None));
                builder.ApplyTo(ref commands);
            }

            // This check is similar to StableTypeHashAttributeDrawer but not identical
            private static bool ValidateStableHashForEnableable(EnableableComponentAsset asset)
            {
                return AuthoringSettingsUtility.GetSettings<ReactionSettings>().Enableables.Contains(asset);
            }
        }
    }
}
