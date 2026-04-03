// <copyright file="ConditionsSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Groups
{
    using BovineLabs.Core;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Entities;

    /// <summary>
    /// System group responsible for evaluating conditions and determining which reactions should be eligible for activation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs first in the <see cref="ReactionSystemGroup"/> on server and local worlds only,
    /// ensuring that condition evaluation completes before active state determination and action processing.
    /// This timing is critical for the reaction system's dependency chain.
    /// </para>
    /// <para>
    /// The group contains condition-related systems in the following execution order:
    /// 1. <see cref="GlobalConditionsSystemGroup"/> - Processes global condition updates
    /// 2. <see cref="ConditionWriteEventsGroup"/> - Writes event-based condition data
    /// 3. <see cref="ConditionAllActiveSystem"/> - Evaluates condition logic and sets results
    /// 4. <see cref="ConditionEventResetSystem"/> - Resets temporary condition states
    /// </para>
    /// <para>
    /// This execution order ensures that:
    /// - Global conditions are updated before local condition evaluation
    /// - Event data is written before condition evaluation uses it
    /// - Condition logic is evaluated with current data
    /// - Temporary conditions are reset after evaluation
    /// </para>
    /// <para>
    /// The output of this system group (<see cref="ConditionAllActive"/> states) drives the
    /// <see cref="ActiveSystemGroup"/> to determine which reactions should be active.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
    [UpdateInGroup(typeof(ReactionSystemGroup))]
    public partial class ConditionsSystemGroup : ComponentSystemGroup
    {
    }
}
