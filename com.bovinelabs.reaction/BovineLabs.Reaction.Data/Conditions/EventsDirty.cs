// <copyright file="EventsDirty.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using Unity.Entities;

    /// <summary>
    /// Enableable component marking entities with event-based conditions that need processing.
    /// </summary>
    public struct EventsDirty : IComponentData, IEnableableComponent
    {
    }
}
