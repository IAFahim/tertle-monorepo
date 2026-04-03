// <copyright file="ActiveSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Groups
{
    using BovineLabs.Core;
    using BovineLabs.Reaction.Actives;
    using Unity.Entities;

    /// <summary>
    /// System group responsible for managing active state determination and timer processing for reactions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs in the <see cref="ReactionSystemGroup"/> after <see cref="ConditionsSystemGroup"/>
    /// on server and local worlds only. It processes the core logic for determining when reactions should
    /// be active and manages their duration and cooldown timers.
    /// </para>
    /// <para>
    /// The group contains systems in the following execution order:
    /// 1. <see cref="ActivePreviousSystem"/> - Tracks previous active state for change detection
    /// 2. <see cref="ActiveCancelSystemGroup"/> - Handles cancellation requests
    /// 3. <see cref="ActiveCancelSystem"/> - Processes active cancellations
    /// 4. <see cref="ActiveCooldownSystem"/> - Updates cooldown timers
    /// 5. <see cref="ActiveDurationSystem"/> - Updates duration timers
    /// 6. <see cref="ActiveSystem"/> - Determines final active state
    /// 7. <see cref="ActiveTimerTriggerFilterSystem"/> - Optimizes timer change filters
    /// </para>
    /// <para>
    /// This execution order ensures that:
    /// - Previous state is captured before any changes
    /// - Cancellation requests are processed before timer updates
    /// - Timer states are current when active state is calculated
    /// - Change filter optimization occurs after all timer updates
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
    [UpdateInGroup(typeof(ReactionSystemGroup))]
    [UpdateAfter(typeof(ConditionsSystemGroup))]
    public partial class ActiveSystemGroup : ComponentSystemGroup
    {
    }
}
