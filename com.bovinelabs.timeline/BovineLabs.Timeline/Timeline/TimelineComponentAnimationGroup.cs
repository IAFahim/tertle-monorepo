// <copyright file="TimelineComponentAnimationGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Entities;

    /// <summary>
    /// System group for custom timeline track systems that animate components.
    /// Updates after TimelineUpdateSystemGroup to apply animation values to components.
    /// User-defined track systems should update in this group to blend and apply values from timeline clips.
    /// </summary>
    [UpdateInGroup(typeof(TimelineSystemGroup))]
    [UpdateAfter(typeof(TimelineUpdateSystemGroup))]
    public partial class TimelineComponentAnimationGroup : ComponentSystemGroup
    {
    }
}
