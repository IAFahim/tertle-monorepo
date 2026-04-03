// <copyright file="DetourNavMeshQueryTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast.Tests
{
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Mathematics;

    public unsafe class DetourNavMeshQueryTests
    {
        [Test]
        public void RaycastKeepsFullHitResultWhenCorridorBufferTruncates()
        {
            var navMesh = DtNavMesh.Alloc(Allocator.Temp);
            var query = default(DtNavMeshQuery);

            try
            {
                var navData = CreateLinearCorridorNavMeshData(out var navDataSize);

                Assert.IsTrue(navMesh != null);
                Assert.AreEqual(DtStatus.Success, navMesh->InitSingleTile(navData, navDataSize, DtTileFlags.TileFreeData));

                query = new DtNavMeshQuery(navMesh, 32, Allocator.Temp);

                var filter = DtQueryFilter.CreateDefault();
                var tile = navMesh->GetTileAt(0, 0, 0);
                Assert.IsTrue(tile != null);

                var startRef = GetPolyRef(navMesh, tile, 0);
                var secondRef = GetPolyRef(navMesh, tile, 1);

                var path = stackalloc DtPolyRef[2];
                var startPos = new float3(0.5f, 0f, 0.5f);
                var endPos = new float3(3.5f, 0f, 0.5f);

                var status = query.Raycast(startRef, startPos, endPos, ref filter, out var t, out _, path, out var pathCount, 2);

                Assert.IsTrue(Detour.StatusSucceed(status));
                Assert.IsTrue(Detour.StatusDetail(status, DtStatus.BufferTooSmall));
                Assert.IsFalse(Detour.StatusDetail(status, DtStatus.PartialResult));
                Assert.AreEqual(2, pathCount);
                Assert.AreEqual(startRef, path[0]);
                Assert.AreEqual(secondRef, path[1]);
                Assert.AreEqual(float.MaxValue, t);
            }
            finally
            {
                query.Dispose();
                DtNavMesh.Free(navMesh);
            }
        }

        private static DtNavMeshData CreateLinearCorridorNavMeshData(out int dataSize)
        {
            var verts = stackalloc ushort3[10];
            for (var x = 0; x <= 4; ++x)
            {
                verts[x] = new ushort3((ushort)x, 0, 0);
                verts[x + 5] = new ushort3((ushort)x, 0, 1);
            }

            var polys = stackalloc ushort[4 * 4 * 2];
            for (var i = 0; i < 4 * 4 * 2; ++i)
            {
                polys[i] = Detour.MeshNullIDX;
            }

            const ushort border = Detour.DTExtLink | 0xf;
            SetPoly(polys, 0, 0, 5, 6, 1, border, border, 1, border);
            SetPoly(polys, 1, 1, 6, 7, 2, 0, border, 2, border);
            SetPoly(polys, 2, 2, 7, 8, 3, 1, border, 3, border);
            SetPoly(polys, 3, 3, 8, 9, 4, 2, border, border, border);

            var polyFlags = stackalloc ushort[4];
            var polyAreas = stackalloc byte[4];
            for (var i = 0; i < 4; ++i)
            {
                polyFlags[i] = 1;
                polyAreas[i] = Recast.RCWalkableArea;
            }

            var createParams = new DtNavMeshCreateParams
            {
                Verts = verts,
                VertCount = 10,
                Polys = polys,
                PolyFlags = polyFlags,
                PolyAreas = polyAreas,
                PolyCount = 4,
                Nvp = 4,
                TileX = 0,
                TileY = 0,
                TileLayer = 0,
                Bmin = float3.zero,
                Bmax = new float3(4f, 1f, 1f),
                WalkableHeight = 2f,
                WalkableRadius = 0f,
                WalkableClimb = 0f,
                Cs = 1f,
                Ch = 1f,
                BuildBvTree = true,
            };

            Assert.IsTrue(Detour.CreateNavMeshData(&createParams, out var navData, out dataSize, Allocator.Temp));
            return navData;
        }

        private static DtPolyRef GetPolyRef(DtNavMesh* navMesh, DtMeshTile* tile, uint polyIndex)
        {
            var tileRef = navMesh->GetTileRef(tile);
            navMesh->DecodePolyId(tileRef, out var salt, out var tileIndex, out _);
            return navMesh->EncodePolyId(salt, tileIndex, polyIndex);
        }

        private static void SetPoly(
            ushort* polys, int polyIndex, ushort v0, ushort v1, ushort v2, ushort v3, ushort n0, ushort n1, ushort n2, ushort n3)
        {
            var poly = polys + (polyIndex * 8);
            poly[0] = v0;
            poly[1] = v1;
            poly[2] = v2;
            poly[3] = v3;
            poly[4] = n0;
            poly[5] = n1;
            poly[6] = n2;
            poly[7] = n3;
        }
    }
}
