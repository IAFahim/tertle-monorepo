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
        private static readonly float3 LongRangeQueryStartPosition = new(0.1f, 0f, 0.8f);
        private static readonly float3 LongRangeQueryEndPosition = new(2.9f, 0f, 0.2f);
        private static readonly float3 LongRangeStartPosition = new(0.5f, 0f, 0.5f);
        private static readonly float3 LongRangeEndPosition = new(2.5f, 0f, 0.5f);

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

        [Test]
        public void FindPath_LongRangeOffMeshLinkAcrossMultipleTiles_CompletesWithoutPartialResult()
        {
            var navMesh = DtNavMesh.Alloc(Allocator.Temp);
            var query = default(DtNavMeshQuery);

            try
            {
                InitializeLongRangeOffMeshNavMesh(navMesh);

                query = new DtNavMeshQuery(navMesh, 32, Allocator.Temp);
                var filter = DtQueryFilter.CreateDefault();

                var sourceTile = navMesh->GetTileAt(0, 0, 0);
                var destinationTile = navMesh->GetTileAt(2, 0, 0);
                Assert.IsTrue(sourceTile != null);
                Assert.IsTrue(destinationTile != null);

                var startRef = GetPolyRef(navMesh, sourceTile, 0);
                var offMeshRef = GetPolyRef(navMesh, sourceTile, 1);
                var endRef = GetPolyRef(navMesh, destinationTile, 0);

                var path = stackalloc DtPolyRef[8];
                var status = query.FindPath(startRef, endRef, LongRangeQueryStartPosition, LongRangeQueryEndPosition, ref filter, path, out var pathCount, 8);

                Assert.IsTrue(Detour.StatusSucceed(status));
                Assert.IsFalse(Detour.StatusDetail(status, DtStatus.PartialResult));
                Assert.AreEqual(3, pathCount);
                Assert.AreEqual(startRef, path[0]);
                Assert.AreEqual(offMeshRef, path[1]);
                Assert.AreEqual(endRef, path[2]);
            }
            finally
            {
                query.Dispose();
                DtNavMesh.Free(navMesh);
            }
        }

        [Test]
        public void FindStraightPath_LongRangeOffMeshLink_EmitsOffMeshConnectionFlag()
        {
            var navMesh = DtNavMesh.Alloc(Allocator.Temp);
            var query = default(DtNavMeshQuery);

            try
            {
                InitializeLongRangeOffMeshNavMesh(navMesh);

                query = new DtNavMeshQuery(navMesh, 32, Allocator.Temp);
                var filter = DtQueryFilter.CreateDefault();

                var sourceTile = navMesh->GetTileAt(0, 0, 0);
                var destinationTile = navMesh->GetTileAt(2, 0, 0);
                Assert.IsTrue(sourceTile != null);
                Assert.IsTrue(destinationTile != null);

                var startRef = GetPolyRef(navMesh, sourceTile, 0);
                var endRef = GetPolyRef(navMesh, destinationTile, 0);

                var path = stackalloc DtPolyRef[8];
                var status = query.FindPath(startRef, endRef, LongRangeQueryStartPosition, LongRangeQueryEndPosition, ref filter, path, out var pathCount, 8);
                Assert.IsTrue(Detour.StatusSucceed(status));
                Assert.IsFalse(Detour.StatusDetail(status, DtStatus.PartialResult));

                var straightPath = stackalloc float3[8];
                var straightFlags = stackalloc DtStraightPathFlags[8];
                var straightRefs = stackalloc DtPolyRef[8];
                status = query.FindStraightPath(
                    LongRangeQueryStartPosition,
                    LongRangeQueryEndPosition,
                    path,
                    pathCount,
                    straightPath,
                    straightFlags,
                    straightRefs,
                    out var straightPathCount,
                    8);

                Assert.IsTrue(Detour.StatusSucceed(status));
                Assert.IsFalse(Detour.StatusDetail(status, DtStatus.PartialResult));

                var foundOffMeshCorner = false;
                for (var i = 0; i < straightPathCount; ++i)
                {
                    if ((straightFlags[i] & DtStraightPathFlags.StraightpathOffmeshConnection) != 0)
                    {
                        foundOffMeshCorner = true;
                        break;
                    }
                }

                Assert.IsTrue(foundOffMeshCorner);
            }
            finally
            {
                query.Dispose();
                DtNavMesh.Free(navMesh);
            }
        }

        [Test]
        public void AddingDestinationTileAfterSourceTile_StitchesLongRangeOffMeshWithoutReloadingSource()
        {
            var navMesh = DtNavMesh.Alloc(Allocator.Temp);
            var query = default(DtNavMeshQuery);

            try
            {
                InitializeLongRangeOffMeshNavMeshParams(navMesh);
                AddLongRangeSourceTile(navMesh);

                var sourceTile = navMesh->GetTileAt(0, 0, 0);
                Assert.IsTrue(sourceTile != null);
                Assert.AreEqual(1, CountPolyLinks(sourceTile, 1));

                AddLongRangeDestinationTile(navMesh);

                sourceTile = navMesh->GetTileAt(0, 0, 0);
                var destinationTile = navMesh->GetTileAt(2, 0, 0);
                Assert.IsTrue(sourceTile != null);
                Assert.IsTrue(destinationTile != null);
                Assert.AreEqual(2, CountPolyLinks(sourceTile, 1));

                query = new DtNavMeshQuery(navMesh, 32, Allocator.Temp);
                var filter = DtQueryFilter.CreateDefault();
                var startRef = GetPolyRef(navMesh, sourceTile, 0);
                var endRef = GetPolyRef(navMesh, destinationTile, 0);

                var path = stackalloc DtPolyRef[8];
                var status = query.FindPath(startRef, endRef, LongRangeQueryStartPosition, LongRangeQueryEndPosition, ref filter, path, out var pathCount, 8);

                Assert.IsTrue(Detour.StatusSucceed(status));
                Assert.IsFalse(Detour.StatusDetail(status, DtStatus.PartialResult));
                Assert.AreEqual(3, pathCount);
            }
            finally
            {
                query.Dispose();
                DtNavMesh.Free(navMesh);
            }
        }

        [Test]
        public void RemovingAndReaddingDestinationTile_UnstitchesAndRestitchesLongRangeOffMesh()
        {
            var navMesh = DtNavMesh.Alloc(Allocator.Temp);
            var query = default(DtNavMeshQuery);

            try
            {
                InitializeLongRangeOffMeshNavMesh(navMesh);

                var sourceTile = navMesh->GetTileAt(0, 0, 0);
                var destinationTile = navMesh->GetTileAt(2, 0, 0);
                Assert.IsTrue(sourceTile != null);
                Assert.IsTrue(destinationTile != null);
                Assert.AreEqual(2, CountPolyLinks(sourceTile, 1));

                var destinationTileRef = navMesh->GetTileRef(destinationTile);
                Assert.AreEqual(DtStatus.Success, navMesh->RemoveTile(destinationTileRef, out _, out _));
                Assert.IsTrue(navMesh->GetTileAt(2, 0, 0) == null);

                sourceTile = navMesh->GetTileAt(0, 0, 0);
                Assert.IsTrue(sourceTile != null);
                Assert.AreEqual(1, CountPolyLinks(sourceTile, 1));

                AddLongRangeDestinationTile(navMesh);

                sourceTile = navMesh->GetTileAt(0, 0, 0);
                destinationTile = navMesh->GetTileAt(2, 0, 0);
                Assert.IsTrue(sourceTile != null);
                Assert.IsTrue(destinationTile != null);
                Assert.AreEqual(2, CountPolyLinks(sourceTile, 1));

                query = new DtNavMeshQuery(navMesh, 32, Allocator.Temp);
                var filter = DtQueryFilter.CreateDefault();
                var startRef = GetPolyRef(navMesh, sourceTile, 0);
                var endRef = GetPolyRef(navMesh, destinationTile, 0);

                var path = stackalloc DtPolyRef[8];
                var status = query.FindPath(startRef, endRef, LongRangeQueryStartPosition, LongRangeQueryEndPosition, ref filter, path, out var pathCount, 8);

                Assert.IsTrue(Detour.StatusSucceed(status));
                Assert.IsFalse(Detour.StatusDetail(status, DtStatus.PartialResult));
                Assert.AreEqual(3, pathCount);
            }
            finally
            {
                query.Dispose();
                DtNavMesh.Free(navMesh);
            }
        }

        [Test]
        public void AddingSecondDestinationLayer_DoesNotDuplicateLongRangeOffMeshLanding()
        {
            var navMesh = DtNavMesh.Alloc(Allocator.Temp);

            try
            {
                InitializeLongRangeOffMeshNavMeshParams(navMesh);
                AddLongRangeSourceTile(navMesh);
                AddLongRangeDestinationTile(navMesh, 0);
                AddLongRangeDestinationTile(navMesh, 1);

                var sourceTile = navMesh->GetTileAt(0, 0, 0);
                var destinationLayer0 = navMesh->GetTileAt(2, 0, 0);
                var destinationLayer1 = navMesh->GetTileAt(2, 0, 1);
                Assert.IsTrue(sourceTile != null);
                Assert.IsTrue(destinationLayer0 != null);
                Assert.IsTrue(destinationLayer1 != null);

                var offMeshRef = GetPolyRef(navMesh, sourceTile, 1);
                Assert.AreEqual(2, CountPolyLinks(sourceTile, 1));
                Assert.AreEqual(1, CountLinksToPolyRef(destinationLayer0, offMeshRef) + CountLinksToPolyRef(destinationLayer1, offMeshRef));
            }
            finally
            {
                DtNavMesh.Free(navMesh);
            }
        }

        [Test]
        public void RemovingChosenDestinationLayer_ReconnectsLongRangeOffMeshToRemainingLayer()
        {
            var navMesh = DtNavMesh.Alloc(Allocator.Temp);
            var query = default(DtNavMeshQuery);

            try
            {
                InitializeLongRangeOffMeshNavMeshParams(navMesh);
                AddLongRangeSourceTile(navMesh);
                AddLongRangeDestinationTile(navMesh, 0);
                AddLongRangeDestinationTile(navMesh, 1);

                var sourceTile = navMesh->GetTileAt(0, 0, 0);
                var destinationLayer0 = navMesh->GetTileAt(2, 0, 0);
                var destinationLayer1 = navMesh->GetTileAt(2, 0, 1);
                Assert.IsTrue(sourceTile != null);
                Assert.IsTrue(destinationLayer0 != null);
                Assert.IsTrue(destinationLayer1 != null);

                var offMeshRef = GetPolyRef(navMesh, sourceTile, 1);
                var connectedLayer = CountLinksToPolyRef(destinationLayer0, offMeshRef) == 1 ? 0 : 1;
                var removedTile = connectedLayer == 0 ? destinationLayer0 : destinationLayer1;
                var remainingLayer = connectedLayer == 0 ? 1 : 0;

                var removedTileRef = navMesh->GetTileRef(removedTile);
                Assert.AreEqual(DtStatus.Success, navMesh->RemoveTile(removedTileRef, out _, out _));

                sourceTile = navMesh->GetTileAt(0, 0, 0);
                var remainingTile = navMesh->GetTileAt(2, 0, remainingLayer);
                Assert.IsTrue(sourceTile != null);
                Assert.IsTrue(remainingTile != null);
                Assert.AreEqual(2, CountPolyLinks(sourceTile, 1));
                Assert.AreEqual(1, CountLinksToPolyRef(remainingTile, offMeshRef));

                query = new DtNavMeshQuery(navMesh, 32, Allocator.Temp);
                var filter = DtQueryFilter.CreateDefault();
                var startRef = GetPolyRef(navMesh, sourceTile, 0);
                var endRef = GetPolyRef(navMesh, remainingTile, 0);

                var path = stackalloc DtPolyRef[8];
                var status = query.FindPath(startRef, endRef, LongRangeQueryStartPosition, LongRangeQueryEndPosition, ref filter, path, out var pathCount, 8);

                Assert.IsTrue(Detour.StatusSucceed(status));
                Assert.IsFalse(Detour.StatusDetail(status, DtStatus.PartialResult));
                Assert.AreEqual(3, pathCount);
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

        private static void InitializeLongRangeOffMeshNavMesh(DtNavMesh* navMesh)
        {
            InitializeLongRangeOffMeshNavMeshParams(navMesh);
            AddLongRangeSourceTile(navMesh);
            AddLongRangeDestinationTile(navMesh);
        }

        private static void InitializeLongRangeOffMeshNavMeshParams(DtNavMesh* navMesh)
        {
            Assert.IsTrue(navMesh != null);
            navMesh->Init(new DtNavMeshParams
            {
                orig = float3.zero,
                tileWidth = 1f,
                tileHeight = 1f,
                maxTiles = 4,
                maxPolys = 2,
            });
        }

        private static void AddLongRangeSourceTile(DtNavMesh* navMesh)
        {
            var navData = CreateLongRangeOffMeshTileNavMeshData(0, 0, includeOffMesh: true, out var dataSize);
            Assert.AreEqual(DtStatus.Success, navMesh->AddTile(navData, dataSize, DtTileFlags.TileFreeData, 0, out _));
        }

        private static void AddLongRangeDestinationTile(DtNavMesh* navMesh, int tileLayer = 0)
        {
            var navData = CreateLongRangeOffMeshTileNavMeshData(2, tileLayer, includeOffMesh: false, out var dataSize);
            Assert.AreEqual(DtStatus.Success, navMesh->AddTile(navData, dataSize, DtTileFlags.TileFreeData, 0, out _));
        }

        private static DtNavMeshData CreateLongRangeOffMeshTileNavMeshData(int tileX, int tileLayer, bool includeOffMesh, out int dataSize)
        {
            var verts = stackalloc ushort3[4];
            verts[0] = new ushort3(0, 0, 0);
            verts[1] = new ushort3(1, 0, 0);
            verts[2] = new ushort3(0, 0, 1);
            verts[3] = new ushort3(1, 0, 1);

            var polys = stackalloc ushort[4 * 2];
            for (var i = 0; i < 8; ++i)
            {
                polys[i] = Detour.MeshNullIDX;
            }

            const ushort border = Detour.DTExtLink | 0xf;
            SetPoly(polys, 0, 0, 2, 3, 1, border, border, border, border);

            var polyFlags = stackalloc ushort[1];
            polyFlags[0] = 1;

            var polyAreas = stackalloc byte[1];
            polyAreas[0] = Recast.RCWalkableArea;

            var createParams = new DtNavMeshCreateParams
            {
                Verts = verts,
                VertCount = 4,
                Polys = polys,
                PolyFlags = polyFlags,
                PolyAreas = polyAreas,
                PolyCount = 1,
                Nvp = 4,
                TileX = tileX,
                TileY = 0,
                TileLayer = tileLayer,
                Bmin = new float3(tileX, 0f, 0f),
                Bmax = new float3(tileX + 1f, 1f, 1f),
                WalkableHeight = 2f,
                WalkableRadius = 0f,
                WalkableClimb = 0f,
                Cs = 1f,
                Ch = 1f,
                BuildBvTree = true,
            };

            if (includeOffMesh)
            {
                var offMeshVerts = stackalloc float3x2[1];
                offMeshVerts[0] = new float3x2(LongRangeStartPosition, LongRangeEndPosition);

                var offMeshRadius = stackalloc float[1];
                offMeshRadius[0] = 0.25f;

                var offMeshFlags = stackalloc ushort[1];
                offMeshFlags[0] = 17;

                var offMeshAreas = stackalloc byte[1];
                offMeshAreas[0] = 5;

                var offMeshDirection = stackalloc byte[1];
                offMeshDirection[0] = 1;

                var offMeshUserIds = stackalloc uint[1];
                offMeshUserIds[0] = 77;

                createParams.OffMeshConVerts = offMeshVerts;
                createParams.OffMeshConRad = offMeshRadius;
                createParams.OffMeshConFlags = offMeshFlags;
                createParams.OffMeshConAreas = offMeshAreas;
                createParams.OffMeshConDir = offMeshDirection;
                createParams.OffMeshConUserID = offMeshUserIds;
                createParams.OffMeshConCount = 1;
            }

            Assert.IsTrue(Detour.CreateNavMeshData(&createParams, out var navData, out dataSize, Allocator.Temp));
            return navData;
        }

        private static int CountPolyLinks(DtMeshTile* tile, int polyIndex)
        {
            var linkCount = 0;
            for (var linkIndex = tile->polys[polyIndex].firstLink; linkIndex != Detour.DTNullLink; linkIndex = tile->links[linkIndex].next)
            {
                linkCount++;
            }

            return linkCount;
        }

        private static int CountLinksToPolyRef(DtMeshTile* tile, DtPolyRef polyRef)
        {
            if (tile == null)
            {
                return 0;
            }

            var linkCount = 0;
            for (var polyIndex = 0; polyIndex < tile->header->polyCount; ++polyIndex)
            {
                for (var linkIndex = tile->polys[polyIndex].firstLink; linkIndex != Detour.DTNullLink; linkIndex = tile->links[linkIndex].next)
                {
                    if (tile->links[linkIndex].polyRef == polyRef)
                    {
                        linkCount++;
                    }
                }
            }

            return linkCount;
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
