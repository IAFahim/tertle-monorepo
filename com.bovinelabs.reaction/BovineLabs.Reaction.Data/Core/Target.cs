// <copyright file="Target.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Core
{
    /// <summary>
    /// Defines the target entities that can be referenced in reaction systems for conditions and actions.
    /// </summary>
    public enum Target : byte
    {
        /// <summary>No target specified.</summary>
        None = 0,

        /// <summary>The primary target entity for the reaction.</summary>
        Target = 1,

        /// <summary>The entity that owns or contains the reaction component.</summary>
        Owner = 2,

        /// <summary>The entity that triggered or initiated the reaction.</summary>
        Source = 3,

        /// <summary>The entity itself (same as Owner in most contexts).</summary>
        Self = 4,

        /// <summary>First custom target slot for user-defined targeting.</summary>
        Custom0 = 6,

        /// <summary>Second custom target slot for user-defined targeting.</summary>
        Custom1 = 7,
    }
}
