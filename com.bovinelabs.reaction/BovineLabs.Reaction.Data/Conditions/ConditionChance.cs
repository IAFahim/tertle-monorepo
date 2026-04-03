// <copyright file="ConditionChance.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using Unity.Entities;

    /// <summary>
    /// Component allowing less than 100% chance of triggering ConditionAllActive reactions with configurable probability.
    /// </summary>
    [WriteGroup(typeof(ConditionAllActive))]
    public struct ConditionChance : IComponentData
    {
        public const int Multi = 10000;

        /// <summary> Value between 0 and <see cref="Multi"/>. </summary>
        public ushort Value;
    }
}
