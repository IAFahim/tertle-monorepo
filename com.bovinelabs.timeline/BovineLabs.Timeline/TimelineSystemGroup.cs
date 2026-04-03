// <copyright file="TimelineSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using BovineLabs.Core;
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    /// <summary>
    /// Root system group for all timeline-related systems.
    /// Updates in the BeforeTransformSystemGroup to ensure timeline data is processed before transforms.
    /// Contains both the Schedule and Timeline update systems.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | Worlds.Menu,
        WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | Worlds.Menu)]
    [UpdateInGroup(typeof(BeforeTransformSystemGroup))]
    public partial class TimelineSystemGroup : ComponentSystemGroup
    {
    }
}
