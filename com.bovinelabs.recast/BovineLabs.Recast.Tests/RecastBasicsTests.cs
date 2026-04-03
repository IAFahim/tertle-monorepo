// <copyright file="RecastBasicsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast.Tests
{
    using System.Text;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    public unsafe class RecastBasicsTests
    {
        [Test]
        public void CalcBoundsSingleVertex()
        {
            var verts = stackalloc float3[1];
            verts[0] = new float3(1, 2, 3);

            Recast.CalcBounds(verts, 1, out var bmin, out var bmax);

            Assert.AreEqual(1f, bmin.x);
            Assert.AreEqual(2f, bmin.y);
            Assert.AreEqual(3f, bmin.z);

            Assert.AreEqual(1f, bmax.x);
            Assert.AreEqual(2f, bmax.y);
            Assert.AreEqual(3f, bmax.z);
        }

        [Test]
        public void CalcBoundsMultipleVertices()
        {
            var verts = stackalloc float3[2];
            verts[0] = new float3(1, 2, 3);
            verts[1] = new float3(0, 2, 5);

            Recast.CalcBounds(verts, 2, out var bmin, out var bmax);

            Assert.AreEqual(0f, bmin.x);
            Assert.AreEqual(2f, bmin.y);
            Assert.AreEqual(3f, bmin.z);

            Assert.AreEqual(1f, bmax.x);
            Assert.AreEqual(2f, bmax.y);
            Assert.AreEqual(5f, bmax.z);
        }

        [Test]
        public void CalcGridSizeComputesExpectedDimensions()
        {
            var verts = stackalloc float3[2];
            verts[0] = new float3(1, 2, 3);
            verts[1] = new float3(0, 2, 6);

            Recast.CalcBounds(verts, 2, out var bmin, out var bmax);

            const float cellSize = 1.5f;

            Recast.CalcGridSize(in bmin, in bmax, cellSize, out var width, out var height);

            Assert.AreEqual(1, width);
            Assert.AreEqual(2, height);
        }

        [Test]
        public void CreateHeightfieldInitializesData()
        {
            var verts = stackalloc float3[2];
            verts[0] = new float3(1, 2, 3);
            verts[1] = new float3(0, 2, 6);

            Recast.CalcBounds(verts, 2, out var bmin, out var bmax);

            const float cellSize = 1.5f;
            const float cellHeight = 2f;

            Recast.CalcGridSize(in bmin, in bmax, cellSize, out var width, out var height);

            var heightfield = new RcHeightfield(Allocator.Temp);

            Recast.CreateHeightfield(&heightfield, width, height, bmin, bmax, cellSize, cellHeight);

            Assert.AreEqual(width, heightfield.Width);
            Assert.AreEqual(height, heightfield.Height);

            Assert.AreEqual(bmin.x, heightfield.Bmin.x);
            Assert.AreEqual(bmin.y, heightfield.Bmin.y);
            Assert.AreEqual(bmin.z, heightfield.Bmin.z);

            Assert.AreEqual(bmax.x, heightfield.Bmax.x);
            Assert.AreEqual(bmax.y, heightfield.Bmax.y);
            Assert.AreEqual(bmax.z, heightfield.Bmax.z);

            Assert.AreEqual(cellSize, heightfield.Cs);
            Assert.AreEqual(cellHeight, heightfield.Ch);

            Assert.IsTrue(heightfield.Spans != null);
            Assert.IsTrue(heightfield.Pools == null);
            Assert.IsTrue(heightfield.Freelist == null);
        }

        [Test]
        public void MarkWalkableTrianglesMarksWalkable()
        {
            const float walkableSlopeAngle = 45f;
            var verts = stackalloc float3[3];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);

            var tris = stackalloc int3[1];
            tris[0] = new int3(0, 1, 2);

            var area = Recast.RCNullArea;

            Recast.MarkWalkableTriangles(walkableSlopeAngle, verts, tris, &area, 1);

            Assert.AreEqual(Recast.RCWalkableArea, area);
        }

        [Test]
        public void MarkWalkableTrianglesLeavesNonWalkable()
        {
            const float walkableSlopeAngle = 45f;
            var verts = stackalloc float3[3];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);

            var tris = stackalloc int3[1];
            tris[0] = new int3(0, 2, 1);

            var area = Recast.RCNullArea;

            Recast.MarkWalkableTriangles(walkableSlopeAngle, verts, tris, &area, 1);

            Assert.AreEqual(Recast.RCNullArea, area);
        }

        [Test]
        public void MarkWalkableTrianglesPreservesCustomArea()
        {
            const float walkableSlopeAngle = 45f;
            var verts = stackalloc float3[3];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);

            var tris = stackalloc int3[1];
            tris[0] = new int3(0, 2, 1);

            byte area = 42;

            Recast.MarkWalkableTriangles(walkableSlopeAngle, verts, tris, &area, 1);

            Assert.AreEqual(42, area);
        }

        [Test]
        public void MarkWalkableTrianglesTreatsMaxSlopeAsUnwalkable()
        {
            const float walkableSlopeAngle = 0f;
            var verts = stackalloc float3[3];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);

            var tris = stackalloc int3[1];
            tris[0] = new int3(0, 1, 2);

            var area = Recast.RCNullArea;

            Recast.MarkWalkableTriangles(walkableSlopeAngle, verts, tris, &area, 1);

            Assert.AreEqual(Recast.RCNullArea, area);
        }

        [Test]
        public void ClearUnwalkableTrianglesMarksNull()
        {
            const float walkableSlopeAngle = 45f;
            var verts = stackalloc float3[3];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);

            var tris = stackalloc int3[1];
            tris[0] = new int3(0, 2, 1);

            byte area = 42;

            Recast.ClearUnwalkableTriangles(walkableSlopeAngle, verts, tris, &area, 1);

            Assert.AreEqual(Recast.RCNullArea, area);
        }

        [Test]
        public void ClearUnwalkableTrianglesPreservesWalkable()
        {
            const float walkableSlopeAngle = 45f;
            var verts = stackalloc float3[3];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);

            var tris = stackalloc int3[1];
            tris[0] = new int3(0, 1, 2);

            byte area = 42;

            Recast.ClearUnwalkableTriangles(walkableSlopeAngle, verts, tris, &area, 1);

            Assert.AreEqual(42, area);
        }

        [Test]
        public void ClearUnwalkableTrianglesTreatsMaxSlopeAsUnwalkable()
        {
            const float walkableSlopeAngle = 0f;
            var verts = stackalloc float3[3];
            verts[0] = new float3(0, 0, 0);
            verts[1] = new float3(1, 0, 0);
            verts[2] = new float3(0, 0, -1);

            var tris = stackalloc int3[1];
            tris[0] = new int3(0, 1, 2);

            byte area = 42;

            Recast.ClearUnwalkableTriangles(walkableSlopeAngle, verts, tris, &area, 1);

            Assert.AreEqual(Recast.RCNullArea, area);
        }

        [Test]
        public void BuildPolyMeshDetailReferencesInteriorSampleVertices()
        {
            var polyMesh = new RcPolyMesh(Allocator.Temp);
            var compactHeightfield = new RcCompactHeightfield(Allocator.Temp);
            var detailMesh = new RcPolyMeshDetail(Allocator.Temp);

            try
            {
                InitializeDetailTestPolyMesh(&polyMesh);
                InitializeDetailTestCompactHeightfield(&compactHeightfield);

                Recast.BuildPolyMeshDetail(&polyMesh, &compactHeightfield, 1f, 0.1f, &detailMesh);

                Assert.AreEqual(1, detailMesh.NMeshes);
                Assert.Greater(detailMesh.NVerts, polyMesh.NVerts);
                Assert.Greater((int)detailMesh.Meshes[0].y, polyMesh.NVerts);
                Assert.Greater(detailMesh.NTris, 0, "Expected the detail triangulation rebuild to keep triangles.");

                var referencesExtraVertex = false;
                for (var i = 0; i < detailMesh.NTris; ++i)
                {
                    var tri = detailMesh.Tris[i];
                    if (tri.x >= polyMesh.NVerts || tri.y >= polyMesh.NVerts || tri.z >= polyMesh.NVerts)
                    {
                        referencesExtraVertex = true;
                        break;
                    }
                }

                Assert.IsTrue(
                    referencesExtraVertex,
                    $"Expected at least one triangle to reference an extra detail vertex. NVerts={detailMesh.NVerts}, NTris={detailMesh.NTris}, Tris={DescribeDetailTris(&detailMesh)}");
            }
            finally
            {
                detailMesh.Dispose();
                compactHeightfield.Dispose();
                polyMesh.Dispose();
            }
        }

        [Test]
        public void BuildPolyMeshDetailHandlesLargeScaleRefinementWithoutCrashing()
        {
            var polyMesh = new RcPolyMesh(Allocator.Temp);
            var compactHeightfield = new RcCompactHeightfield(Allocator.Temp);
            var detailMesh = new RcPolyMeshDetail(Allocator.Temp);
            const float cellSize = 32768f;

            try
            {
                InitializeDetailTestPolyMesh(&polyMesh, cellSize);
                InitializeDetailTestCompactHeightfield(&compactHeightfield, cellSize);

                Recast.BuildPolyMeshDetail(&polyMesh, &compactHeightfield, cellSize, cellSize * 0.1f, &detailMesh);

                Assert.AreEqual(1, detailMesh.NMeshes);
                Assert.IsTrue(detailMesh.Meshes != null);
                Assert.GreaterOrEqual((int)detailMesh.Meshes[0].y, polyMesh.NVerts);
            }
            finally
            {
                detailMesh.Dispose();
                compactHeightfield.Dispose();
                polyMesh.Dispose();
            }
        }

        private static void InitializeDetailTestPolyMesh(RcPolyMesh* polyMesh, float cellSize = 1f)
        {
            polyMesh->NVerts = 4;
            polyMesh->NPolys = 1;
            polyMesh->MaxPolys = 1;
            polyMesh->Nvp = 4;
            polyMesh->BMin = float3.zero;
            polyMesh->BMax = new float3(2 * cellSize, 2, 2 * cellSize);
            polyMesh->CellSize = cellSize;
            polyMesh->CellHeight = 1f;
            polyMesh->BorderSize = 0;
            polyMesh->MaxEdgeError = 1f;

            polyMesh->Verts = (ushort3*)AllocatorManager.Allocate(polyMesh->Allocator, sizeof(ushort3) * polyMesh->NVerts, UnsafeUtility.AlignOf<ushort3>());
            polyMesh->Polys = (ushort*)AllocatorManager.Allocate(polyMesh->Allocator, sizeof(ushort) * polyMesh->MaxPolys * polyMesh->Nvp * 2, UnsafeUtility.AlignOf<ushort>());
            polyMesh->Regs = (ushort*)AllocatorManager.Allocate(polyMesh->Allocator, sizeof(ushort) * polyMesh->MaxPolys, UnsafeUtility.AlignOf<ushort>());
            polyMesh->Flags = (ushort*)AllocatorManager.Allocate(polyMesh->Allocator, sizeof(ushort) * polyMesh->MaxPolys, UnsafeUtility.AlignOf<ushort>());
            polyMesh->Areas = (byte*)AllocatorManager.Allocate(polyMesh->Allocator, sizeof(byte) * polyMesh->MaxPolys, UnsafeUtility.AlignOf<byte>());

            polyMesh->Verts[0] = new ushort3(0, 0, 0);
            polyMesh->Verts[1] = new ushort3(0, 0, 2);
            polyMesh->Verts[2] = new ushort3(2, 0, 2);
            polyMesh->Verts[3] = new ushort3(2, 0, 0);

            for (var i = 0; i < polyMesh->MaxPolys * polyMesh->Nvp * 2; ++i)
            {
                polyMesh->Polys[i] = Recast.RCMeshNullIdx;
            }

            polyMesh->Polys[0] = 0;
            polyMesh->Polys[1] = 1;
            polyMesh->Polys[2] = 2;
            polyMesh->Polys[3] = 3;
            polyMesh->Regs[0] = 1;
            polyMesh->Flags[0] = 0;
            polyMesh->Areas[0] = Recast.RCWalkableArea;
        }

        private static void InitializeDetailTestCompactHeightfield(RcCompactHeightfield* compactHeightfield, float cellSize = 1f)
        {
            compactHeightfield->Width = 3;
            compactHeightfield->Height = 3;
            compactHeightfield->SpanCount = 9;
            compactHeightfield->WalkableHeight = 2;
            compactHeightfield->WalkableClimb = 1;
            compactHeightfield->BorderSize = 0;
            compactHeightfield->MaxDistance = 0;
            compactHeightfield->MaxRegions = 1;
            compactHeightfield->BMin = float3.zero;
            compactHeightfield->BMax = new float3(3 * cellSize, 3, 3 * cellSize);
            compactHeightfield->CellSize = cellSize;
            compactHeightfield->CellHeight = 1f;

            compactHeightfield->Cells = (RcCompactCell*)AllocatorManager.Allocate(
                compactHeightfield->Allocator,
                sizeof(RcCompactCell) * compactHeightfield->Width * compactHeightfield->Height,
                UnsafeUtility.AlignOf<RcCompactCell>());
            compactHeightfield->Spans = (RcCompactSpan*)AllocatorManager.Allocate(
                compactHeightfield->Allocator,
                sizeof(RcCompactSpan) * compactHeightfield->SpanCount,
                UnsafeUtility.AlignOf<RcCompactSpan>());

            for (var z = 0; z < compactHeightfield->Height; ++z)
            {
                for (var x = 0; x < compactHeightfield->Width; ++x)
                {
                    var index = x + (z * compactHeightfield->Width);
                    compactHeightfield->Cells[index].Index = (uint)index;
                    compactHeightfield->Cells[index].Count = 1;

                    var span = &compactHeightfield->Spans[index];
                    span->Y = (ushort)(x == 1 && z == 1 ? 2 : 0);
                    span->Reg = 1;
                    span->H = 1;

                    for (var dir = 0; dir < 4; ++dir)
                    {
                        Recast.SetCon(span, dir, Recast.RCNotConnected);
                    }
                }
            }
        }

        private static string DescribeDetailTris(RcPolyMeshDetail* detailMesh)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < detailMesh->NTris; ++i)
            {
                var tri = detailMesh->Tris[i];
                if (builder.Length > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append('[')
                    .Append(tri.x)
                    .Append(',')
                    .Append(tri.y)
                    .Append(',')
                    .Append(tri.z)
                    .Append(']');
            }

            return builder.ToString();
        }
    }
}
