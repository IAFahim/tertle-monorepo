// <copyright file="DtNodePool.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>Pool for managing navigation nodes with hash table for efficient lookup.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DtNodePool : IDisposable
    {
        public const int DTNodeParentBits = 24;
        public const int DTNodeStateBits = 2;
        public const int DTMaxStatesPerNode = 1 << DTNodeStateBits; // 4

        public const int Null = 0xFFFF;

        public static readonly DtNodeIndex DTNullIDX = new(Null);

        private readonly int maxNodes;
        private readonly int hashSize;
        private DtNode* nodes;
        private DtNodeIndex* first; // Hash table buckets
        private DtNodeIndex* next; // Linked list chain
        private int nodeCount;

        public DtNodePool(int maxNodes, int hashSize)
        {
            // Ensure hash size is power of 2
            hashSize = (int)Detour.NextPowerOfTwo((uint)hashSize);

            // pidx is special as 0 means "none" and 1 is the first node
            if (maxNodes <= 0 || maxNodes > DTNullIDX || maxNodes > (1 << DTNodeParentBits) - 1)
            {
                throw new ArgumentException($"Invalid maxNodes: {maxNodes}");
            }

            this.maxNodes = maxNodes;
            this.hashSize = hashSize;
            this.nodeCount = 0;

            // Allocate memory
            var nodeSize = sizeof(DtNode) * maxNodes;
            var nextSize = sizeof(DtNodeIndex) * maxNodes;
            var firstSize = sizeof(DtNodeIndex) * hashSize;

            this.nodes = (DtNode*)AllocatorManager.Allocate(Allocator.Persistent, nodeSize, UnsafeUtility.AlignOf<DtNode>());
            this.next = (DtNodeIndex*)AllocatorManager.Allocate(Allocator.Persistent, nextSize, UnsafeUtility.AlignOf<DtNodeIndex>());
            this.first = (DtNodeIndex*)AllocatorManager.Allocate(Allocator.Persistent, firstSize, UnsafeUtility.AlignOf<DtNodeIndex>());

            // Initialize hash table
            UnsafeUtility.MemSet(this.first, 0xFF, firstSize);
            UnsafeUtility.MemSet(this.next, 0xFF, nextSize);
        }

        public void Clear()
        {
            UnsafeUtility.MemSet(this.first, 0xFF, sizeof(DtNodeIndex) * this.hashSize);
            this.nodeCount = 0;
        }

        /// <summary>Get a node by polygon reference and state, allocating if necessary.</summary>
        public DtNode* GetNode(DtPolyRef id, byte state = 0)
        {
            var bucket = HashRef(id) & (this.hashSize - 1);
            var i = this.first[bucket];

            // Search for existing node
            while (i != DTNullIDX)
            {
                if (this.nodes[i].id == id && this.nodes[i].State == state)
                {
                    return &this.nodes[i];
                }

                i = this.next[i];
            }

            // Allocate new node if we have space
            if (this.nodeCount >= this.maxNodes)
            {
                return null;
            }

            i = (DtNodeIndex)this.nodeCount;
            this.nodeCount++;

            // Initialize node
            var node = &this.nodes[i];
            node->Clear();
            node->id = id;
            node->State = state;

            // Insert into hash table
            this.next[i] = this.first[bucket];
            this.first[bucket] = i;

            return node;
        }

        /// <summary>Find existing node by polygon reference and state.</summary>
        public DtNode* FindNode(DtPolyRef id, byte state)
        {
            var bucket = HashRef(id) & (this.hashSize - 1);
            var i = this.first[bucket];

            while (i != DTNullIDX)
            {
                if (this.nodes[i].id == id && this.nodes[i].State == state)
                {
                    return &this.nodes[i];
                }

                i = this.next[i];
            }

            return null;
        }

        /// <summary>Find all nodes with the given polygon reference.</summary>
        public uint FindNodes(DtPolyRef id, DtNode** nodeArray, int maxNodes)
        {
            var count = 0u;
            var bucket = HashRef(id) & (this.hashSize - 1);
            var i = this.first[bucket];

            while (i != DTNullIDX)
            {
                if (this.nodes[i].id == id)
                {
                    if (count >= maxNodes)
                    {
                        return count;
                    }

                    nodeArray[count++] = &this.nodes[i];
                }

                i = this.next[i];
            }

            return count;
        }

        /// <summary>Get 1-based index of node (0 means null).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetNodeIdx(DtNode* node)
        {
            if (node == null)
            {
                return 0;
            }

            return (uint)(node - this.nodes) + 1;
        }

        /// <summary>Get node at 1-based index (const version).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly DtNode* GetNodeAtIdx(uint idx)
        {
            if (idx == 0)
            {
                return null;
            }

            return &this.nodes[idx - 1];
        }

        public readonly int MaxNodes => this.maxNodes;

        public readonly int HashSize => this.hashSize;

        public readonly int NodeCount => this.nodeCount;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.nodes != null)
            {
                AllocatorManager.Free(Allocator.Persistent, this.nodes);
                this.nodes = null;
            }

            if (this.next != null)
            {
                AllocatorManager.Free(Allocator.Persistent, this.next);
                this.next = null;
            }

            if (this.first != null)
            {
                AllocatorManager.Free(Allocator.Persistent, this.first);
                this.first = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint HashRef(DtPolyRef polyRef)
        {
#if DT_POLYREF32
            var a = (uint)polyRef;
            a += ~(a << 15);
            a ^= a >> 10;
            a += a << 3;
            a ^= a >> 6;
            a += ~(a << 11);
            a ^= a >> 16;
            return a;
#else
            // From Thomas Wang, https://gist.github.com/badboy/6267743
            var a = (ulong)polyRef;
            a = (~a) + (a << 18); // a = (a << 18) - a - 1;
            a = a ^ (a >> 31);
            a = a * 21; // a = (a + (a << 2)) + (a << 4);
            a = a ^ (a >> 11);
            a = a + (a << 6);
            a = a ^ (a >> 22);
            return (uint)a;
#endif
        }

        public readonly struct DtNodeIndex : IEquatable<DtNodeIndex>
        {
            public readonly ushort Value;

            public DtNodeIndex(ushort value) => this.Value = value;

            public static implicit operator ushort(DtNodeIndex index) => index.Value;

            public static implicit operator DtNodeIndex(ushort value) => new(value);

            /// <inheritdoc/>
            public bool Equals(DtNodeIndex other) => this.Value == other.Value;

            /// <inheritdoc/>
            public override bool Equals(object obj) => obj is DtNodeIndex other && this.Equals(other);

            /// <inheritdoc/>
            public override int GetHashCode() => this.Value.GetHashCode();

            public static bool operator ==(DtNodeIndex left, DtNodeIndex right) => left.Equals(right);

            public static bool operator !=(DtNodeIndex left, DtNodeIndex right) => !left.Equals(right);
        }
    }
}
