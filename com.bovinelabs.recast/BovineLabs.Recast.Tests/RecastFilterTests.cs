// <copyright file="RecastFilterTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast.Tests
{
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Mathematics;

    public unsafe class RecastFilterTests
    {
        [Test]
        public void FilterLowHangingWalkableObstacles_NoSpanAbove_Unchanged()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            Recast.CreateHeightfield(&heightfield, 1, 1, float3.zero, new float3(1, 1, 1), 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 1, 1);

            Recast.FilterLowHangingWalkableObstacles(5, &heightfield);

            var span = heightfield.Spans[0];
            Assert.IsTrue(span != null);
            Assert.AreEqual(1u, span->Area);
        }

        [Test]
        public void FilterLowHangingWalkableObstacles_HighObstacleUnchanged()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            const int walkableHeight = 5;
            Recast.CreateHeightfield(&heightfield, 1, 1, float3.zero, new float3(1, 1, 1), 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 1, 1);
            Recast.AddSpan(&heightfield, 0, 0, 1 + walkableHeight, 2 + walkableHeight, Recast.RCNullArea, 1);

            Recast.FilterLowHangingWalkableObstacles(walkableHeight, &heightfield);

            var baseSpan = heightfield.Spans[0];
            Assert.IsTrue(baseSpan != null);
            Assert.AreEqual(1u, baseSpan->Area);
            Assert.IsTrue(baseSpan->Next != null);
            Assert.AreEqual(Recast.RCNullArea, baseSpan->Next->Area);

            baseSpan->Next->SMin += 10;
            baseSpan->Next->SMax += 10;

            Recast.FilterLowHangingWalkableObstacles(walkableHeight, &heightfield);

            Assert.AreEqual(1u, baseSpan->Area);
            Assert.AreEqual(Recast.RCNullArea, baseSpan->Next->Area);
        }

        [Test]
        public void FilterLowHangingWalkableObstacles_LowObstacleBecomesWalkable()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            const int walkableHeight = 5;
            Recast.CreateHeightfield(&heightfield, 1, 1, float3.zero, new float3(1, 1, 1), 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 1, 1);
            Recast.AddSpan(&heightfield, 0, 0, 1 + (walkableHeight - 1), 2 + (walkableHeight - 1), Recast.RCNullArea, 1);

            Recast.FilterLowHangingWalkableObstacles(walkableHeight, &heightfield);

            var baseSpan = heightfield.Spans[0];
            Assert.IsTrue(baseSpan != null);
            Assert.AreEqual(1u, baseSpan->Area);
            Assert.IsTrue(baseSpan->Next != null);
            Assert.AreEqual(1u, baseSpan->Next->Area);
        }

        [Test]
        public void FilterLowHangingWalkableObstacles_OverlapBeyondClimbDoesNotChange()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            const int walkableHeight = 5;
            Recast.CreateHeightfield(&heightfield, 1, 1, float3.zero, new float3(1, 1, 1), 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 1, 1);
            Recast.AddSpan(&heightfield, 0, 0, 2 + (walkableHeight - 1), 3 + (walkableHeight - 1), Recast.RCNullArea, 1);

            Recast.FilterLowHangingWalkableObstacles(walkableHeight, &heightfield);

            var baseSpan = heightfield.Spans[0];
            Assert.IsTrue(baseSpan != null);
            Assert.AreEqual(1u, baseSpan->Area);
            Assert.IsTrue(baseSpan->Next != null);
            Assert.AreEqual(Recast.RCNullArea, baseSpan->Next->Area);
        }

        [Test]
        public void FilterLowHangingWalkableObstacles_MarksOnlyFirstLowObstacle()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            const int walkableHeight = 5;
            Recast.CreateHeightfield(&heightfield, 1, 1, float3.zero, new float3(1, 1, 1), 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 1, 1);

            var lastSMax = 1u;
            for (var i = 0; i < 9; ++i)
            {
                var smin = lastSMax + (uint)(walkableHeight - 1);
                var smax = smin + 1;
                Recast.AddSpan(&heightfield, 0, 0, smin, smax, Recast.RCNullArea, 1);
                lastSMax = smax;
            }

            Recast.FilterLowHangingWalkableObstacles(walkableHeight, &heightfield);

            var span = heightfield.Spans[0];
            for (var i = 0; i < 10; ++i)
            {
                Assert.IsTrue(span != null);
                Assert.AreEqual(i <= 1 ? 1u : Recast.RCNullArea, span->Area);
                span = span->Next;
            }
        }

        [Test]
        public void FilterLedgeSpans_MarksBorderCells()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            const int width = 10;
            const int height = 10;

            Recast.CreateHeightfield(&heightfield, width, height, float3.zero, new float3(10, 1, 10), 1, 1);

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < height; z++)
                {
                    Recast.AddSpan(&heightfield, x, z, 0, 1, 1, 1);
                }
            }

            Recast.FilterLedgeSpans(10, 5, &heightfield);

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < height; z++)
                {
                    var span = heightfield.Spans[x + (z * width)];
                    Assert.IsTrue(span != null);

                    if (x == 0 || z == 0 || x == width - 1 || z == height - 1)
                    {
                        Assert.AreEqual(Recast.RCNullArea, span->Area);
                    }
                    else
                    {
                        Assert.AreEqual(1u, span->Area);
                    }

                    Assert.IsTrue(span->Next == null);
                    Assert.AreEqual(0u, span->SMin);
                    Assert.AreEqual(1u, span->SMax);
                }
            }
        }

        [Test]
        public void FilterWalkableLowHeightSpans_NoOverheadSpanUnchanged()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            const int walkableHeight = 5;

            Recast.CreateHeightfield(&heightfield, 1, 1, float3.zero, new float3(1, 1, 1), 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 1, 1);

            Recast.FilterWalkableLowHeightSpans(walkableHeight, &heightfield);

            var span = heightfield.Spans[0];
            Assert.IsTrue(span != null);
            Assert.AreEqual(1u, span->Area);
        }

        [Test]
        public void FilterWalkableLowHeightSpans_WithLargeOverheadUnchanged()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            const int walkableHeight = 5;

            Recast.CreateHeightfield(&heightfield, 1, 1, float3.zero, new float3(1, 1, 1), 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 1, 1);
            Recast.AddSpan(&heightfield, 0, 0, 10, 11, Recast.RCNullArea, 1);

            Recast.FilterWalkableLowHeightSpans(walkableHeight, &heightfield);

            var span = heightfield.Spans[0];
            Assert.IsTrue(span != null);
            Assert.AreEqual(1u, span->Area);
            Assert.IsTrue(span->Next != null);
            Assert.AreEqual(Recast.RCNullArea, span->Next->Area);
        }

        [Test]
        public void FilterWalkableLowHeightSpans_LowOverheadMarksUnwalkable()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            const int walkableHeight = 5;

            Recast.CreateHeightfield(&heightfield, 1, 1, float3.zero, new float3(1, 1, 1), 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 1, 1);
            Recast.AddSpan(&heightfield, 0, 0, 3, 4, Recast.RCNullArea, 1);

            Recast.FilterWalkableLowHeightSpans(walkableHeight, &heightfield);

            var span = heightfield.Spans[0];
            Assert.IsTrue(span != null);
            Assert.AreEqual(Recast.RCNullArea, span->Area);
            Assert.IsTrue(span->Next != null);
            Assert.AreEqual(Recast.RCNullArea, span->Next->Area);
        }
    }
}