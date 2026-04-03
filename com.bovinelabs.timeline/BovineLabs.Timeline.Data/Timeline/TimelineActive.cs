// <copyright file="TimelineActive.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;

    /// <summary> Enableable component that indicates whether a timeline is currently active. </summary>
    public struct TimelineActive : IComponentData, IEnableableComponent
    {
    }
}
