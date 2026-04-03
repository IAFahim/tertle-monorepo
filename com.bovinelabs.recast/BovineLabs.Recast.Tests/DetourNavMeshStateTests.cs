// <copyright file="DetourNavMeshStateTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast.Tests
{
    using System;
    using System.Runtime.InteropServices;
    using NUnit.Framework;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe class DetourNavMeshStateTests
    {
        [Test]
        public void GetTileStateSizeMatchesUpstreamAlignedLayout()
        {
            var navMesh = default(DtNavMesh);
            var tiles = stackalloc DtMeshTile[1];
            var header = stackalloc DtMeshHeader[1];
            var polys = stackalloc DtPoly[2];

            navMesh.tiles = tiles;
            InitializeTile(&tiles[0], header, polys, 1, 2);

            var expectedSize = Align4(UnsafeUtility.SizeOf<TestTileState>()) + Align4(UnsafeUtility.SizeOf<TestPolyState>() * header->polyCount);

            Assert.AreEqual(expectedSize, navMesh.GetTileStateSize(&tiles[0]));
        }

        [Test]
        public void StoreAndRestoreTileStateRoundTripsPolyState()
        {
            var navMesh = default(DtNavMesh);
            var tiles = stackalloc DtMeshTile[1];
            var header = stackalloc DtMeshHeader[1];
            var polys = stackalloc DtPoly[2];

            navMesh.tiles = tiles;
            InitializeTile(&tiles[0], header, polys, 7, 2);

            polys[0].flags = 42;
            polys[0].SetArea(5);
            polys[1].flags = 1337;
            polys[1].SetArea(17);

            var stateSize = navMesh.GetTileStateSize(&tiles[0]);
            var state = stackalloc byte[stateSize];

            Assert.AreEqual(DtStatus.Success, navMesh.StoreTileState(&tiles[0], state, stateSize));

            polys[0].flags = 1;
            polys[0].SetArea(2);
            polys[1].flags = 3;
            polys[1].SetArea(4);

            Assert.AreEqual(DtStatus.Success, navMesh.RestoreTileState(&tiles[0], state, stateSize));
            Assert.AreEqual(42, polys[0].flags);
            Assert.AreEqual(5, polys[0].GetArea());
            Assert.AreEqual(1337, polys[1].flags);
            Assert.AreEqual(17, polys[1].GetArea());
        }

        [Test]
        public void RestoreTileStateRejectsStoredStateFromDifferentTile()
        {
            var navMesh = default(DtNavMesh);
            var tiles = stackalloc DtMeshTile[2];
            var headers = stackalloc DtMeshHeader[2];
            var polys = stackalloc DtPoly[2];

            navMesh.tiles = tiles;
            InitializeTile(&tiles[0], &headers[0], &polys[0], 3, 1);
            InitializeTile(&tiles[1], &headers[1], &polys[1], 4, 1);

            var stateSize = navMesh.GetTileStateSize(&tiles[0]);
            var state = stackalloc byte[stateSize];

            Assert.AreEqual(DtStatus.Success, navMesh.StoreTileState(&tiles[0], state, stateSize));
            Assert.AreEqual(DtStatus.Failure | DtStatus.InvalidParam, navMesh.RestoreTileState(&tiles[1], state, stateSize));
        }

        [Test]
        public void RcSpanLayoutMatchesPackedBitsThenPointerOrder()
        {
            var expectedNextOffset = IntPtr.Size == 8 ? 8 : 4;

            Assert.AreEqual(expectedNextOffset, Marshal.OffsetOf<RcSpan>(nameof(RcSpan.Next)).ToInt32());
            Assert.AreEqual(expectedNextOffset + IntPtr.Size, UnsafeUtility.SizeOf<RcSpan>());
        }

        private static void InitializeTile(DtMeshTile* tile, DtMeshHeader* header, DtPoly* polys, uint salt, int polyCount)
        {
            *tile = default;
            *header = default;
            header->polyCount = polyCount;
            tile->salt = salt;
            tile->header = header;
            tile->polys = polys;

            for (var i = 0; i < polyCount; ++i)
            {
                polys[i] = default;
            }
        }

        private static int Align4(int value)
        {
            return (value + 3) & ~3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TestTileState
        {
            public int Magic;
            public int Version;
            public DtTileRef TileRef;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TestPolyState
        {
            public ushort Flags;
            public byte Area;
        }
    }
}
