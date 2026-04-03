// <copyright file="ActionIntrinsicAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring.Actions
{
    using System;
    using BovineLabs.Core.Authoring.EntityCommands;
    using BovineLabs.Essence.Data.Actions;
    using BovineLabs.Essence.Data.Builders;
    using BovineLabs.Reaction.Authoring;
    using BovineLabs.Reaction.Authoring.Core;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring component for configuring intrinsic modifications that occur as part of a reaction action.
    /// This component defines how intrinsics should be modified when a reaction is triggered, specifying which intrinsics to change and by what amount.
    /// </summary>
    [ReactionAuthoring]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReactionAuthoring))]
    public class ActionIntrinsicAuthoring : MonoBehaviour
    {
        public Data[] Intrinsics = Array.Empty<Data>();

        /// <summary>
        /// Configuration data for a single intrinsic modification action.
        /// Defines which intrinsic to modify, the amount to change it by, and the target entity to apply the change to.
        /// </summary>
        [Serializable]
        public class Data
        {
            public IntrinsicSchemaObject? Intrinsic;
            public int Amount;
            public Target Target = Target.Target;
        }

        private class Baker : Baker<ActionIntrinsicAuthoring>
        {
            /// <inheritdoc/>
            public override void Bake(ActionIntrinsicAuthoring authoring)
            {
                var builder = new ActionIntrinsicBuilder(Allocator.Temp);

                foreach (var change in authoring.Intrinsics)
                {
                    if (change.Intrinsic == null)
                    {
                        continue;
                    }

                    if (change.Target == Target.None)
                    {
                        Debug.LogWarning($"{nameof(ActionIntrinsicAuthoring)}: Can't use target none on {authoring}");
                        return;
                    }

                    builder.WithIntrinsic(new ActionIntrinsic
                    {
                        Intrinsic = change.Intrinsic,
                        Amount = change.Amount,
                        Target = change.Target,
                    });
                }

                var commands = new BakerCommands(this, this.GetEntity(TransformUsageFlags.None));
                builder.ApplyTo(ref commands);
            }
        }
    }
}
