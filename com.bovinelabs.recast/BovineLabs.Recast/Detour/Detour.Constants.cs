// <copyright file="Detour.Constants.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;

    /// <summary>Navigation mesh polygon types.</summary>
    public enum DtPolyTypes : byte
    {
        PolytypeGround = 0,
        PolytypeOffMeshConnection = 1,
    }

    /// <summary>Tile flags for dtNavMesh operations.</summary>
    [Flags]
    public enum DtTileFlags
    {
        TileFreeData = 0x01,
    }

    /// <summary>Core Detour constants.</summary>
    public static partial class Detour
    {
        public const ushort MeshNullIDX = 0xffff; // MESH_NULL_IDX

        // Core constants
        public const int DTVertsPerPolygon = 6; // DT_VERTS_PER_POLYGON

        /// <summary> A value that indicates the entity does not link to anything. </summary>
        public const uint DTNullLink = 0xffffffff; // DT_NULL_LINK

        /// <summary> A value that indicates the entity does not link to anything. </summary>
        public const ushort DTNullLinkShort = 0xffff; // DT_NULL_LINK_SHORT

        public const ushort DTExtLink = 0x8000; // DT_EXT_LINK
        public const uint DTOffMeshConBidir = 1; // DT_OFFMESH_CON_BIDIR
        public const int DTMaxAreas = 64; // DT_MAX_AREAS

        // Serialization constants
        public const int DTNavmeshMagic = ('D' << 24) | ('N' << 16) | ('A' << 8) | 'V'; // DT_NAVMESH_MAGIC
        public const int DTNavmeshVersion = 7; // DT_NAVMESH_VERSION
        public const int DTNavmeshStateMagic = ('D' << 24) | ('N' << 16) | ('M' << 8) | 'S'; // DT_NAVMESH_STATE_MAGIC
        public const int DTNavmeshStateVersion = 1; // DT_NAVMESH_STATE_VERSION

        public const float DTRayCastLimitProportions = 50.0f; // DT_RAY_CAST_LIMIT_PROPORTIONS

#if !DT_POLYREF32
        public const int DTSaltBits = 16; // DT_SALT_BITS
        public const int DTTileBits = 28; // DT_TILE_BITS
        public const int DTPolyBits = 20; // DT_POLY_BITS
#endif
    }
}
