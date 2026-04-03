// <copyright file="ActiveOnCooldown.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Enableable component marking reactions that are currently on cooldown and cannot be triggered.
    /// </summary>
    public struct ActiveOnCooldown : IComponentData, IEnableableComponent
    {
    }
}
