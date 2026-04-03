// <copyright file="RcCompactSpan.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;

    /// <summary> Represents a span of unobstructed space within a compact heightfield. </summary>
    /// <remarks>Memory layout must match C++ rcCompactSpan exactly.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct RcCompactSpan
    {
        /// <summary>
        /// The lower extent of the span (measured from the heightfield's base).
        /// </summary>
        public ushort Y;

        /// <summary>
        /// The id of the region the span belongs to (or zero if not in a region).
        /// </summary>
        public ushort Reg;

        /// <summary>
        /// Packed data containing con(24) + h(8).
        /// </summary>
        private uint packedData;

        /// <summary> Gets or sets packed neighbor connection data. </summary>
        public uint Con
        {
            get => this.packedData & 0x00FFFFFF;
            set => this.packedData = (this.packedData & 0xFF000000) | (value & 0x00FFFFFF);
        }

        /// <summary> Gets or sets the height of the span (measured from y). </summary>
        public uint H
        {
            get => (this.packedData >> 24) & 0xFF;
            set => this.packedData = (this.packedData & 0x00FFFFFF) | ((value & 0xFF) << 24);
        }
    }
}