// <copyright file="ActiveCooldownRemaining.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Component tracking the remaining cooldown time before a reaction can be triggered again.
    /// </summary>
    public struct ActiveCooldownRemaining : IComponentData
    {
        public float Value;
    }
}
