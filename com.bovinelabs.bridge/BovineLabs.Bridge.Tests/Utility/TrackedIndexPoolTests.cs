// <copyright file="TrackedIndexPoolTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Utility
{
    using NUnit.Framework;

    public class TrackedIndexPoolTests
    {
        [Test]
        public void Constructor_InitializesAllIndicesAsAvailable()
        {
            using var pool = new TrackedIndexPool(3);

            Assert.AreEqual(3, pool.Available.Count);
            Assert.AreEqual(0, pool.Returned.Count);
            Assert.AreEqual(0, pool.Requests.Count);
        }

        [Test]
        public void GetAndReturn_MovesIndexBetweenSets()
        {
            using var pool = new TrackedIndexPool(3);

            var index = pool.Get();

            Assert.IsTrue(pool.Requests.Contains(index));
            Assert.IsFalse(pool.Available.Contains(index));

            pool.Return(index);

            Assert.IsTrue(pool.Returned.Contains(index));
            Assert.IsFalse(pool.Requests.Contains(index));

            var reused = pool.Get();
            Assert.AreEqual(index, reused);
        }

        [Test]
        public void ClearReturned_MovesReturnedBackToAvailable()
        {
            using var pool = new TrackedIndexPool(3);

            var first = pool.Get();
            var second = pool.Get();

            pool.Return(first);
            pool.Return(second);
            pool.ClearReturned();

            Assert.AreEqual(0, pool.Returned.Count);
            Assert.AreEqual(3, pool.Available.Count);
        }

        [Test]
        public void ClearRequests_ClearsRequestTracking()
        {
            using var pool = new TrackedIndexPool(3);
            pool.Get();
            pool.Get();

            Assert.Greater(pool.Requests.Count, 0);

            pool.ClearRequests();

            Assert.AreEqual(0, pool.Requests.Count);
        }
    }
}
