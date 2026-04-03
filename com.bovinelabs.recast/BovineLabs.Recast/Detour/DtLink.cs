// <copyright file="DtLink.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;

    /// <summary>Defines a link between polygons.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DtLink
    {
        /// <summary>Neighbour reference.</summary>
        public DtPolyRef polyRef;

        /// <summary>Index of the next link.</summary>
        public uint next;

        /// <summary>Index of the polygon edge that owns this link.</summary>
        public byte edge;

        /// <summary>If a boundary link, defines on which side the link is.</summary>
        public byte side;

        /// <summary>If a boundary link, defines the minimum sub-edge area.</summary>
        public byte bmin;

        /// <summary>If a boundary link, defines the maximum sub-edge area.</summary>
        public byte bmax;
    }
}
