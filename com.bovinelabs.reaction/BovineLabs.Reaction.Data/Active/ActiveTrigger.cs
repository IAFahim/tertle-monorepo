// <copyright file="ActiveTrigger.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Enableable component requiring an additional manual trigger activation before the reaction can activate.
    /// </summary>
    public struct ActiveTrigger : IComponentData, IEnableableComponent
    {
    }
}
