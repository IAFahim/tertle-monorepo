// <copyright file="DtPolyRef.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using Unity.Collections.LowLevel.Unsafe;
#if DT_POLYREF32
    [Serializable]
    public readonly struct DtPolyRef : IEquatable<DtPolyRef>
    {
        internal readonly uint Value;

        private DtPolyRef(uint value)
        {
            this.Value = value;
        }

        public static implicit operator uint(DtPolyRef polyRef)
        {
            return polyRef.Value;
        }

        public static implicit operator DtPolyRef(uint polyRef)
        {
            return new DtPolyRef(polyRef);
        }

        public static implicit operator DtPolyRef(int polyRef)
        {
            return (uint)polyRef;
        }

        public static implicit operator DtPolyRef(DtTileRef dtTileRef)
        {
            return new DtPolyRef(dtTileRef.Value);
        }

        public static bool operator ==(DtPolyRef left, DtPolyRef right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DtPolyRef left, DtPolyRef right)
        {
            return !left.Equals(right);
        }

        public static ref DtPolyRef From(ref uint i)
        {
            return ref UnsafeUtility.As<uint, DtPolyRef>(ref i);
        }

        public override bool Equals(object obj)
        {
            return obj is DtPolyRef other && this.Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(DtPolyRef other)
        {
            return this.Value == other.Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)this.Value;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.Value}";
        }
    }
#else
    [Serializable]
    public readonly struct DtPolyRef : IEquatable<DtPolyRef>
    {
        internal readonly ulong Value;

        private DtPolyRef(ulong value)
        {
            this.Value = value;
        }

        public static implicit operator ulong(DtPolyRef polyRef)
        {
            return polyRef.Value;
        }

        public static implicit operator DtPolyRef(ulong polyRef)
        {
            return new DtPolyRef(polyRef);
        }

        public static implicit operator DtPolyRef(int polyRef)
        {
            return new DtPolyRef((uint)polyRef);
        }

        public static implicit operator DtPolyRef(DtTileRef dtTileRef)
        {
            return new DtPolyRef(dtTileRef.Value);
        }

        public static bool operator ==(DtPolyRef left, DtPolyRef right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DtPolyRef left, DtPolyRef right)
        {
            return !left.Equals(right);
        }

        public static ref DtPolyRef From(ref ulong i)
        {
            return ref UnsafeUtility.As<ulong, DtPolyRef>(ref i);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is DtPolyRef other && this.Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(DtPolyRef other)
        {
            return this.Value == other.Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.Value}";
        }
    }
#endif
}