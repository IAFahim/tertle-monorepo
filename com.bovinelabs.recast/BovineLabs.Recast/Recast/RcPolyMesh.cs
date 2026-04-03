// <copyright file="RcPolyMesh.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary>
    /// Represents a polygon mesh suitable for use in building a navigation mesh.
    /// </summary>
    /// <remarks>Memory layout must match C++ rcPolyMesh exactly.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcPolyMesh : IDisposable
    {
        /// <summary>
        /// The mesh vertices [Form: (x, y, z) * <see cref="NVerts"/>].
        /// </summary>
        public ushort3* Verts;

        /// <summary>
        /// Polygon and neighbor data [Length: <see cref="MaxPolys"/> * 2 * nvp].
        /// </summary>
        public ushort* Polys;

        /// <summary>
        /// The region id assigned to each polygon [Length: <see cref="MaxPolys"/>].
        /// </summary>
        public ushort* Regs;

        /// <summary>
        /// The user defined flags for each polygon [Length: <see cref="MaxPolys"/>].
        /// </summary>
        public ushort* Flags;

        /// <summary>
        /// The area id assigned to each polygon [Length: <see cref="MaxPolys"/>].
        /// </summary>
        public byte* Areas;

        /// <summary>
        /// The number of vertices.
        /// </summary>
        public int NVerts;

        /// <summary>
        /// The number of polygons.
        /// </summary>
        public int NPolys;

        /// <summary>
        /// The number of allocated polygons.
        /// </summary>
        public int MaxPolys;

        /// <summary>
        /// The maximum number of vertices per polygon.
        /// </summary>
        public int Nvp;

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
        /// The AABB border size used to generate the source data.
        /// </summary>
        public int BorderSize;

        /// <summary>
        /// The max error of the polygon edges in the mesh.
        /// </summary>
        public float MaxEdgeError;

        public RcPolyMesh(Allocator allocator)
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

            if (this.Polys != null)
            {
                AllocatorManager.Free(this.Allocator, this.Polys);
                this.Polys = null;
            }

            if (this.Regs != null)
            {
                AllocatorManager.Free(this.Allocator, this.Regs);
                this.Regs = null;
            }

            if (this.Flags != null)
            {
                AllocatorManager.Free(this.Allocator, this.Flags);
                this.Flags = null;
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
