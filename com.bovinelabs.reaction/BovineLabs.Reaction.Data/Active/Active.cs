// <copyright file="Active.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Enableable component indicating whether a reaction is currently active and executing its actions.
    /// </summary>
    public struct Active : IComponentData, IEnableableComponent
    {
    }
}
