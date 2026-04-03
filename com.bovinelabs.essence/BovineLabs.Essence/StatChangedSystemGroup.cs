// <copyright file="StatChangedSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence
{
    using BovineLabs.Reaction.Groups;
    using Unity.Entities;

    /// <summary>
    /// Coordinates the processing of stat changes in a specific order to ensure consistency.
    /// This system group contains all systems that react to stat modifications and ensures they execute in the correct sequence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs in the ReactionSystemGroup after both ActiveEnabledSystemGroup and 
    /// ActiveDisabledSystemGroup to ensure all action-based stat modifications have been applied.
    /// It coordinates the execution of stat calculation, intrinsic validation, and cleanup systems.
    /// </para>
    /// <para>
    /// The processing order within this group:
    /// 1. StatCalculationSystem - Recalculates final stat values (OrderFirst)
    /// 2. IntrinsicValidationSystem - Validates intrinsic ranges against new stat values (OrderLast)  
    /// 3. StatChangedResetSystem - Resets StatChanged flags for next frame (OrderLast)
    /// </para>
    /// </remarks>
    [UpdateAfter(typeof(ActiveEnabledSystemGroup))]
    [UpdateAfter(typeof(ActiveDisabledSystemGroup))] // TODO maybe this should be before conditions? undecided
    [UpdateInGroup(typeof(ReactionSystemGroup))]
    public partial class StatChangedSystemGroup : ComponentSystemGroup
    {
    }
}
