// <copyright file="ActiveCooldown.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Component defining the cooldown duration that must elapse before a reaction can be triggered again.
    /// </summary>
    public struct ActiveCooldown : IComponentData
    {
        public float Value;
    }
}
