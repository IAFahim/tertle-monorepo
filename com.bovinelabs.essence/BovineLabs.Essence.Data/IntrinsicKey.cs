// <copyright file="IntrinsicKey.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using System;
    using BovineLabs.Core.Assertions;

    /// <summary>
    /// A unique identifier for intrinsic types, designed to allow future size increases.
    /// </summary>
    [Serializable]
    public struct IntrinsicKey : IEquatable<ushort>, IEquatable<IntrinsicKey>, IComparable<IntrinsicKey>
    {
        public const int MaxValue = ushort.MaxValue;
        public ushort Value;

        public static implicit operator ushort(IntrinsicKey t)
        {
            return t.Value;
        }

        public static implicit operator IntrinsicKey(ushort b)
        {
            return new IntrinsicKey { Value = b };
        }

        public static implicit operator IntrinsicKey(int b)
        {
            Check.Assume(b is >= 0 and <= MaxValue, "Trying to use a IntrinsicKey out of range");
            return new IntrinsicKey { Value = (ushort)b };
        }

        /// <inheritdoc/>
        public bool Equals(ushort other)
        {
            return this.Value == other;
        }

        /// <inheritdoc/>
        public bool Equals(IntrinsicKey other)
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

        /// <inheritdoc/>
        public int CompareTo(IntrinsicKey other)
        {
            return this.Value.CompareTo(other.Value);
        }
    }
}
