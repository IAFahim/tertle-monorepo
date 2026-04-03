// <copyright file="RcSpanPool.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary> A memory pool used for quick allocation of spans within a heightfield. </summary>
    /// <remarks> Memory layout must match C++ rcSpanPool exactly. </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcSpanPool
    {
        /// <summary>
        /// Pointer to the next span pool in the linked list.
        /// </summary>
        public RcSpanPool* Next;

        /// <summary>
        /// Fixed array of span items [RcSpan items[RC_SPANS_PER_POOL]].
        /// </summary>
        public fixed byte Items[Recast.RCSpanPoolItemsSize];

        public RcSpan* GetSpan(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index is < 0 or >= Recast.RCSpansPerPool)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
#endif
            fixed (byte* ptr = this.Items)
            {
                return (RcSpan*)ptr + index;
            }
        }
    }
}