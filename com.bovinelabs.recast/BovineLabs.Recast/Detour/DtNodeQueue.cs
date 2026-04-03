// <copyright file="DtNodeQueue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>Priority queue (min-heap) for pathfinding nodes.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DtNodeQueue : IDisposable
    {
        private readonly AllocatorManager.AllocatorHandle allocator;
        private readonly int capacity;
        private DtNode** heap;
        private int size;

        public DtNodeQueue(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            if (capacity <= 0)
            {
                throw new ArgumentException($"Invalid capacity: {capacity}");
            }

            this.capacity = capacity;
            this.allocator = allocator;
            this.size = 0;

            var heapSize = sizeof(DtNode*) * (capacity + 1);
            this.heap = (DtNode**)AllocatorManager.Allocate(allocator, heapSize, UnsafeUtility.AlignOf<IntPtr>());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.heap != null)
            {
                AllocatorManager.Free(this.allocator, this.heap);
                this.heap = null;
            }
        }

        public void Clear()
        {
            this.size = 0;
        }

        /// <summary>Get the top node (minimum total cost).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DtNode* Top()
        {
            return this.size > 0 ? this.heap[0] : null;
        }

        /// <summary>Remove and return the top node.</summary>
        public DtNode* Pop()
        {
            if (this.size == 0)
            {
                return null;
            }

            var result = this.heap[0];
            this.size--;
            if (this.size > 0)
            {
                this.TrickleDown(0, this.heap[this.size]);
            }

            return result;
        }

        /// <summary>Add a node to the queue.</summary>
        public void Push(DtNode* node)
        {
            if (this.size >= this.capacity)
            {
                return;
            }

            this.BubbleUp(this.size, node);
            this.size++;
        }

        /// <summary>Update a node's position in the queue after its cost changed.</summary>
        public void Modify(DtNode* node)
        {
            for (var i = 0; i < this.size; ++i)
            {
                if (this.heap[i] == node)
                {
                    this.BubbleUp(i, node);
                    return;
                }
            }
        }

        public readonly bool Empty => this.size == 0;

        public readonly int Capacity => this.capacity;
        public readonly int Size => this.size;

        private void BubbleUp(int i, DtNode* node)
        {
            var parent = (i - 1) / 2;

            // note: (i > 0) means there is a parent
            while ((i > 0) && (this.heap[parent]->total > node->total))
            {
                this.heap[i] = this.heap[parent];
                i = parent;
                parent = (i - 1) / 2;
            }

            this.heap[i] = node;
        }

        private void TrickleDown(int i, DtNode* node)
        {
            var child = (i * 2) + 1;
            while (child < this.size)
            {
                if (((child + 1) < this.size) &&
                    (this.heap[child]->total > this.heap[child + 1]->total))
                {
                    child++;
                }

                this.heap[i] = this.heap[child];
                i = child;
                child = (i * 2) + 1;
            }

            this.BubbleUp(i, node);
        }
    }
}
