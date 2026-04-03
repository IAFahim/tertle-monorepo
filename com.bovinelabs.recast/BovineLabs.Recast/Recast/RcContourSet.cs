// <copyright file="RcContourSet.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary>
    /// Represents a group of related contours.
    /// </summary>
    /// <remarks>Memory layout must match C++ rcContourSet exactly.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcContourSet : IDisposable
    {
        /// <summary>
        /// An array of the contours in the set [Size: Nconts].
        /// </summary>
        public RcContour* Conts;

        /// <summary>
        /// The number of contours in the set.
        /// </summary>
        public int Nconts;

        /// <summary>
        /// The minimum bounds in world space.
        /// </summary>
        public float3 Bmin;

        /// <summary>
        /// The maximum bounds in world space.
        /// </summary>
        public float3 Bmax;

        /// <summary>
        /// The size of each cell (on the xz-plane).
        /// </summary>
        public float Cs;

        /// <summary>
        /// The height of each cell (the minimum increment along the y-axis).
        /// </summary>
        public float Ch;

        /// <summary>
        /// The width of the set (along the x-axis in cell units).
        /// </summary>
        public int Width;

        /// <summary>
        /// The height of the set (along the z-axis in cell units).
        /// </summary>
        public int Height;

        /// <summary>
        /// The AABB border size used to generate the source data.
        /// </summary>
        public int BorderSize;

        /// <summary>
        /// The max edge error that this contour set was simplified with.
        /// </summary>
        public float MaxError;

        public RcContourSet(Allocator allocator)
        {
            this = default;
            this.Allocator = allocator;
        }

        public Allocator Allocator { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.Conts != null)
            {
                for (var i = 0; i < this.Nconts; i++)
                {
                    this.Conts[i].Dispose();
                }

                AllocatorManager.Free(this.Allocator, this.Conts);
                this.Conts = null;
            }

            this = default;
        }
    }
}
