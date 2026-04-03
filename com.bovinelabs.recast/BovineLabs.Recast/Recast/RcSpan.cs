// <copyright file="RcSpan.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.InteropServices;

    /// <summary> Represents a span in a heightfield. </summary>
    /// <remarks> Memory layout must match C++ rcSpan exactly. </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcSpan
    {
        /// <summary>
        /// Packed bitfields - total 32 bits: smin(13) + smax(13) + area(6).
        /// </summary>
        private uint packedData;

        /// <summary>
        /// Pointer to the next span in the linked list.
        /// </summary>
        public RcSpan* Next;

        public uint SMin
        {
            get => this.packedData & ((1u << Recast.RCSpanHeightBits) - 1);
            set => this.packedData = (this.packedData & ~((1u << Recast.RCSpanHeightBits) - 1)) |
                (value & ((1u << Recast.RCSpanHeightBits) - 1));
        }

        public uint SMax
        {
            get => (this.packedData >> Recast.RCSpanHeightBits) & ((1u << Recast.RCSpanHeightBits) - 1);
            set => this.packedData = (this.packedData & ~(((1u << Recast.RCSpanHeightBits) - 1) << Recast.RCSpanHeightBits)) |
                ((value & ((1u << Recast.RCSpanHeightBits) - 1)) << Recast.RCSpanHeightBits);
        }

        public uint Area
        {
            get => (this.packedData >> (Recast.RCSpanHeightBits * 2)) & 0x3F;
            set => this.packedData = (this.packedData & ~(0x3Fu << (Recast.RCSpanHeightBits * 2))) |
                ((value & 0x3F) << (Recast.RCSpanHeightBits * 2));
        }
    }
}
