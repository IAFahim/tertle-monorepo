// <copyright file="DtNode.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    /// <summary>Node flags for pathfinding.</summary>
    [Flags]
    public enum DtNodeFlags : byte
    {
        DT_NODE_OPEN = 0x01,
        DT_NODE_CLOSED = 0x02,
        DT_NODE_PARENT_DETACHED = 0x04, // parent of the node is not adjacent. Found using raycast.
    }

    /// <summary>A node in the navigation graph for pathfinding.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DtNode
    {
        public float3 pos;           // Position of the node
        public float cost;           // Cost from previous node to current node
        public float total;          // Cost up to the node
        private uint packedData;     // Packed: pidx(24) + state(2) + flags(3) + reserved(3)
        public DtPolyRef id;         // Polygon ref the node corresponds to

        /// <summary>Index to parent node (24 bits).</summary>
        public uint ParentIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.packedData & 0xFFFFFF;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.packedData = (this.packedData & 0xFF000000) | (value & 0xFFFFFF);
        }

        /// <summary>Extra state information (2 bits).</summary>
        public uint State
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (this.packedData >> 24) & 0x3;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.packedData = (this.packedData & 0xFCFFFFFF) | ((value & 0x3) << 24);
        }

        /// <summary>Node flags (3 bits).</summary>
        public DtNodeFlags Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (DtNodeFlags)((this.packedData >> 26) & 0x7);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.packedData = (this.packedData & 0xE3FFFFFF) | (((uint)value & 0x7) << 26);
        }

        public void Clear()
        {
            this.pos = float3.zero;
            this.cost = 0;
            this.total = 0;
            this.packedData = 0;
            this.id = 0;
        }
    }
}