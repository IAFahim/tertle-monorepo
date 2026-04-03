// <copyright file="DtPolyDetail.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;

    /// <summary>Defines the location of detail sub-mesh data within a mesh tile.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DtPolyDetail
    {
        /// <summary>The offset of the vertices in the detail verts array.</summary>
        public uint vertBase;

        /// <summary>The offset of the triangles in the detail tris array.</summary>
        public uint triBase;

        /// <summary>The number of vertices in the sub-mesh.</summary>
        public byte vertCount;

        /// <summary>The number of triangles in the sub-mesh.</summary>
        public byte triCount;
    }
}
