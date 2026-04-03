// <copyright file="TimerSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Groups
{
    using BovineLabs.Reaction.Actives;
    using Unity.Entities;

    /// <summary>
    /// System group responsible for managing timer-related systems within the active reaction lifecycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs in the <see cref="ActiveSystemGroup"/> before <see cref="ActiveSystem"/>
    /// on server and local worlds only. It processes all timer-based components for reactions,
    /// ensuring that duration and cooldown states are updated before the final active state calculation.
    /// </para>
    /// <para>
    /// The group contains the following systems in execution order:
    /// 1. <see cref="ActiveCooldownSystem"/> - Updates cooldown timers and prevents retriggering
    /// 2. <see cref="ActiveDurationSystem"/> - Updates duration timers for time-limited reactions
    /// 3. <see cref="ActiveTimerTriggerFilterSystem"/> - Optimizes timer change filters for performance
    /// </para>
    /// <para>
    /// This execution order ensures that:
    /// - Cooldown timers are processed first to block unwanted activations
    /// - Duration timers are updated to maintain accurate timing state
    /// - Change filter optimization occurs after all timer updates for maximum efficiency
    /// - All timer states are current when <see cref="ActiveSystem"/> calculates final active state
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveSystemGroup))]
    [UpdateAfter(typeof(ActiveSystem))]
    public partial class TimerSystemGroup : ComponentSystemGroup
    {
    }
}
