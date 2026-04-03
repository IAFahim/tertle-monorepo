// <copyright file="DtNavMeshCreateParams.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    /// <summary>Represents the source data used to build a navigation mesh tile.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DtNavMeshCreateParams
    {
        /// <summary>Polygon mesh vertices stored as (x, y, z) triplets repeated <c>vertCount</c> times.</summary>
        public ushort3* Verts;

        /// <summary>Total number of vertices in the polygon mesh.</summary>
        public int VertCount;

        /// <summary>Polygon data laid out as <c>2 * nvp * polyCount</c> entries.</summary>
        public ushort* Polys;

        /// <summary>User-defined flags assigned to each polygon.</summary>
        public ushort* PolyFlags;

        /// <summary>User-defined area identifiers assigned to each polygon.</summary>
        public byte* PolyAreas;

        /// <summary>Total number of polygons in the mesh.</summary>
        public int PolyCount;

        /// <summary>Maximum number of vertices per polygon.</summary>
        public int Nvp;

        /// <summary>Height detail sub-mesh data laid out as <c>polyCount</c> entries.</summary>
        public uint4* DetailMeshes;

        /// <summary>Detail mesh vertices stored as (x, y, z) triplets repeated <c>detailVertsCount</c> times.</summary>
        public float3* DetailVerts;

        /// <summary>Total number of vertices in the detail mesh.</summary>
        public int DetailVertsCount;

        /// <summary>Detail mesh triangles laid out as <c>detailTriCount</c> entries.</summary>
        public byte4* DetailTris;

        /// <summary>Total number of triangles in the detail mesh.</summary>
        public int DetailTriCount;

        /// <summary>Off-mesh connection vertices stored as <see cref="float3x2"/> pairs repeated <c>offMeshConCount</c> times.</summary>
        public float3x2* OffMeshConVerts;

        /// <summary>Off-mesh connection radii array containing <c>offMeshConCount</c> entries.</summary>
        public float* OffMeshConRad;

        /// <summary>User-defined flags assigned to each off-mesh connection.</summary>
        public ushort* OffMeshConFlags;

        /// <summary>User-defined area identifiers assigned to each off-mesh connection.</summary>
        public byte* OffMeshConAreas;

        /// <summary>Permitted travel direction for each off-mesh connection.</summary>
        public byte* OffMeshConDir;

        /// <summary>User-defined identifiers assigned to each off-mesh connection.</summary>
        public uint* OffMeshConUserID;

        /// <summary>Total number of off-mesh connections.</summary>
        public int OffMeshConCount;

        /// <summary>User-defined identifier of the tile.</summary>
        public uint UserId;

        /// <summary>Tile x-grid location within the multi-tile destination mesh.</summary>
        public int TileX;

        /// <summary>Tile y-grid location within the multi-tile destination mesh.</summary>
        public int TileY;

        /// <summary>Tile layer within the layered destination mesh.</summary>
        public int TileLayer;

        /// <summary>Minimum bounds of the tile.</summary>
        public float3 Bmin;

        /// <summary>Maximum bounds of the tile.</summary>
        public float3 Bmax;

        /// <summary>Agent height.</summary>
        public float WalkableHeight;

        /// <summary>Agent radius.</summary>
        public float WalkableRadius;

        /// <summary>Maximum traversable ledge height for the agent.</summary>
        public float WalkableClimb;

        /// <summary>XZ-plane cell size of the polygon mesh.</summary>
        public float Cs;

        /// <summary>Y-axis cell height of the polygon mesh.</summary>
        public float Ch;

        /// <summary>Indicates whether a bounding volume tree should be built for the tile.</summary>
        public bool BuildBvTree;
    }
}
