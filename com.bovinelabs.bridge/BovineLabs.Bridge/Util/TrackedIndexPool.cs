// <copyright file="TrackedIndexPool.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Util
{
    using System;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Collections;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe readonly struct TrackedIndexPool : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly UnsafeHashSet<int>* available;

        [NativeDisableUnsafePtrRestriction]
        private readonly UnsafeHashSet<int>* returned;

        [NativeDisableUnsafePtrRestriction]
        private readonly UnsafeHashSet<int>* requests;

        public TrackedIndexPool(int length)
        {
            this.available = CollectionCreator.CreateHashSet<int>(length, 0, Allocator.Persistent);
            this.returned = CollectionCreator.CreateHashSet<int>(length, 0, Allocator.Persistent);
            this.requests = CollectionCreator.CreateHashSet<int>(length, 0, Allocator.Persistent);

            this.Length = length;

            for (var i = 0; i < this.Length; i++)
            {
                this.available->Add(i);
            }
        }

        public bool IsCreated => this.available != null;

        public UnsafeHashSet<int>.ReadOnly Available => this.available->AsReadOnly();

        public UnsafeHashSet<int>.ReadOnly Returned => this.returned->AsReadOnly();

        public UnsafeHashSet<int>.ReadOnly Requests => this.requests->AsReadOnly();

        public int Length { get; }

        public void Dispose()
        {
            CollectionCreator.Destroy(this.available);
            CollectionCreator.Destroy(this.returned);
            CollectionCreator.Destroy(this.requests);
        }

        public int Get()
        {
            // Prioritize reusing returned indices so that we don't need to reinitialize
            if (this.returned->Count > 0)
            {
                using var e = this.returned->GetEnumerator();
                var result = e.MoveNext();
                Check.Assume(result, "Broken hash set");

                var index = e.Current;
                this.returned->Remove(index);
                this.requests->Add(index);
                return index;
            }
            else
            {
                using var e = this.available->GetEnumerator();
                var result = e.MoveNext();
                Check.Assume(result, "No more indices");

                var index = e.Current;
                this.available->Remove(index);
                this.requests->Add(index);
                return index;
            }
        }

        public void Return(int index)
        {
            var result = this.returned->Add(index);
            Check.Assume(result, "Returning index twice");

            this.requests->Remove(index); // In case it was requested and returned before using
            Check.Assume(!this.available->Contains(index));
        }

        public void ClearReturned()
        {
            using var e = this.returned->GetEnumerator();
            while (e.MoveNext())
            {
                this.available->Add(e.Current);
            }

            this.returned->Clear();
        }

        public void ClearRequests()
        {
            this.requests->Clear();
        }
    }
}
