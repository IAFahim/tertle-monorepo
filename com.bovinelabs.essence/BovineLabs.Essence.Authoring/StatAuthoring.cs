// <copyright file="StatAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using System;
    using BovineLabs.Core.Authoring.EntityCommands;
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Builders;
    using BovineLabs.Reaction.Authoring;
    using BovineLabs.Reaction.Authoring.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring component for configuring stats and intrinsics on entities.
    /// This component allows setting up default stat values, stat groups, and intrinsic values that will be baked into runtime components.
    /// Supports initialization from other entities and optional event writing for stat modifications.
    /// </summary>
    [ReactionAuthoring]
    [DisallowMultipleComponent]
    public class StatAuthoring : MonoBehaviour, ILookupAuthoring<InitializeStats, InitializeStats.Data>
    {
        [Header("Stats")]
        public bool AddStats = true;
        [Tooltip("These are additive")]
        public StatModifierAuthoring[] StatDefaults = Array.Empty<StatModifierAuthoring>();

        [Tooltip("These are additive")]
        public StatGroup[] StatDefaultGroups = Array.Empty<StatGroup>();

        [PrefabElement]
        public bool StatsCanBeModified = true;

        [Header("Intrinsics")]
        public bool AddIntrinsics = true;

        [Tooltip("These are additive")]
        public IntrinsicDefault[] IntrinsicDefaults = Array.Empty<IntrinsicDefault>();

        [Tooltip("These are additive")]
        public IntrinsicGroup[] IntrinsicDefaultGroups = Array.Empty<IntrinsicGroup>();

        [PrefabElement]
        public InitializeData Initialize = new();

        /// <inheritdoc/>
        public bool TryGetInitialization(out InitializeStats.Data value)
        {
            if (this.StatsCanBeModified)
            {
                value = default;
                return false;
            }

            value = new InitializeStats.Data { Source = this.Initialize.CopyFrom };
            return true;
        }

        /// <summary>
        /// Baker for converting StatAuthoring components to runtime ECS components.
        /// Handles the baking of both stats and intrinsics during the authoring-to-runtime conversion process.
        /// </summary>
        public class Baker : Baker<StatAuthoring>
        {
            /// <inheritdoc/>
            public override void Bake(StatAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                var writeEvents = authoring.GetComponent<EventWriterAuthoring>() != null;

                this.BakeStats(authoring, entity, writeEvents);
                this.BakeIntrinsics(authoring, entity, writeEvents);
            }

            private void BakeStats(StatAuthoring authoring, Entity entity, bool writeEvents)
            {
                if (!authoring.AddStats)
                {
                    return;
                }

                var builder = new StatsBuilder(Allocator.Temp);

                foreach (var group in authoring.StatDefaultGroups)
                {
                    if (group == null)
                    {
                        continue;
                    }

                    this.DependsOn(group);

                    foreach (var stat in group.Values)
                    {
                        if (stat.Stat == null)
                        {
                            continue;
                        }

                        builder.WithDefault(stat.ToStatModifier());
                    }
                }

                foreach (var stat in authoring.StatDefaults)
                {
                    if (stat.Stat == null)
                    {
                        continue;
                    }

                    builder.WithDefault(stat.ToStatModifier());
                }

                builder.WithCanBeModified(authoring.StatsCanBeModified);
                builder.WithWriteEvents(writeEvents);

                var commands = new BakerCommands(this, entity);
                builder.ApplyTo(ref commands);
            }

            private void BakeIntrinsics(StatAuthoring authoring, Entity entity, bool writeEvents)
            {
                if (!authoring.AddIntrinsics)
                {
                    return;
                }

                var builder = new IntrinsicBuilder(Allocator.Temp);

                // Apply sets first and we can override them by defaults
                foreach (var group in authoring.IntrinsicDefaultGroups)
                {
                    if (group == null)
                    {
                        continue;
                    }

                    this.DependsOn(group);

                    foreach (var intrinsic in group.Values)
                    {
                        if (intrinsic.Intrinsic == null)
                        {
                            continue;
                        }

                        builder.WithDefault(new IntrinsicBuilder.Default(intrinsic.Intrinsic, intrinsic.Value));
                    }
                }

                foreach (var intrinsic in authoring.IntrinsicDefaults)
                {
                    if (intrinsic.Intrinsic == null)
                    {
                        continue;
                    }

                    builder.WithDefault(new IntrinsicBuilder.Default(intrinsic.Intrinsic, intrinsic.Value));
                }

                builder.WithWriteEvents(writeEvents);

                var commands = new BakerCommands(this, entity);
                builder.ApplyTo(ref commands);
            }
        }

        /// <summary>
        /// Configuration data for initializing stats from another entity target.
        /// Used when stats cannot be modified and need to be copied from a source entity during initialization.
        /// </summary>
        [Serializable]
        public class InitializeData
        {
            [Tooltip("When initialized should we copy stats from another target, for example our source. This is only allowed if stats can not be modified." +
                     "This is designed for things like projectiles which might want to snapshot the others stats on creation.")]
            public Target CopyFrom = Target.Source;
        }
    }
}
