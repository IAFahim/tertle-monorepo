// <copyright file="GlobalConditionsSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Groups
{
    using BovineLabs.Core;
    using BovineLabs.Reaction.Conditions;
    using Unity.Entities;

    /// <summary>
    /// System group for processing global condition updates that affect condition evaluation across multiple entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs first in the <see cref="ConditionsSystemGroup"/> (OrderFirst = true)
    /// on server and local worlds only. It ensures that global condition data is updated before
    /// any local condition evaluation occurs.
    /// </para>
    /// <para>
    /// Global conditions are special condition types that:
    /// - Are managed by dedicated global entities rather than local targets
    /// - Can be referenced by multiple reaction entities simultaneously
    /// - Provide shared state that affects condition evaluation across the system
    /// - Are registered and managed through <see cref="ConditionInitializeSystem"/>
    /// </para>
    /// <para>
    /// This system group contains systems that update global condition states before local
    /// condition evaluation. By running first, it ensures that global condition data is
    /// current when <see cref="ConditionAllActiveSystem"/> evaluates conditions that depend
    /// on global state.
    /// </para>
    /// <para>
    /// The separation of global and local condition processing allows for optimized
    /// condition evaluation where global state changes can be processed once and then
    /// referenced by multiple dependent reactions.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
    [UpdateInGroup(typeof(ConditionsSystemGroup), OrderFirst = true)]
    public partial class GlobalConditionsSystemGroup : ComponentSystemGroup
    {
    }
}
