// <copyright file="ConditionWriteEventsGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Groups
{
    using BovineLabs.Core;
    using BovineLabs.Reaction.Conditions;
    using Unity.Entities;

    /// <summary>
    /// System group for processing event-based condition data before condition evaluation occurs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs in the <see cref="ConditionsSystemGroup"/> before <see cref="ConditionAllActiveSystem"/>
    /// on server and local worlds only. It ensures that event-based condition data is processed
    /// and written to condition states before any condition evaluation occurs.
    /// </para>
    /// <para>
    /// The group contains systems that handle event-driven condition updates:
    /// - <see cref="ConditionEventWriteSystem"/> - Processes events and updates condition states
    /// - Other systems that may write event-based condition data
    /// </para>
    /// <para>
    /// Event-based conditions differ from continuous conditions in that they:
    /// - Are triggered by discrete events rather than continuous state
    /// - May only be active for a single frame
    /// - Require processing of event data before condition evaluation
    /// - May accumulate values or perform complex event matching
    /// </para>
    /// <para>
    /// This execution order ensures that:
    /// 1. Event data is processed and condition states are updated
    /// 2. <see cref="ConditionAllActiveSystem"/> evaluates conditions with current event data
    /// 3. Event-based conditions are properly integrated with other condition types
    /// </para>
    /// <para>
    /// The separation allows for optimized event processing while maintaining integration
    /// with the broader condition evaluation system.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
    [UpdateInGroup(typeof(ConditionsSystemGroup))]
    [UpdateBefore(typeof(ConditionAllActiveSystem))]
    public partial class ConditionWriteEventsGroup : ComponentSystemGroup
    {
    }
}
