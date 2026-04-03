// <copyright file="RcHeightField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary> A dynamic heightfield representing obstructed space. </summary>
    /// <remarks> Memory layout must match C++ rcHeightfield exactly. </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcHeightfield : IDisposable
    {
        /// <summary>
        /// Width of heightfield (x-axis in cell units).
        /// </summary>
        public int Width;

        /// <summary>
        /// Height of heightfield (z-axis in cell units).
        /// </summary>
        public int Height;

        /// <summary>
        /// Minimum bounds in world space.
        /// </summary>
        public float3 Bmin;

        /// <summary>
        /// Maximum bounds in world space.
        /// </summary>
        public float3 Bmax;

        /// <summary>
        /// Size of each cell (xz-plane).
        /// </summary>
        public float Cs;

        /// <summary>
        /// Height of each cell (y-axis increment).
        /// </summary>
        public float Ch;

        /// <summary>
        /// Heightfield of spans (width*height).
        /// </summary>
        public RcSpan** Spans;

        /// <summary>
        /// Linked list of span pools.
        /// </summary>
        public RcSpanPool* Pools;

        /// <summary>
        /// Next free span.
        /// </summary>
        public RcSpan* Freelist;

        public RcHeightfield(Allocator allocator)
        {
            this = default;
            this.Allocator = allocator;
        }

        public Allocator Allocator { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Free all span pools
            while (this.Pools != null)
            {
                var next = this.Pools->Next;
                AllocatorManager.Free(this.Allocator, this.Pools);
                this.Pools = next;
            }

            // Free spans array
            if (this.Spans != null)
            {
                AllocatorManager.Free(this.Allocator, this.Spans);
            }

            this = default;
        }
    }
}