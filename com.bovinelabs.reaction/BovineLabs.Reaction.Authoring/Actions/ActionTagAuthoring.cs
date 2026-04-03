// <copyright file="ActionTagAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Actions
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Authoring.EntityCommands;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Reaction.Authoring;
    using BovineLabs.Reaction.Authoring.Core;
    using BovineLabs.Reaction.Data.Builders;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring component for adding or removing tag components on entities when a reaction is triggered.
    /// Works with zero-size component tags for entity categorization and filtering.
    /// </summary>
    [ReactionAuthoring]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReactionAuthoring))]
    public class ActionTagAuthoring : MonoBehaviour
    {
        public Data[] Tags = Array.Empty<Data>();

        /// <summary>
        /// Configuration data for a single tag component action.
        /// </summary>
        [Serializable]
        public class Data
        {
            public Target Target = Target.Target;

            [StableTypeHash(StableTypeHashAttribute.TypeCategory.ComponentData, OnlyZeroSize = true, AllowUnityNamespace = false)]
            public ulong Tag;
        }

        private class Baker : Baker<ActionTagAuthoring>
        {
            public override void Bake(ActionTagAuthoring authoring)
            {
                var builder = new ActionTagBuilder(Allocator.Temp);

                foreach (var t in authoring.Tags.Where(t => ReactionValidationUtil.ValidateStableHashForTag(t.Tag)))
                {
                    if (!ReactionValidationUtil.ValidateStableHashForTag(t.Tag))
                    {
                        Debug.LogWarning($"Type {t.Tag} is no longer valid for action tag");
                        continue;
                    }

                    builder.WithTag(t.Target, t.Tag);
                }

                var commands = new BakerCommands(this, this.GetEntity(TransformUsageFlags.None));
                builder.ApplyTo(ref commands);
            }
        }
    }
}
