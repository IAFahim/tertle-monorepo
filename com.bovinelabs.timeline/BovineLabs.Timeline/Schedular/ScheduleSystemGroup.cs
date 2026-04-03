// <copyright file="ScheduleSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Schedular
{
    using BovineLabs.Core;
    using Unity.Entities;

    /// <summary>
    /// System group responsible for updating timers and clocks.
    /// Updates first within the TimelineSystemGroup to ensure timing data is ready before timeline evaluation.
    /// Contains clock and timer update systems.
    /// </summary>
    [UpdateInGroup(typeof(TimelineSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | Worlds.Menu,
        WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | Worlds.Menu)]
    public partial class ScheduleSystemGroup : ComponentSystemGroup
    {
    }
}
