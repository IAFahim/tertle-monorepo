// <copyright file="StatKey.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using System;
    using BovineLabs.Core.Assertions;

    /// <summary>
    /// A unique identifier for stat types, designed to allow future size increases.
    /// </summary>
    [Serializable]
    public struct StatKey : IEquatable<ushort>, IEquatable<StatKey>
    {
        public ushort Value;

        public static implicit operator ushort(StatKey t)
        {
            return t.Value;
        }

        public static implicit operator StatKey(ushort b)
        {
            return new StatKey { Value = b };
        }

        public static implicit operator StatKey(int b)
        {
            Check.Assume(b is >= 0 and <= ushort.MaxValue, "Trying to use a StatKey out of range");
            return new StatKey { Value = (ushort)b };
        }

        /// <inheritdoc/>
        public bool Equals(ushort other)
        {
            return this.Value == other;
        }

        /// <inheritdoc/>
        public bool Equals(StatKey other)
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
            return this.Value.ToString();
        }
    }
}
