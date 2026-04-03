// <copyright file="RecastRasterizationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast.Tests
{
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Mathematics;

    public unsafe class RecastRasterizationTests
    {
        [Test]
        public void AddSpanAddsSpanToEmptyColumn()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            var bmin = float3.zero;
            var bmax = new float3(1, 1, 1);
            Recast.CreateHeightfield(&heightfield, 1, 1, bmin, bmax, 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 42, 1);

            var span = heightfield.Spans[0];
            Assert.IsTrue(span != null);
            Assert.AreEqual(0u, span->SMin);
            Assert.AreEqual(1u, span->SMax);
            Assert.AreEqual(42u, span->Area);
            Assert.IsTrue(span->Next == null);
        }

        [Test]
        public void AddSpanMergesWithExistingSpan()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            var bmin = float3.zero;
            var bmax = new float3(1, 1, 1);
            Recast.CreateHeightfield(&heightfield, 1, 1, bmin, bmax, 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 42, 1);
            Recast.AddSpan(&heightfield, 0, 0, 1, 2, 42, 1);

            var span = heightfield.Spans[0];
            Assert.IsTrue(span != null);
            Assert.AreEqual(0u, span->SMin);
            Assert.AreEqual(2u, span->SMax);
            Assert.AreEqual(42u, span->Area);
            Assert.IsTrue(span->Next == null);
        }

        [Test]
        public void AddSpanMergesAboveAndBelow()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            var bmin = float3.zero;
            var bmax = new float3(1, 1, 1);
            Recast.CreateHeightfield(&heightfield, 1, 1, bmin, bmax, 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, 0, 1, 42, 1);
            Recast.AddSpan(&heightfield, 0, 0, 2, 3, 42, 1);
            Recast.AddSpan(&heightfield, 0, 0, 1, 2, 42, 1);

            var span = heightfield.Spans[0];
            Assert.IsTrue(span != null);
            Assert.AreEqual(0u, span->SMin);
            Assert.AreEqual(3u, span->SMax);
            Assert.AreEqual(42u, span->Area);
            Assert.IsTrue(span->Next == null);
        }

        [TestCase(1u, 1u)]
        [TestCase(2u, 1u)]
        public void AddSpanIgnoresInvalidSpanAndKeepsColumnUsable(uint min, uint max)
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            var bmin = float3.zero;
            var bmax = new float3(1, 1, 1);
            Recast.CreateHeightfield(&heightfield, 1, 1, bmin, bmax, 1, 1);

            Recast.AddSpan(&heightfield, 0, 0, min, max, 42, 1);

            Assert.IsTrue(heightfield.Spans[0] == null);

            Recast.AddSpan(&heightfield, 0, 0, 0, 2, 7, 1);

            var span = heightfield.Spans[0];
            Assert.IsTrue(span != null);
            Assert.AreEqual(0u, span->SMin);
            Assert.AreEqual(2u, span->SMax);
            Assert.AreEqual(7u, span->Area);
            Assert.IsTrue(span->Next == null);
        }

        [Test]
        public void RasterizeTriangleWritesExpectedSpans()
        {
            var verts = stackalloc float3[3];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);

            Recast.CalcBounds(verts, 3, out var bmin, out var bmax);

            const float cellSize = 0.5f;
            const float cellHeight = 0.5f;

            Recast.CalcGridSize(in bmin, in bmax, cellSize, out var width, out var height);

            var heightfield = new RcHeightfield(Allocator.Temp);

            Recast.CreateHeightfield(&heightfield, width, height, bmin, bmax, cellSize, cellHeight);

            Recast.RasterizeTriangle(verts[0], verts[1], verts[2], 42, &heightfield);

            Assert.IsTrue(heightfield.Spans[0 + (0 * width)] != null);
            Assert.IsTrue(heightfield.Spans[1 + (0 * width)] == null);
            Assert.IsTrue(heightfield.Spans[0 + (1 * width)] != null);
            Assert.IsTrue(heightfield.Spans[1 + (1 * width)] != null);

            var first = heightfield.Spans[0 + (0 * width)];
            Assert.AreEqual(0u, first->SMin);
            Assert.AreEqual(1u, first->SMax);
            Assert.AreEqual(42u, first->Area);
            Assert.IsTrue(first->Next == null);

            var second = heightfield.Spans[0 + (1 * width)];
            Assert.AreEqual(0u, second->SMin);
            Assert.AreEqual(1u, second->SMax);
            Assert.AreEqual(42u, second->Area);
            Assert.IsTrue(second->Next == null);

            var third = heightfield.Spans[1 + (1 * width)];
            Assert.AreEqual(0u, third->SMin);
            Assert.AreEqual(1u, third->SMax);
            Assert.AreEqual(42u, third->Area);
            Assert.IsTrue(third->Next == null);
        }

        [Test]
        public void RasterizeTriangleOutsideHeightfieldCreatesNoSpans()
        {
            var heightfield = new RcHeightfield(Allocator.Temp);

            var bmin = float3.zero;
            var bmax = new float3(10, 10, 10);
            Recast.CreateHeightfield(&heightfield, 10, 10, bmin, bmax, 1, 1);

            var verts = stackalloc float3[3];
            verts[0] = new float3(-10, 5.5f, -10);
            verts[1] = new float3(-10, 5.5f, 3);
            verts[2] = new float3(3, 5.5f, -10);

            Recast.RasterizeTriangle(verts[0], verts[1], verts[2], 42, &heightfield);

            for (var x = 0; x < heightfield.Width; x++)
            {
                for (var z = 0; z < heightfield.Height; z++)
                {
                    Assert.IsTrue(heightfield.Spans[x + (z * heightfield.Width)] == null);
                }
            }
        }

        [Test]
        public void RasterizeSkinnyTrianglesAlongXAxisCompletes()
        {
            var verts = stackalloc float3[6];
            verts[0] = new float3(5, 0, 0.005f);
            verts[1] = new float3(5, 0, -0.005f);
            verts[2] = new float3(-5, 0, 0.005f);
            verts[3] = new float3(-5, 0, 0.005f);
            verts[4] = new float3(5, 0, -0.005f);
            verts[5] = new float3(-5, 0, -0.005f);

            Recast.CalcBounds(verts, 6, out var bmin, out var bmax);

            var heightfield = new RcHeightfield(Allocator.Temp);

            Recast.CalcGridSize(in bmin, in bmax, 1, out var width, out var height);
            Recast.CreateHeightfield(&heightfield, width, height, bmin, bmax, 1, 1);

            var areas = stackalloc byte[2];
            areas[0] = 42;
            areas[1] = 42;

            Recast.RasterizeTriangles(verts, areas, 2, &heightfield);
        }

        [Test]
        public void RasterizeSkinnyTrianglesAlongZAxisCompletes()
        {
            var verts = stackalloc float3[6];
            verts[0] = new float3(0.005f, 0, 5);
            verts[1] = new float3(-0.005f, 0, 5);
            verts[2] = new float3(0.005f, 0, -5);
            verts[3] = new float3(0.005f, 0, -5);
            verts[4] = new float3(-0.005f, 0, 5);
            verts[5] = new float3(-0.005f, 0, -5);

            Recast.CalcBounds(verts, 6, out var bmin, out var bmax);

            var heightfield = new RcHeightfield(Allocator.Temp);

            Recast.CalcGridSize(in bmin, in bmax, 1, out var width, out var height);
            Recast.CreateHeightfield(&heightfield, width, height, bmin, bmax, 1, 1);

            var areas = stackalloc byte[2];
            areas[0] = 42;
            areas[1] = 42;

            Recast.RasterizeTriangles(verts, areas, 2, &heightfield);
        }

        [Test]
        public void RasterizeTrianglesWithIndicesProducesExpectedSpans()
        {
            var verts = stackalloc float3[4];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);
            verts[3] = new float3(0, 0, 1);

            Recast.CalcBounds(verts, 4, out var bmin, out var bmax);

            const float cellSize = 0.5f;
            const float cellHeight = 0.5f;

            Recast.CalcGridSize(in bmin, in bmax, cellSize, out var width, out var height);

            var heightfield = new RcHeightfield(Allocator.Temp);

            Recast.CreateHeightfield(&heightfield, width, height, bmin, bmax, cellSize, cellHeight);

            var tris = stackalloc int3[2];
            tris[0] = new int3(0, 1, 2);
            tris[1] = new int3(0, 3, 1);

            var areas = stackalloc byte[2];
            areas[0] = 1;
            areas[1] = 2;

            Recast.RasterizeTriangles(verts, tris, areas, 2, &heightfield);

            AssertCrossPattern(&heightfield, width);
        }

        [Test]
        public void RasterizeTrianglesWithUshortIndicesProducesExpectedSpans()
        {
            var verts = stackalloc float3[4];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);
            verts[3] = new float3(0, 0, 1);

            Recast.CalcBounds(verts, 4, out var bmin, out var bmax);

            const float cellSize = 0.5f;
            const float cellHeight = 0.5f;

            Recast.CalcGridSize(in bmin, in bmax, cellSize, out var width, out var height);

            var heightfield = new RcHeightfield(Allocator.Temp);

            Recast.CreateHeightfield(&heightfield, width, height, bmin, bmax, cellSize, cellHeight);

            var tris = stackalloc ushort3[2];
            tris[0] = new ushort3(0, 1, 2);
            tris[1] = new ushort3(0, 3, 1);

            var areas = stackalloc byte[2];
            areas[0] = 1;
            areas[1] = 2;

            Recast.RasterizeTriangles(verts, tris, areas, 2, &heightfield);

            AssertCrossPattern(&heightfield, width);
        }

        [Test]
        public void RasterizeTriangleListProducesExpectedSpans()
        {
            var verts = stackalloc float3[6];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);
            verts[3] = new float3(0, 0, 0);
            verts[4] = new float3(0, 0, 1);
            verts[5] = new float3(1, 0, 0);

            Recast.CalcBounds(verts, 6, out var bmin, out var bmax);

            const float cellSize = 0.5f;
            const float cellHeight = 0.5f;

            Recast.CalcGridSize(in bmin, in bmax, cellSize, out var width, out var height);

            var heightfield = new RcHeightfield(Allocator.Temp);

            Recast.CreateHeightfield(&heightfield, width, height, bmin, bmax, cellSize, cellHeight);

            var areas = stackalloc byte[2];
            areas[0] = 1;
            areas[1] = 2;

            Recast.RasterizeTriangles(verts, areas, 2, &heightfield);

            AssertCrossPattern(&heightfield, width);
        }

        private static void AssertCrossPattern(RcHeightfield* heightfield, int width)
        {
            Assert.IsTrue(heightfield->Spans[0 + (0 * width)] != null);
            Assert.IsTrue(heightfield->Spans[0 + (1 * width)] != null);
            Assert.IsTrue(heightfield->Spans[0 + (2 * width)] != null);
            Assert.IsTrue(heightfield->Spans[0 + (3 * width)] != null);
            Assert.IsTrue(heightfield->Spans[1 + (0 * width)] == null);
            Assert.IsTrue(heightfield->Spans[1 + (3 * width)] == null);
            Assert.IsTrue(heightfield->Spans[1 + (1 * width)] != null);
            Assert.IsTrue(heightfield->Spans[1 + (2 * width)] != null);

            var span00 = heightfield->Spans[0 + (0 * width)];
            Assert.AreEqual(0u, span00->SMin);
            Assert.AreEqual(1u, span00->SMax);
            Assert.AreEqual(1u, span00->Area);
            Assert.IsTrue(span00->Next == null);

            var span01 = heightfield->Spans[0 + (1 * width)];
            Assert.AreEqual(0u, span01->SMin);
            Assert.AreEqual(1u, span01->SMax);
            Assert.AreEqual(1u, span01->Area);
            Assert.IsTrue(span01->Next == null);

            var span02 = heightfield->Spans[0 + (2 * width)];
            Assert.AreEqual(0u, span02->SMin);
            Assert.AreEqual(1u, span02->SMax);
            Assert.AreEqual(2u, span02->Area);
            Assert.IsTrue(span02->Next == null);

            var span03 = heightfield->Spans[0 + (3 * width)];
            Assert.AreEqual(0u, span03->SMin);
            Assert.AreEqual(1u, span03->SMax);
            Assert.AreEqual(2u, span03->Area);
            Assert.IsTrue(span03->Next == null);

            var span11 = heightfield->Spans[1 + (1 * width)];
            Assert.AreEqual(0u, span11->SMin);
            Assert.AreEqual(1u, span11->SMax);
            Assert.AreEqual(1u, span11->Area);
            Assert.IsTrue(span11->Next == null);

            var span12 = heightfield->Spans[1 + (2 * width)];
            Assert.AreEqual(0u, span12->SMin);
            Assert.AreEqual(1u, span12->SMax);
            Assert.AreEqual(2u, span12->Area);
            Assert.IsTrue(span12->Next == null);
        }
    }
}
