// <copyright file="DtMeshHeader.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    /// <summary>Provides high level information related to a mesh tile object.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DtMeshHeader
    {
        public int magic; // Tile magic number
        public int version; // Tile data format version number
        public int x; // The x-position of the tile within the grid
        public int y; // The y-position of the tile within the grid
        public int layer; // The layer of the tile within the grid
        public uint userId; // The user defined id of the tile
        public int polyCount; // The number of polygons in the tile
        public int vertCount; // The number of vertices in the tile
        public int maxLinkCount; // The number of allocated links
        public int detailMeshCount; // The number of sub-meshes in the detail mesh
        public int detailVertCount; // The number of unique vertices in the detail mesh
        public int detailTriCount; // The number of triangles in the detail mesh
        public int bvNodeCount; // The number of bounding volume nodes
        public int offMeshConCount; // The number of off-mesh connections
        public int offMeshBase; // The index of the first polygon which is an off-mesh connection
        public float walkableHeight; // The height of the agents using the tile
        public float walkableRadius; // The radius of the agents using the tile
        public float walkableClimb; // The maximum climb height of the agents using the tile
        public float3 bmin; // The minimum bounds of the tile's AABB
        public float3 bmax; // The maximum bounds of the tile's AABB
        public float bvQuantFactor; // The bounding volume quantization factor
    }
}
