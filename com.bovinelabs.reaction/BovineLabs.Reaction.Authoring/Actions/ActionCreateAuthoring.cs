// <copyright file="ActionCreateAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Actions
{
    using System;
    using BovineLabs.Core.Authoring.EntityCommands;
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Reaction.Authoring.Core;
    using BovineLabs.Reaction.Data.Builders;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring component for creating new entities when a reaction is triggered.
    /// Supports creating multiple entities with different targets and destruction behaviors.
    /// </summary>
    [ReactionAuthoring]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReactionAuthoring))]
    public class ActionCreateAuthoring : MonoBehaviour
    {
        public Data[] Create = Array.Empty<Data>();

        /// <summary>
        /// Configuration data for a single entity creation action.
        /// </summary>
        [Serializable]
        public class Data
        {
            public ObjectDefinition Definition;

            public Target Target = Target.Target;

            [Tooltip("Destroy the created entity when either the source gets destroyed or disabled")]
            public bool DestroyOnDisabled;
        }

        public class Baker : Baker<ActionCreateAuthoring>
        {
            public static void Bake(IBaker baker, Data[] create)
            {
                var builder = new ActionCreateBuilder(Allocator.Temp);

                foreach (var e in create)
                {
                    if (e.Definition == null)
                    {
                        continue;
                    }

                    baker.DependsOn(e.Definition);

                    builder.WithCreate(e.Definition, e.Target, e.DestroyOnDisabled);
                }

                var commands = new BakerCommands(baker, baker.GetEntity(TransformUsageFlags.None));
                builder.ApplyTo(ref commands);
            }

            public override void Bake(ActionCreateAuthoring authoring)
            {
                Bake(this, authoring.Create);
            }
        }
    }
}
