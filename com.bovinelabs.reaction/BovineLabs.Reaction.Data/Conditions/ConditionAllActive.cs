// <copyright file="ConditionAllActive.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using Unity.Entities;

    /// <summary>
    /// Enableable component indicating when all conditions for a reaction are active and the reaction should trigger.
    /// </summary>
    public struct ConditionAllActive : IComponentData, IEnableableComponent
    {
    }
}
