// <copyright file="ScheduleSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Schedular
{
    using Unity.Entities;

    /// <summary>
    /// System group responsible for updating timers and clocks.
    /// Updates first within the TimelineSystemGroup to ensure timing data is ready before timeline evaluation.
    /// Contains clock and timer update systems.
    /// </summary>
    [UpdateInGroup(typeof(TimelineSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor, WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class ScheduleSystemGroup : ComponentSystemGroup
    {
    }
}
