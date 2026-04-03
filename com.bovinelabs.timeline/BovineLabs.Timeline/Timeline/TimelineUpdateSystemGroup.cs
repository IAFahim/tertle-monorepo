// <copyright file="TimelineUpdateSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using BovineLabs.Timeline.Schedular;
    using Unity.Entities;

    /// <summary>
    /// System group responsible for updating timeline clip state and weights.
    /// Updates after ScheduleSystemGroup to use updated timer data.
    /// Contains systems for calculating clip local time, weights, and active states.
    /// </summary>
    [UpdateAfter(typeof(ScheduleSystemGroup))]
    [UpdateInGroup(typeof(TimelineSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor, WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class TimelineUpdateSystemGroup : ComponentSystemGroup
    {
    }
}
