// <copyright file="TimelineActivePrevious.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;

    /// <summary> Enableable component that tracks the previous frame's timeline active state. </summary>
    public struct TimelineActivePrevious : IComponentData, IEnableableComponent
    {
    }
}
