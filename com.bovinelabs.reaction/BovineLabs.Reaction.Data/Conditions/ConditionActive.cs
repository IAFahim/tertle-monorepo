// <copyright file="ConditionActive.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Reaction.Data.Builders;
    using Unity.Entities;

    /// <summary>
    /// Component data tracking the active state of up to 32 individual conditions using a bit array.
    ///
    /// <para>
    /// Important: When created through <see cref="ConditionBuilder"/>, unused condition bits
    /// (those beyond the actual condition count) are automatically set to <c>true</c> using
    /// <c>uint.MaxValue &lt;&lt; conditionCount</c>. This ensures that <see cref="BitArray32.AllTrue"/>
    /// correctly evaluates to <c>true</c> when all defined conditions are active, regardless of
    /// how many conditions are actually used (1-32).
    /// </para>
    ///
    /// <para>
    /// For example, if only 3 conditions are defined, bits 0-2 represent the actual conditions,
    /// while bits 3-31 are automatically set to <c>true</c> by the builder. This allows
    /// using <see cref="BitArray32.AllTrue"/> to check if all defined conditions are active.
    /// </para>
    /// </summary>
    [WriteGroup(typeof(ConditionAllActive))]
    public struct ConditionActive : IComponentData
    {
        public const int MaxConditions = 32;

        /// <summary>
        /// Bit array where each bit represents the active state of a condition.
        /// Unused bits (beyond the actual condition count) are set to true by <see cref="ConditionBuilder"/>.
        /// </summary>
        public BitArray32 Value;
    }
}
