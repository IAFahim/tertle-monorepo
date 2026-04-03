// <copyright file="ActivePrevious.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Enableable component tracking the previous active state of a reaction for state change detection.
    /// </summary>
    public struct ActivePrevious : IComponentData, IEnableableComponent
    {
    }
}
