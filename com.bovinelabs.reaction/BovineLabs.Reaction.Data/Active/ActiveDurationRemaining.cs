// <copyright file="ActiveDurationRemaining.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Component tracking the remaining duration time for an active reaction before it automatically deactivates.
    /// </summary>
    public struct ActiveDurationRemaining : IComponentData
    {
        public float Value;
    }
}
