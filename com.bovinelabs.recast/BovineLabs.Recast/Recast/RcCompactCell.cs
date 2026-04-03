// <copyright file="RcCompactCell.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;

    /// <summary> Provides information on the content of a cell column in a compact heightfield. </summary>
    /// <remarks>Memory layout must match C++ rcCompactCell exactly.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct RcCompactCell
    {
        /// <summary>
        /// Packed data containing index(24) + count(8).
        /// </summary>
        private uint packedData;

        /// <summary> Gets or sets index to the first span in the column. </summary>
        public uint Index
        {
            get => this.packedData & 0x00FFFFFF;
            set => this.packedData = (this.packedData & 0xFF000000) | (value & 0x00FFFFFF);
        }

        /// <summary> Gets or sets number of spans in the column. </summary>
        public uint Count
        {
            get => (this.packedData >> 24) & 0xFF;
            set => this.packedData = (this.packedData & 0x00FFFFFF) | ((value & 0xFF) << 24);
        }
    }
}