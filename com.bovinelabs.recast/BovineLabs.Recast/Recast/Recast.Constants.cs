// <copyright file="Recast.Constants.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;

    /// <summary> Contour build flags. </summary>
    [Flags]
    public enum RcBuildContoursFlags
    {
        /// <summary> Tessellate solid (impassable) edges during contour simplification. </summary>
        RCContourTessWallEdges = 0x01,

        /// <summary> Tessellate edges between areas during contour simplification. </summary>
        RCContourTessAreaEdges = 0x02,
    }

    /// <summary>
    /// Constants used in the Recast library. This mirrors the constants defined in the C++ Recast.h header.
    /// </summary>
    public static partial class Recast
    {
        /// <summary> An value which indicates an invalid index within a mesh. </summary>
        public const ushort RCMeshNullIdx = 0xffff;

        /// <summary> Represents the null area. When a data element is given this value it is considered to no longer be assigned to a usable area. </summary>
        public const byte RCNullArea = 0;

        /// <summary> The default area id used to indicate a walkable polygon. </summary>
        public const byte RCWalkableArea = 63;

        /// <summary> The value returned if a neighbor is not connected to another span. </summary>
        public const int RCNotConnected = 0x3f;

        /// <summary> Defines the number of bits allocated to rcSpan::smin and rcSpan::smax. </summary>
        public const int RCSpanHeightBits = 13;

        /// <summary> Defines the maximum value for rcSpan::smin and rcSpan::smax. </summary>
        public const int RCSpanMaxHeight = (1 << RCSpanHeightBits) - 1;

        /// <summary> The number of spans allocated per span spool. </summary>
        public const int RCSpansPerPool = 2048;

        /// <summary> Heightfield border flag. If a heightfield region ID has this bit set, then the region is a border region. </summary>
        public const ushort RCBorderReg = 0x8000;

        /// <summary> Polygon touches multiple regions. </summary>
        public const ushort RCMultipleRegs = 0;

        /// <summary> If a region ID has this bit set, then the associated element lies on a tile border. </summary>
        public const int RCBorderVertex = 0x10000;

        /// <summary> If a region ID has this bit set, then the associated element lies on the border of an area. </summary>
        public const int RCAreaBorder = 0x20000;

        /// <summary> Mask used to extract the region id from a bitfield. </summary>
        public const int RCContourRegMask = 0xffff;

        // Match C++ version for memory layout
        public const int RCSpanSize = 16;
        public const int RCSpanPoolItemsSize = RCSpansPerPool * RCSpanSize;
    }
}