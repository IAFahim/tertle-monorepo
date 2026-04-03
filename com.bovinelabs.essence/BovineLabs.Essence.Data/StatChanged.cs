// <copyright file="StatChanged.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using Unity.Entities;

    /// <summary>
    /// A tag component indicating that stat values have changed and need to be synchronized or processed.
    /// </summary>
    public struct StatChanged : IComponentData, IEnableableComponent
    {
    }
}
