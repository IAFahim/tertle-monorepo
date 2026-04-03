// <copyright file="RcContour.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary>
    /// Represents a simple, non-overlapping contour in field space.
    /// </summary>
    /// <remarks>Memory layout must match C++ rcContour exactly.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcContour : IDisposable
    {
        /// <summary>
        /// Simplified contour vertex and connection data [One element per <see cref="NVerts"/>].
        /// </summary>
        public int4* Verts;

        /// <summary>
        /// The number of vertices in the simplified contour.
        /// </summary>
        public int NVerts;

        /// <summary>
        /// Raw contour vertex and connection data [One element per <see cref="NRVerts"/>].
        /// </summary>
        public int4* RVerts;

        /// <summary>
        /// The number of vertices in the raw contour.
        /// </summary>
        public int NRVerts;

        /// <summary>
        /// The region id of the contour.
        /// </summary>
        public ushort Reg;

        /// <summary>
        /// The area id of the contour.
        /// </summary>
        public byte Area;

        public RcContour(Allocator allocator)
        {
            this = default;
            this.Allocator = allocator;
        }

        public Allocator Allocator { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.Verts != null)
            {
                AllocatorManager.Free(this.Allocator, this.Verts);
                this.Verts = null;
            }

            if (this.RVerts != null)
            {
                AllocatorManager.Free(this.Allocator, this.RVerts);
                this.RVerts = null;
            }

            this = default;
        }
    }
}
