// <copyright file="ReactionSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Groups
{
    using BovineLabs.Core;
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    /// <summary>
    /// Root system group for all reaction-related systems, providing centralized organization and execution order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs in the <see cref="AfterTransformSystemGroup"/> on server and local worlds only,
    /// ensuring that all transform calculations are complete before reaction processing begins.
    /// This timing is critical because many reactions depend on accurate entity positions and orientations.
    /// </para>
    /// <para>
    /// The group contains the following child system groups in execution order:
    /// 1. <see cref="ConditionsSystemGroup"/> - Evaluates conditions and determines reaction activation
    /// 2. <see cref="ActiveSystemGroup"/> - Manages active state and timer processing
    /// 3. <see cref="ActiveDisabledSystemGroup"/> - Processes reactions being deactivated
    /// 4. <see cref="ActiveEnabledSystemGroup"/> - Processes reactions being activated
    /// </para>
    /// <para>
    /// This hierarchical organization ensures proper execution order for reaction processing:
    /// conditions are evaluated first, then active states are determined, and finally
    /// deactivation and activation effects are applied in sequence.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
    [UpdateInGroup(typeof(AfterTransformSystemGroup))]
    public partial class ReactionSystemGroup : ComponentSystemGroup
    {
    }
}
