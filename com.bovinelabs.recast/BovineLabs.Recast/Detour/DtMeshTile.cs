// <copyright file="DtMeshTile.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    /// <summary>Defines a navigation mesh tile.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DtMeshTile
    {
        public uint salt; // Counter describing modifications to the tile
        public uint linksFreeList; // Index to the next free link
        public DtMeshHeader* header; // The tile header
        public DtPoly* polys; // The tile polygons
        public float3* verts; // The tile vertices
        public DtLink* links; // The tile links
        public DtPolyDetail* detailMeshes; // The tile's detail sub-meshes
        public float3* detailVerts; // The detail mesh's unique vertices
        public byte4* detailTris; // The detail mesh's triangles
        public DtBVNode* bvTree; // The tile bounding volume nodes
        public DtOffMeshConnection* offMeshCons; // The tile off-mesh connections
        public byte* data; // The tile data (not directly accessed under normal situations)
        public int dataSize; // Size of the tile data
        public DtTileFlags flags; // Tile flags
        public DtMeshTile* next; // The next free tile, or the next tile in the spatial grid
    }
}
