// <copyright file="DtBVNode.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;

    /// <summary>Bounding volume node.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DtBVNode
    {
        /// <summary>Minimum bounds of the node's AABB.</summary>
        public ushort3 bmin;

        /// <summary>Maximum bounds of the node's AABB.</summary>
        public ushort3 bmax;

        /// <summary>The node's index (negative for escape sequence).</summary>
        public int i;
    }
}
