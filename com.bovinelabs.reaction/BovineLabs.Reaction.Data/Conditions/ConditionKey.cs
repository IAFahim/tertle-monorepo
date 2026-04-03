// <copyright file="ConditionKey.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using System;
    using BovineLabs.Core.Assertions;

    /// <summary>
    /// The condition type key. This is just a single field wrapped in a struct to easily allow size increases in future if required.
    /// </summary>
    [Serializable]
    public struct ConditionKey : IEquatable<ushort>, IEquatable<ConditionKey>
    {
        public const int MaxValue = ushort.MaxValue;
        public static readonly ConditionKey Null = 0;

        public ushort Value;

        /// <summary>
        /// Implicitly converts a ushort value to a ConditionKey.
        /// </summary>
        /// <param name="b">The ushort value to convert.</param>
        /// <returns>A new ConditionKey with the specified value.</returns>
        public static implicit operator ConditionKey(ushort b)
        {
            return new ConditionKey { Value = b };
        }

        /// <summary>
        /// Implicitly converts an int value to a ConditionKey.
        /// </summary>
        /// <param name="b">The int value to convert (must be 0-65535).</param>
        /// <returns>A new ConditionKey with the specified value.</returns>
        public static implicit operator ConditionKey(int b)
        {
            Check.Assume(b is >= 0 and <= MaxValue, "Trying to use a ConditionKey out of range");
            return new ConditionKey { Value = (ushort)b };
        }

        /// <summary>
        /// Implicitly converts a ConditionKey to its underlying ushort value.
        /// </summary>
        /// <param name="t">The ConditionKey to convert.</param>
        /// <returns>The underlying ushort value.</returns>
        public static implicit operator ushort(ConditionKey t)
        {
            return t.Value;
        }

        /// <inheritdoc/>
        public bool Equals(ushort other)
        {
            return this.Value == other;
        }

        /// <inheritdoc/>
        public bool Equals(ConditionKey other)
        {
            return this.Value == other.Value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ConditionKey {this.Value.ToString()}";
        }
    }
}
