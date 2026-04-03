// <copyright file="DtNavMeshParams.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    /// <summary>Navigation mesh parameters for initialization.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DtNavMeshParams
    {
        /// <summary> The world space origin of the navigation mesh's tile space. </summary>
        public float3 orig;

        /// <summary> The width of each tile (along the x-axis). </summary>
        public float tileWidth;

        /// <summary> The height of each tile (along the z-axis). </summary>
        public float tileHeight;

        /// <summary> The maximum number of tiles the navigation mesh can contain. </summary>
        public int maxTiles;

        /// <summary> The maximum number of polygons each tile can contain. </summary>
        public int maxPolys;
    }
}
