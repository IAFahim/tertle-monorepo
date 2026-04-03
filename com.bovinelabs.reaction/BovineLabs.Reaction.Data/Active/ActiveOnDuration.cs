// <copyright file="ActiveOnDuration.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Enableable component marking reactions that are currently active with a duration timer running.
    /// </summary>
    public struct ActiveOnDuration : IComponentData, IEnableableComponent
    {
    }
}
