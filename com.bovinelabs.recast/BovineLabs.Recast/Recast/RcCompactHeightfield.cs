// <copyright file="RcCompactHeightfield.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary> A compact, static heightfield representing unobstructed space. </summary>
    /// <remarks>Memory layout must match C++ rcCompactHeightfield exactly.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcCompactHeightfield : IDisposable
    {
        /// <summary>
        /// The width of the heightfield (along the x-axis in cell units).
        /// </summary>
        public int Width;

        /// <summary>
        /// The height of the heightfield (along the z-axis in cell units).
        /// </summary>
        public int Height;

        /// <summary>
        /// The number of spans in the heightfield.
        /// </summary>
        public int SpanCount;

        /// <summary>
        /// The walkable height used during the build of the field.
        /// </summary>
        public int WalkableHeight;

        /// <summary>
        /// The walkable climb used during the build of the field.
        /// </summary>
        public int WalkableClimb;

        /// <summary>
        /// The AABB border size used during the build of the field.
        /// </summary>
        public int BorderSize;

        /// <summary>
        /// The maximum distance value of any span within the field.
        /// </summary>
        public ushort MaxDistance;

        /// <summary>
        /// The maximum region id of any span within the field.
        /// </summary>
        public ushort MaxRegions;

        /// <summary>
        /// The minimum bounds in world space.
        /// </summary>
        public float3 BMin;

        /// <summary>
        /// The maximum bounds in world space.
        /// </summary>
        public float3 BMax;

        /// <summary>
        /// The size of each cell (on the xz-plane).
        /// </summary>
        public float CellSize;

        /// <summary>
        /// The height of each cell (the minimum increment along the y-axis).
        /// </summary>
        public float CellHeight;

        /// <summary>
        /// Array of cells [Size: <see cref="Width"/>*<see cref="Height"/>].
        /// </summary>
        public RcCompactCell* Cells;

        /// <summary>
        /// Array of spans [Size: <see cref="SpanCount"/>].
        /// </summary>
        public RcCompactSpan* Spans;

        /// <summary>
        /// Array containing border distance data [Size: <see cref="SpanCount"/>].
        /// </summary>
        public ushort* Dist;

        /// <summary>
        /// Array containing area id data [Size: <see cref="SpanCount"/>].
        /// </summary>
        public byte* Areas;

        public RcCompactHeightfield(Allocator allocator)
        {
            this = default;
            this.Allocator = allocator;
        }

        public Allocator Allocator { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.Cells != null)
            {
                AllocatorManager.Free(this.Allocator, this.Cells);
                this.Cells = null;
            }

            if (this.Spans != null)
            {
                AllocatorManager.Free(this.Allocator, this.Spans);
                this.Spans = null;
            }

            if (this.Dist != null)
            {
                AllocatorManager.Free(this.Allocator, this.Dist);
                this.Dist = null;
            }

            if (this.Areas != null)
            {
                AllocatorManager.Free(this.Allocator, this.Areas);
                this.Areas = null;
            }

            this = default;
        }
    }
}
