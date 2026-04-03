// <copyright file="ActiveCancelSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Groups
{
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Active;
    using Unity.Entities;

    /// <summary>
    /// System group for handling cancellation requests for active reactions before they are processed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs in the <see cref="ActiveSystemGroup"/> before <see cref="ActiveCancelSystem"/>
    /// on server and local worlds only. It provides an organized location for systems that need to
    /// request cancellation of active reactions based on various criteria.
    /// </para>
    /// <para>
    /// The group contains systems that enable the <see cref="ActiveCancel"/> component:
    /// - <see cref="ConditionCancelActiveSystem"/> - Cancels reactions when conditions are no longer met
    /// - Other systems that may request reaction cancellation based on game logic
    /// </para>
    /// <para>
    /// This execution order ensures that:
    /// 1. Cancellation requests are collected and processed first
    /// 2. <see cref="ActiveCancelSystem"/> then processes all cancellation requests
    /// 3. Timer systems update with the cancelled durations
    /// 4. <see cref="ActiveSystem"/> determines final active state
    /// </para>
    /// <para>
    /// This design allows multiple systems to request cancellation independently while ensuring
    /// all requests are processed in a single, coordinated manner by <see cref="ActiveCancelSystem"/>.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(TimerSystemGroup))]
    [UpdateAfter(typeof(ActiveDurationSystem))]
    [UpdateBefore(typeof(ActiveCancelSystem))]
    public partial class ActiveCancelSystemGroup : ComponentSystemGroup
    {
    }
}
