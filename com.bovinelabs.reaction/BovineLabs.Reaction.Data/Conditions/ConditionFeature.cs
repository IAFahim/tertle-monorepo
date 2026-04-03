// <copyright file="ConditionFeature.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using System;
    using System.Runtime.CompilerServices;

    [Flags]
    public enum ConditionFeature : byte
    {
        /// <summary> Using this is an error. </summary>
        Invalid = 0,

        /// <summary> Condition will be used in <see cref="ConditionActive"/> to check if active. </summary>
        Condition = 1 << 0,

        /// <summary> Condition value will be recorded into <see cref="ConditionValues"/>. </summary>
        Value = 1 << 1,

        /// <summary>
        /// Instead of replacing, each event will accumulate it's value in <see cref="ConditionValues"/>. This only works for Events not States.
        /// </summary>
        Accumulate = 1 << 2 | Condition | Value,
    }

    /// <summary>
    /// Static extension methods for ConditionFeature enum providing utility methods to check feature flags.
    /// </summary>
    public static class ConditionFeatureExtensions
    {
        /// <summary>
        /// Determines whether the specified feature includes condition checking functionality.
        /// </summary>
        /// <param name="feature">The condition feature to check.</param>
        /// <returns>True if the feature includes condition checking; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasCondition(this ConditionFeature feature)
        {
            return (feature & ConditionFeature.Condition) != 0;
        }

        /// <summary>
        /// Determines whether the specified feature includes value recording functionality.
        /// </summary>
        /// <param name="feature">The condition feature to check.</param>
        /// <returns>True if the feature includes value recording; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasValue(this ConditionFeature feature)
        {
            return (feature & ConditionFeature.Value) != 0;
        }

        /// <summary>
        /// Determines whether the specified feature is configured for accumulating values.
        /// </summary>
        /// <param name="feature">The condition feature to check.</param>
        /// <returns>True if the feature accumulates values; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAccumulate(this ConditionFeature feature)
        {
            return (feature & ConditionFeature.Accumulate) == ConditionFeature.Accumulate;
        }
    }
}
