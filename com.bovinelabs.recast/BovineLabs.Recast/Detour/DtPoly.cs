// <copyright file="DtPoly.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>Defines a polygon within a navigation mesh tile.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DtPoly
    {
        /// <summary>Index to first link in linked list.</summary>
        public uint firstLink;

        /// <summary>The indices of the polygon's vertices.</summary>
        public fixed ushort verts[Detour.DTVertsPerPolygon];

        /// <summary>Packed data representing neighbor polygons references and flags for each edge.</summary>
        public fixed ushort neis[Detour.DTVertsPerPolygon];

        /// <summary>The user defined polygon flags.</summary>
        public ushort flags;

        /// <summary>The number of vertices in the polygon.</summary>
        public byte vertCount;

        /// <summary>The bit packed area id and polygon type.</summary>
        private byte areaAndtype;

        /// <summary>Sets the user defined area id. [Limit: <see cref="Detour.DTMaxAreas"/>].</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetArea(byte area)
        {
            this.areaAndtype = (byte)((this.areaAndtype & 0xc0) | (area & 0x3f));
        }

        /// <summary>Sets the polygon type.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetType(byte type)
        {
            this.areaAndtype = (byte)((this.areaAndtype & 0x3f) | (type << 6));
        }

        /// <summary>Gets the user defined area id.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetArea()
        {
            return (byte)(this.areaAndtype & 0x3f);
        }

        /// <summary>Gets the polygon type.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DtPolyTypes GetPolyType()
        {
            return (DtPolyTypes)(this.areaAndtype >> 6);
        }
    }
}
