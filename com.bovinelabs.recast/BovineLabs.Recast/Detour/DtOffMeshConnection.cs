// <copyright file="DtOffMeshConnection.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    /// <summary>Defines a navigation mesh off-mesh connection within a mesh tile.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DtOffMeshConnection
    {
        /// <summary>The start point of the connection. </summary>
        public float3 StartPos;

        /// <summary>The end point of the connection. </summary>
        public float3 EndPos;

        /// <summary>The radius of the endpoints.</summary>
        public float rad;

        /// <summary>The polygon reference of the connection within the tile.</summary>
        public ushort poly;

        /// <summary>Link flags.</summary>
        public byte flags;

        /// <summary>End point side.</summary>
        public byte side;

        /// <summary>The id of the offmesh connection.</summary>
        public uint userId;
    }
}
