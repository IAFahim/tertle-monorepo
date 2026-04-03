// <copyright file="RcHeightfieldLayer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary>
    /// Represents a heightfield layer within a layer set.
    /// </summary>
    /// <remarks>Memory layout must match C++ rcHeightfieldLayer exactly.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcHeightfieldLayer : IDisposable
    {
        /// <summary>
        /// The minimum bounds in world space.
        /// </summary>
        public float3 BoundMin;

        /// <summary>
        /// The maximum bounds in world space.
        /// </summary>
        public float3 BoundMax;

        /// <summary>
        /// The size of each cell (on the xz-plane).
        /// </summary>
        public float CellSize;

        /// <summary>
        /// The height of each cell (the minimum increment along the y-axis).
        /// </summary>
        public float CellHeight;

        /// <summary>
        /// The width of the heightfield (along the x-axis in cell units).
        /// </summary>
        public int Width;

        /// <summary>
        /// The height of the heightfield (along the z-axis in cell units).
        /// </summary>
        public int Height;

        /// <summary>
        /// The minimum x-bounds of usable data.
        /// </summary>
        public int MinX;

        /// <summary>
        /// The maximum x-bounds of usable data.
        /// </summary>
        public int MaxX;

        /// <summary>
        /// The minimum y-bounds of usable data (along the z-axis).
        /// </summary>
        public int MinY;

        /// <summary>
        /// The maximum y-bounds of usable data (along the z-axis).
        /// </summary>
        public int MaxY;

        /// <summary>
        /// The minimum height bounds of usable data (along the y-axis).
        /// </summary>
        public int HeightMin;

        /// <summary>
        /// The maximum height bounds of usable data (along the y-axis).
        /// </summary>
        public int HeightMax;

        /// <summary>
        /// The heightfield [Size: width * height].
        /// </summary>
        public byte* Heights;

        /// <summary>
        /// Area ids [Size: Same as heights].
        /// </summary>
        public byte* Areas;

        /// <summary>
        /// Packed neighbor connection information [Size: Same as heights].
        /// </summary>
        public byte* Cons;

        public RcHeightfieldLayer(Allocator allocator)
        {
            this = default;
            this.Allocator = allocator;
        }

        public Allocator Allocator { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.Heights != null)
            {
                AllocatorManager.Free(this.Allocator, this.Heights);
                this.Heights = null;
            }

            if (this.Areas != null)
            {
                AllocatorManager.Free(this.Allocator, this.Areas);
                this.Areas = null;
            }

            if (this.Cons != null)
            {
                AllocatorManager.Free(this.Allocator, this.Cons);
                this.Cons = null;
            }

            this = default;
        }
    }
}
