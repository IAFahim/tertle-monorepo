// <copyright file="InitializeStatsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Authoring.Core;
    using BovineLabs.Reaction.Data.Core;
    using UnityEngine;

    /// <summary>
    /// Authoring component for initializing stats from a source entity.
    /// This component configures an entity to copy stats from another entity (typically the owner) during initialization.
    /// Requires a TargetsAuthoring component to define the source target.
    /// </summary>
    [RequireComponent(typeof(TargetsAuthoring))]
    public class InitializeStatsAuthoring : LookupAuthoring<InitializeStats, InitializeStats.Data>
    {
        public Target Source = Target.Owner;

        /// <inheritdoc/>
        public override bool TryGetInitialization(out InitializeStats.Data value)
        {
            value = new InitializeStats.Data { Source = this.Source };
            return true;
        }
    }
}
