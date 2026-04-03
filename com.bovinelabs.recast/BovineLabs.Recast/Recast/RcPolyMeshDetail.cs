// <copyright file="RcPolyMeshDetail.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Mathematics;

    /// <summary>
    /// Contains triangle meshes that represent detailed height data associated
    /// with the polygons in its associated polygon mesh object.
    /// </summary>
    /// <remarks>Memory layout must match C++ rcPolyMeshDetail exactly.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcPolyMeshDetail : IDisposable
    {
        /// <summary>
        /// The sub-mesh data [Size: <see cref="NMeshes"/>].
        /// </summary>
        public uint4* Meshes;

        /// <summary>
        /// The mesh vertices [Size: <see cref="NVerts"/>].
        /// </summary>
        public float3* Verts;

        /// <summary>
        /// The mesh triangles [Size: <see cref="NTris"/>].
        /// </summary>
        public byte4* Tris;

        /// <summary>
        /// The number of sub-meshes defined by meshes.
        /// </summary>
        public int NMeshes;

        /// <summary>
        /// The number of vertices in verts.
        /// </summary>
        public int NVerts;

        /// <summary>
        /// The number of triangles in tris.
        /// </summary>
        public int NTris;

        public RcPolyMeshDetail(Allocator allocator)
        {
            this = default;
            this.Allocator = allocator;
        }

        public Allocator Allocator { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.Meshes != null)
            {
                AllocatorManager.Free(this.Allocator, this.Meshes);
                this.Meshes = null;
            }

            if (this.Verts != null)
            {
                AllocatorManager.Free(this.Allocator, this.Verts);
                this.Verts = null;
            }

            if (this.Tris != null)
            {
                AllocatorManager.Free(this.Allocator, this.Tris);
                this.Tris = null;
            }

            this = default;
        }
    }
}
