// <copyright file="DtNavMeshData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;

    /// <summary>
    /// Wrapper for navigation mesh data created by CreateNavMeshData.
    /// Provides typed access to the various data sections within the raw byte array.
    /// </summary>
    public unsafe readonly struct DtNavMeshData
    {
        private readonly byte* data;

        /// <summary>Initializes a new instance of the <see cref="DtNavMeshData"/> struct.  </summary>
        /// <param name="data">Pointer to the navigation mesh data. </param>
        public DtNavMeshData(byte* data)
        {
            this.data = data;
        }

        /// <summary> Gets the mesh header containing metadata about the navigation mesh. </summary>
        public DtMeshHeader* Header
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (DtMeshHeader*)this.data;
        }

        /// <summary> Gets the navigation mesh vertices [vertCount]. </summary>
        public float3* Vertices
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var offset = sizeof(DtMeshHeader);
                return (float3*)(this.data + offset);
            }
        }

        /// <summary> Gets the navigation mesh polygons [polyCount]. </summary>
        public DtPoly* Polygons
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var header = this.Header;
                return (DtPoly*)(this.Vertices + header->vertCount);
            }
        }

        /// <summary> Gets the polygon links [maxLinkCount]. Note: Links are created at runtime, this space is reserved but empty. </summary>
        public DtLink* Links
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var header = this.Header;
                return (DtLink*)(this.Polygons + header->polyCount);
            }
        }

        /// <summary> Gets the detail mesh data [detailMeshCount]. </summary>
        public DtPolyDetail* DetailMeshes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (DtPolyDetail*)(this.Links + this.Header->maxLinkCount);
        }

        /// <summary> Gets the detail mesh vertices [detailVertCount]. </summary>
        public float3* DetailVertices
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (float3*)(this.DetailMeshes + this.Header->detailMeshCount);
        }

        /// <summary> Gets the detail mesh triangles [detailTriCount]. </summary>
        public byte4* DetailTriangles
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (byte4*)(this.DetailVertices + this.Header->detailVertCount);
        }

        /// <summary> Gets the bounding volume tree nodes [bvNodeCount]. </summary>
        public DtBVNode* BVTree
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (DtBVNode*)(this.DetailTriangles + this.Header->detailTriCount);
        }

        /// <summary> Gets the off-mesh connections [offMeshConCount]. </summary>
        public DtOffMeshConnection* OffMeshConnections
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (DtOffMeshConnection*)(this.BVTree + this.Header->bvNodeCount);
        }

        /// <summary> Gets the raw data pointer. </summary>
        public byte* RawData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data;
        }

        /// <summary> Checks if the wrapper contains valid data. </summary>
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data != null;
        }

        /// <summary> Implicit conversion from byte pointer. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DtNavMeshData(byte* data) => new(data);

        /// <summary> Implicit conversion to byte pointer. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte*(DtNavMeshData navMeshData) => navMeshData.data;
    }
}
