// <copyright file="TimerPaused.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data.Schedular
{
    using Unity.Entities;

    /// <summary>Enableable component that indicates whether a timer is currently paused.</summary>
    public struct TimerPaused : IComponentData, IEnableableComponent
    {
    }
}
