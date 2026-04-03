// <copyright file="ClipActivePrevious.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;

    /// <summary> Enableable component that tracks the previous frame's clip active state. </summary>
    public struct ClipActivePrevious : IComponentData, IEnableableComponent
    {
    }
}
