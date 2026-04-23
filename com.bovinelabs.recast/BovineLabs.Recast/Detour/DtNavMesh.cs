// <copyright file="DtNavMesh.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>A navigation mesh based on tiles of convex polygons.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DtNavMesh : IDisposable
    {
        private const int MaxNeighbourTiles = 32;
        private const int NullRemoteOffMeshEntry = -1;

        public DtNavMeshParams parameters; // Current initialization params
        public float3 origin; // Origin of the tile (0,0)
        public float tileWidth;
        public float tileHeight; // Dimensions of each tile
        public int maxTiles; // Max number of tiles
        public int tileLookupSize; // Tile hash lookup size (must be pot)
        public int tileLookupMask; // Tile hash lookup mask

        public DtMeshTile** positionLookup; // Tile hash lookup
        public DtMeshTile* nextFreeTile; // Freelist of tiles
        public DtMeshTile* tiles; // List of tiles

#if DT_POLYREF32
        public uint saltBits; // Number of salt bits in the tile ID (32-bit mode only)
        public uint tileBits; // Number of tile bits in the tile ID (32-bit mode only)
        public uint polyBits; // Number of poly bits in the tile ID (32-bit mode only)
#endif

        private AllocatorManager.AllocatorHandle allocator;
        private int remoteOffMeshLookupSize;
        private int remoteOffMeshLookupMask;
        private int* remoteOffMeshLookupBuckets;
        private RemoteOffMeshLookupEntry* remoteOffMeshLookupEntries;
        private int remoteOffMeshLookupEntryCount;
        private int remoteOffMeshLookupEntryCapacity;
        private int remoteOffMeshLookupFreeList;

        [StructLayout(LayoutKind.Sequential)]
        private struct RemoteOffMeshLookupEntry
        {
            public int2 Destination;
            public DtTileRef SourceTileRef;
            public int OffMeshConnectionIndex;
            public int Next;
        }

        /// <summary>Allocates a navigation mesh object.</summary>
        /// <returns>A navigation mesh that is ready for initialization, or null on failure.</returns>
        /// <remarks>Equivalent to dtAllocNavMesh() in C++.</remarks>
        public static DtNavMesh* Alloc(AllocatorManager.AllocatorHandle allocator)
        {
            var navMesh = (DtNavMesh*)AllocatorManager.Allocate(allocator, sizeof(DtNavMesh), UnsafeUtility.AlignOf<DtNavMesh>());
            *navMesh = default;
            navMesh->allocator = allocator;
            return navMesh;
        }

        /// <summary>Frees the specified navigation mesh object.</summary>
        /// <param name="navMesh">A navigation mesh allocated using Alloc.</param>
        /// <remarks>Equivalent to dtFreeNavMesh() in C++.</remarks>
        public static void Free(DtNavMesh* navMesh)
        {
            if (navMesh == null)
            {
                return;
            }

            navMesh->Dispose();
            AllocatorManager.Free(navMesh->allocator, navMesh);
        }

        /// <summary>Gets the navigation mesh initialization params.</summary>
        /// <returns>The initialization parameters.</returns>
        /// <remarks>Equivalent to dtNavMesh::getParams() in C++.</remarks>
        public DtNavMeshParams* GetParams()
        {
            fixed (DtNavMeshParams* ptr = &this.parameters)
            {
                return ptr;
            }
        }

        /// <summary>
        /// Initializes the navigation mesh for tiled use.
        /// </summary>
        /// <param name="param">Initialization parameters.</param>
        /// <exception cref="ArgumentException">Thrown when the 32-bit poly ref configuration cannot encode the requested tile and polygon counts.</exception>
        /// <remarks>Equivalent to dtNavMesh::init() in C++.</remarks>
        public void Init(in DtNavMeshParams param)
        {
            // For 64-bit mode, use fixed bit allocation
            // Note: saltBits, tileBits, polyBits are not stored in 64-bit mode since they're constants
#if DT_POLYREF32
            this.tileBits = Detour.IntegerLog2(Detour.NextPowerOfTwo((uint)param.maxTiles));
            this.polyBits = Detour.IntegerLog2(Detour.NextPowerOfTwo((uint)param.maxPolys));
            var usedBits = this.tileBits + this.polyBits;
            if (usedBits > 31u)
            {
                throw new ArgumentException($"Invalid 32-bit navmesh ref configuration: maxTiles={param.maxTiles}, maxPolys={param.maxPolys}.");
            }

            this.saltBits = math.min(31u, 32u - usedBits);
            if (this.saltBits < 10u)
            {
                throw new ArgumentException($"Invalid 32-bit navmesh ref configuration: maxTiles={param.maxTiles}, maxPolys={param.maxPolys}.");
            }
#endif

            this.parameters = param;
            this.origin = param.orig;
            this.tileWidth = param.tileWidth;
            this.tileHeight = param.tileHeight;

            // Initialize tile system
            this.maxTiles = param.maxTiles;
            this.tileLookupSize = math.ceilpow2(param.maxTiles / 4);
            if (this.tileLookupSize == 0)
            {
                this.tileLookupSize = 1;
            }

            this.tileLookupMask = this.tileLookupSize - 1;

            // Allocate tile arrays
            this.tiles = (DtMeshTile*)AllocatorManager.Allocate(this.allocator, sizeof(DtMeshTile) * this.maxTiles, UnsafeUtility.AlignOf<DtMeshTile>());
            this.positionLookup = (DtMeshTile**)AllocatorManager.Allocate(this.allocator, sizeof(DtMeshTile*) * this.tileLookupSize,
                UnsafeUtility.AlignOf<IntPtr>());
            this.remoteOffMeshLookupSize = this.tileLookupSize;
            this.remoteOffMeshLookupMask = this.tileLookupMask;
            this.remoteOffMeshLookupBuckets =
                (int*)AllocatorManager.Allocate(this.allocator, sizeof(int) * this.remoteOffMeshLookupSize, UnsafeUtility.AlignOf<int>());
            this.remoteOffMeshLookupEntries = null;
            this.remoteOffMeshLookupEntryCount = 0;
            this.remoteOffMeshLookupEntryCapacity = 0;
            this.remoteOffMeshLookupFreeList = NullRemoteOffMeshEntry;

            // Initialize arrays
            UnsafeUtility.MemClear(this.tiles, sizeof(DtMeshTile) * this.maxTiles);
            UnsafeUtility.MemClear(this.positionLookup, sizeof(DtMeshTile*) * this.tileLookupSize);
            UnsafeUtility.MemSet(this.remoteOffMeshLookupBuckets, 0xff, sizeof(int) * this.remoteOffMeshLookupSize);

            // Initialize free list
            this.nextFreeTile = null;
            for (var i = this.maxTiles - 1; i >= 0; --i)
            {
                this.tiles[i].salt = 1;
                this.tiles[i].next = this.nextFreeTile;
                this.nextFreeTile = &this.tiles[i];
            }
        }

        /// <summary>
        /// Initializes the navigation mesh for single tile use.
        /// </summary>
        /// <param name="data">Data of the new tile.</param>
        /// <param name="dataSize">The data size of the new tile.</param>
        /// <param name="flags">The tile flags.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::init() single tile version in C++.</remarks>
        public DtStatus InitSingleTile(byte* data, int dataSize, DtTileFlags flags)
        {
            if (data == null)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Make sure the data is in right format
            var header = (DtMeshHeader*)data;
            if (header->magic != Detour.DTNavmeshMagic)
            {
                return DtStatus.Failure | DtStatus.WrongMagic;
            }

            if (header->version != Detour.DTNavmeshVersion)
            {
                return DtStatus.Failure | DtStatus.WrongVersion;
            }

            var param = new DtNavMeshParams
            {
                tileWidth = header->bmax.x - header->bmin.x,
                tileHeight = header->bmax.z - header->bmin.z,
                maxTiles = 1,
                maxPolys = header->polyCount,
            };

            param.orig = header->bmin;

            this.Init(param);
            return this.AddTile(data, dataSize, flags, 0, out _);
        }

        /// <summary>
        /// Adds a tile to the navigation mesh.
        /// </summary>
        /// <param name="data">Data for the new tile mesh.</param>
        /// <param name="dataSize">Data size of the new tile mesh.</param>
        /// <param name="flags">Tile flags.</param>
        /// <param name="lastRef">The desired reference for the tile (when reloading a tile).</param>
        /// <param name="result">The tile reference (if the tile was successfully added).</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::addTile() in C++.</remarks>
        public DtStatus AddTile(byte* data, int dataSize, DtTileFlags flags, DtTileRef lastRef, out DtTileRef result)
        {
            result = default;

            if (data == null)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Make sure the data is in right format
            var header = (DtMeshHeader*)data;
            if (header->magic != Detour.DTNavmeshMagic)
            {
                return DtStatus.Failure | DtStatus.WrongMagic;
            }

            if (header->version != Detour.DTNavmeshVersion)
            {
                return DtStatus.Failure | DtStatus.WrongVersion;
            }

            // Do not allow adding more polygons than specified in the NavMesh's maxPolys constraint
#if DT_POLYREF32
            if (this.polyBits < Detour.IntegerLog2(Detour.NextPowerOfTwo((uint)header->polyCount)))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }
#endif

            // Make sure the location is free
            if (this.GetTileAt(header->x, header->y, header->layer) != null)
            {
                return DtStatus.Failure | DtStatus.AlreadyOccupied;
            }

            // Allocate a tile
            DtMeshTile* tile = null;
            if (lastRef == 0)
            {
                if (this.nextFreeTile != null)
                {
                    tile = this.nextFreeTile;
                    this.nextFreeTile = tile->next;
                    tile->next = null;
                }
            }
            else
            {
                // Try to relocate the tile to specific index with same salt
                var tileIndex = (int)this.DecodePolyIdTile(lastRef);
                if (tileIndex >= this.maxTiles)
                {
                    return DtStatus.Failure | DtStatus.OutOfMemory;
                }

                // Try to find the specific tile id from the free list
                var target = &this.tiles[tileIndex];
                DtMeshTile* prev = null;
                tile = this.nextFreeTile;
                while (tile != null && tile != target)
                {
                    prev = tile;
                    tile = tile->next;
                }

                // Could not find the correct location
                if (tile != target)
                {
                    return DtStatus.Failure | DtStatus.OutOfMemory;
                }

                // Remove from freelist
                if (prev == null)
                {
                    this.nextFreeTile = tile->next;
                }
                else
                {
                    prev->next = tile->next;
                }

                // Restore salt
                tile->salt = this.DecodePolyIdSalt(lastRef);
            }

            // Make sure we could allocate a tile
            if (tile == null)
            {
                return DtStatus.Failure | DtStatus.OutOfMemory;
            }

            // Insert tile into the position lookup table
            var hash = ComputeTileHash(header->x, header->y, this.tileLookupMask);
            tile->next = this.positionLookup[hash];
            this.positionLookup[hash] = tile;

            // Patch header pointers
            var navMeshData = new DtNavMeshData(data);
            tile->verts = navMeshData.Vertices;
            tile->polys = navMeshData.Polygons;
            tile->links = navMeshData.Links;
            tile->detailMeshes = navMeshData.DetailMeshes;
            tile->detailVerts = navMeshData.DetailVertices;
            tile->detailTris = navMeshData.DetailTriangles;
            tile->bvTree = navMeshData.BVTree;
            tile->offMeshCons = navMeshData.OffMeshConnections;

            // If there are no items in the bvtree, reset the tree pointer
            if (navMeshData.Header->bvNodeCount == 0)
            {
                tile->bvTree = null;
            }

            // Build links freelist
            tile->linksFreeList = 0;
            tile->links[header->maxLinkCount - 1].next = Detour.DTNullLink;
            for (var i = 0; i < header->maxLinkCount - 1; ++i)
            {
                tile->links[i].next = (uint)(i + 1);
            }

            // Initialize tile
            tile->header = header;
            tile->data = data;
            tile->dataSize = dataSize;
            tile->flags = flags;

            this.ConnectInternalLinks(tile);

            // Base off-mesh connections to their starting polygons, then stitch landings by exact destination tile.
            this.BaseOffMeshLinks(tile);
            this.RegisterOffMeshLinks(tile);
            this.ConnectOffMeshLinksFromTile(tile);
            this.ConnectOffMeshLinksToTile(tile);

            // Create connections with neighbor tiles
            var neighbors = stackalloc DtMeshTile*[MaxNeighbourTiles];

            // Connect with layers in current tile
            var neighborCount = this.GetTilesAt(header->x, header->y, neighbors, MaxNeighbourTiles);
            for (var j = 0; j < neighborCount; ++j)
            {
                if (neighbors[j] == tile)
                {
                    continue;
                }

                this.ConnectExternalLinks(tile, neighbors[j], -1);
                this.ConnectExternalLinks(neighbors[j], tile, -1);
            }

            // Connect with neighbor tiles
            for (byte i = 0; i < 8; ++i)
            {
                neighborCount = this.GetNeighborTilesAt(header->x, header->y, i, neighbors, MaxNeighbourTiles);
                for (var j = 0; j < neighborCount; ++j)
                {
                    this.ConnectExternalLinks(tile, neighbors[j], i);
                    this.ConnectExternalLinks(neighbors[j], tile, Detour.OppositeTile(i));
                }
            }

            result = this.GetTileRef(tile);

            return DtStatus.Success;
        }

        /// <summary>
        /// Removes the specified tile from the navigation mesh.
        /// </summary>
        /// <param name="tileRef">The reference of the tile to remove.</param>
        /// <param name="data">Data associated with deleted tile.</param>
        /// <param name="dataSize">Size of the data associated with deleted tile.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::removeTile() in C++.</remarks>
        public DtStatus RemoveTile(DtTileRef tileRef, out byte* data, out int dataSize)
        {
            data = null;
            dataSize = 0;

            if (tileRef == 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var tileIndex = this.DecodePolyIdTile(tileRef);
            var tileSalt = this.DecodePolyIdSalt(tileRef);
            if ((int)tileIndex >= this.maxTiles)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var tile = &this.tiles[tileIndex];
            if (tile->salt != tileSalt)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            this.UnregisterOffMeshLinks(tile);

            // Remove tile from hash lookup
            var hash = ComputeTileHash(tile->header->x, tile->header->y, this.tileLookupMask);
            DtMeshTile* prev = null;
            var current = this.positionLookup[hash];
            while (current != null)
            {
                if (current == tile)
                {
                    if (prev != null)
                    {
                        prev->next = current->next;
                    }
                    else
                    {
                        this.positionLookup[hash] = current->next;
                    }

                    break;
                }

                prev = current;
                current = current->next;
            }

            // Remove connections to neighbor tiles
            var neighbors = stackalloc DtMeshTile*[MaxNeighbourTiles];

            // Disconnect from other layers in current tile
            var neighborCount = this.GetTilesAt(tile->header->x, tile->header->y, neighbors, MaxNeighbourTiles);
            for (var j = 0; j < neighborCount; ++j)
            {
                if (neighbors[j] == tile)
                {
                    continue;
                }

                this.UnconnectLinks(neighbors[j], tile);
            }

            // Disconnect from neighbor tiles
            for (var i = 0; i < 8; ++i)
            {
                neighborCount = this.GetNeighborTilesAt(tile->header->x, tile->header->y, i, neighbors, MaxNeighbourTiles);
                for (var j = 0; j < neighborCount; ++j)
                {
                    this.UnconnectLinks(neighbors[j], tile);
                }
            }

            this.UnconnectRemoteIncomingOffMeshLinks(tile);
            this.UnconnectRemoteOutgoingOffMeshLinks(tile);

            // Reset tile
            if ((tile->flags & DtTileFlags.TileFreeData) != 0)
            {
                // Owns data
                AllocatorManager.Free(allocator, tile->data);
                tile->data = null;
                tile->dataSize = 0;
                data = null;
                dataSize = 0;
            }
            else
            {
                data = tile->data;
                dataSize = tile->dataSize;
            }

            tile->header = null;
            tile->flags = 0;
            tile->linksFreeList = 0;
            tile->polys = null;
            tile->verts = null;
            tile->links = null;
            tile->detailMeshes = null;
            tile->detailVerts = null;
            tile->detailTris = null;
            tile->bvTree = null;
            tile->offMeshCons = null;

            // Update salt, salt should never be zero
#if DT_POLYREF32
            tile->salt = (tile->salt + 1) & ((1u << (int)this.saltBits) - 1);
#else
            tile->salt = (tile->salt + 1) & ((1u << Detour.DTSaltBits) - 1);

#endif
            if (tile->salt == 0)
            {
                tile->salt++;
            }

            // Add to free list
            tile->next = this.nextFreeTile;
            this.nextFreeTile = tile;

            return DtStatus.Success;
        }

        /// <summary>
        /// Calculates the tile grid location for the specified world position.
        /// </summary>
        /// <param name="position">The world position for the query.</param>
        /// <param name="tileX">The tile's x-location.</param>
        /// <param name="tileY">The tile's y-location.</param>
        /// <remarks>Equivalent to dtNavMesh::calcTileLoc() in C++.</remarks>
        public void CalculateTileLocation(in float3 position, out int tileX, out int tileY)
        {
            tileX = (int)math.floor((position.x - this.origin.x) / this.tileWidth);
            tileY = (int)math.floor((position.z - this.origin.z) / this.tileHeight);
        }

        /// <summary>
        /// Gets the tile at the specified grid location.
        /// </summary>
        /// <param name="x">The tile's x-location.</param>
        /// <param name="y">The tile's y-location.</param>
        /// <param name="layer">The tile's layer.</param>
        /// <returns>The tile, or null if the tile does not exist.</returns>
        /// <remarks>Equivalent to dtNavMesh::getTileAt() in C++.</remarks>
        public DtMeshTile* GetTileAt(int x, int y, int layer)
        {
            // Find tile based on hash
            var hash = ComputeTileHash(x, y, this.tileLookupMask);
            var tile = this.positionLookup[hash];
            while (tile != null)
            {
                if (tile->header != null && tile->header->x == x && tile->header->y == y && tile->header->layer == layer)
                {
                    return tile;
                }

                tile = tile->next;
            }

            return null;
        }

        /// <summary>
        /// Gets all tiles at the specified grid location (all layers).
        /// </summary>
        /// <param name="x">The tile's x-location.</param>
        /// <param name="y">The tile's y-location.</param>
        /// <param name="tilesOut">A pointer to an array of tiles that will hold the result.</param>
        /// <param name="maxTilesOut">The maximum tiles the tiles parameter can hold.</param>
        /// <returns>The number of tiles returned in the tiles array.</returns>
        /// <remarks>Equivalent to dtNavMesh::getTilesAt() in C++.</remarks>
        public int GetTilesAt(int x, int y, DtMeshTile** tilesOut, int maxTilesOut)
        {
            if (tilesOut == null)
            {
                return 0;
            }

            var count = 0;

            // Find tile based on hash
            var hash = ComputeTileHash(x, y, this.tileLookupMask);
            var tile = this.positionLookup[hash];
            while (tile != null)
            {
                if (tile->header != null && tile->header->x == x && tile->header->y == y)
                {
                    if (count < maxTilesOut)
                    {
                        tilesOut[count++] = tile;
                    }
                }

                tile = tile->next;
            }

            return count;
        }

        /// <summary>
        /// Gets the tile reference for the tile at specified grid location.
        /// </summary>
        /// <param name="x">The tile's x-location.</param>
        /// <param name="y">The tile's y-location.</param>
        /// <param name="layer">The tile's layer.</param>
        /// <returns>The tile reference of the tile, or 0 if there is none.</returns>
        /// <remarks>Equivalent to dtNavMesh::getTileRefAt() in C++.</remarks>
        public DtTileRef GetTileRefAt(int x, int y, int layer)
        {
            return this.GetTileRef(this.GetTileAt(x, y, layer));
        }

        /// <summary>
        /// Gets the tile for the specified tile reference.
        /// </summary>
        /// <param name="tileRef">The tile reference of the tile to retrieve.</param>
        /// <returns>The tile for the specified reference, or null if the reference is invalid.</returns>
        /// <remarks>Equivalent to dtNavMesh::getTileByRef() in C++.</remarks>
        public DtMeshTile* GetTileByRef(DtTileRef tileRef)
        {
            if (tileRef == 0)
            {
                return null;
            }

            var tileIndex = this.DecodePolyIdTile(tileRef);
            var tileSalt = this.DecodePolyIdSalt(tileRef);
            if ((int)tileIndex >= this.maxTiles)
            {
                return null;
            }

            var tile = &this.tiles[tileIndex];
            if (tile->salt != tileSalt)
            {
                return null;
            }

            return tile;
        }

        /// <summary>
        /// The maximum number of tiles supported by the navigation mesh.
        /// </summary>
        /// <returns>The maximum number of tiles supported by the navigation mesh.</returns>
        /// <remarks>Equivalent to dtNavMesh::getMaxTiles() in C++.</remarks>
        public int GetMaxTiles()
        {
            return this.maxTiles;
        }

        /// <summary>
        /// Gets the tile at the specified index.
        /// </summary>
        /// <param name="i">The tile index.</param>
        /// <returns>The tile at the specified index.</returns>
        /// <remarks>Equivalent to dtNavMesh::getTile() in C++.</remarks>
        public DtMeshTile* GetTile(int i)
        {
            if (i < 0 || i >= this.maxTiles)
            {
                return null;
            }

            return &this.tiles[i];
        }

        /// <summary>
        /// Gets the tile and polygon for the specified polygon reference.
        /// </summary>
        /// <param name="polyRef">The reference for a polygon.</param>
        /// <param name="tile">The tile containing the polygon.</param>
        /// <param name="poly">The polygon.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::getTileAndPolyByRef() in C++.</remarks>
        public DtStatus GetTileAndPolyByRef(DtPolyRef polyRef, out DtMeshTile* tile, out DtPoly* poly)
        {
            tile = null;
            poly = null;

            if (polyRef == 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            this.DecodePolyId(polyRef, out var salt, out var tileIndex, out var polyIndex);
            if ((int)tileIndex >= this.maxTiles)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var t = &this.tiles[tileIndex];
            if (t->salt != salt || t->header == null)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if ((int)polyIndex >= t->header->polyCount)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            tile = t;
            poly = &t->polys[polyIndex];
            return DtStatus.Success;
        }

        /// <summary>
        /// Returns the tile and polygon for the specified polygon reference.
        /// </summary>
        /// <param name="polyRef">A known valid reference for a polygon.</param>
        /// <param name="tile">The tile containing the polygon.</param>
        /// <param name="poly">The polygon.</param>
        /// <remarks>Equivalent to dtNavMesh::getTileAndPolyByRefUnsafe() in C++.</remarks>
        public void GetTileAndPolyByRefUnsafe(DtPolyRef polyRef, out DtMeshTile* tile, out DtPoly* poly)
        {
            this.DecodePolyId(polyRef, out _, out var tileIndex, out var polyIndex);
            tile = &this.tiles[tileIndex];
            poly = &this.tiles[tileIndex].polys[polyIndex];
        }

        /// <summary>
        /// Gets neighbor tiles at the specified location and side.
        /// </summary>
        /// <param name="x">The tile's x-location.</param>
        /// <param name="y">The tile's y-location.</param>
        /// <param name="side">The side to check (0-7).</param>
        /// <param name="tiles">A pointer to an array of tiles that will hold the result.</param>
        /// <param name="maxTiles">The maximum tiles the tiles parameter can hold.</param>
        /// <returns>The number of tiles returned in the tiles array.</returns>
        /// <remarks>Equivalent to dtNavMesh::getNeighbourTilesAt() in C++.</remarks>
        public int GetNeighborTilesAt(int x, int y, int side, DtMeshTile** tiles, int maxTiles)
        {
            var neighborX = x;
            var neighborY = y;

            switch (side)
            {
                case 0:
                    neighborX++;
                    break;
                case 1:
                    neighborX++;
                    neighborY++;
                    break;
                case 2:
                    neighborY++;
                    break;
                case 3:
                    neighborX--;
                    neighborY++;
                    break;
                case 4:
                    neighborX--;
                    break;
                case 5:
                    neighborX--;
                    neighborY--;
                    break;
                case 6:
                    neighborY--;
                    break;
                case 7:
                    neighborX++;
                    neighborY--;
                    break;
            }

            return this.GetTilesAt(neighborX, neighborY, tiles, maxTiles);
        }

        /// <summary>
        /// Gets the tile reference for the specified tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <returns>The tile reference of the tile.</returns>
        /// <remarks>Equivalent to dtNavMesh::getTileRef() in C++.</remarks>
        public DtTileRef GetTileRef(DtMeshTile* tile)
        {
            if (tile == null)
            {
                return 0;
            }

            var tileIndex = (uint)(tile - this.tiles);
            return this.EncodePolyId(tile->salt, tileIndex, 0);
        }

        /// <summary>
        /// Gets the polygon reference base for the tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <returns>The polygon reference base for the tile.</returns>
        /// <remarks>Equivalent to dtNavMesh::getPolyRefBase() in C++.</remarks>
        public DtPolyRef GetPolyRefBase(DtMeshTile* tile)
        {
            if (tile == null)
            {
                return 0;
            }

            var tileIndex = (uint)(tile - this.tiles);
            return this.EncodePolyId(tile->salt, tileIndex, 0);
        }

        /// <summary>
        /// Gets the endpoints for an off-mesh connection, ordered by "direction of travel".
        /// </summary>
        /// <param name="prevRef">The reference of the polygon before the connection.</param>
        /// <param name="polyRef">The reference of the off-mesh connection polygon.</param>
        /// <param name="startPos">The start position of the off-mesh connection.</param>
        /// <param name="endPos">The end position of the off-mesh connection.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::getOffMeshConnectionPolyEndPoints() in C++.</remarks>
        public DtStatus GetOffMeshConnectionPolyEndPoints(DtPolyRef prevRef, DtPolyRef polyRef, float3* startPos, float3* endPos)
        {
            if (polyRef == 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Get current polygon
            var status = this.GetTileAndPolyByRef(polyRef, out var tile, out var poly);
            if (Detour.StatusFailed(status))
            {
                return status;
            }

            // Make sure that the current poly is indeed off-mesh link
            if (poly->GetPolyType() != DtPolyTypes.PolytypeOffMeshConnection)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Figure out which way to hand out the vertices
            var idx0 = 0;
            var idx1 = 1;

            // Find link that points to first vertex
            for (var linkIndex = poly->firstLink; linkIndex != Detour.DTNullLink; linkIndex = tile->links[linkIndex].next)
            {
                if (tile->links[linkIndex].edge == 0)
                {
                    if (tile->links[linkIndex].polyRef != prevRef)
                    {
                        idx0 = 1;
                        idx1 = 0;
                    }

                    break;
                }
            }

            if (startPos != null)
            {
                *startPos = tile->verts[poly->verts[idx0]];
            }

            if (endPos != null)
            {
                *endPos = tile->verts[poly->verts[idx1]];
            }

            return DtStatus.Success;
        }

        /// <summary>
        /// Gets the specified off-mesh connection.
        /// </summary>
        /// <param name="polyRef">The polygon reference of the off-mesh connection.</param>
        /// <returns>The specified off-mesh connection, or null if the polygon reference is not valid.</returns>
        /// <remarks>Equivalent to dtNavMesh::getOffMeshConnectionByRef() in C++.</remarks>
        public DtOffMeshConnection* GetOffMeshConnectionByRef(DtPolyRef polyRef)
        {
            if (polyRef == 0)
            {
                return null;
            }

            var status = this.GetTileAndPolyByRef(polyRef, out var tile, out var poly);
            if (Detour.StatusFailed(status))
            {
                return null;
            }

            // Make sure that the current poly is indeed off-mesh link
            if (poly->GetPolyType() != DtPolyTypes.PolytypeOffMeshConnection)
            {
                return null;
            }

            var idx = (int)this.DecodePolyIdPoly(polyRef) - tile->header->offMeshBase;
            if (idx < 0 || idx >= tile->header->offMeshConCount)
            {
                return null;
            }

            return &tile->offMeshCons[idx];
        }

        /// <summary>
        /// Sets the user defined flags for the specified polygon.
        /// </summary>
        /// <param name="polyRef">The polygon reference.</param>
        /// <param name="flags">The new flags for the polygon.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::setPolyFlags() in C++.</remarks>
        public DtStatus SetPolyFlags(DtPolyRef polyRef, ushort flags)
        {
            if (polyRef == 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var status = this.GetTileAndPolyByRef(polyRef, out _, out var poly);
            if (Detour.StatusFailed(status))
            {
                return status;
            }

            poly->flags = flags;
            return DtStatus.Success;
        }

        /// <summary>
        /// Gets the user defined flags for the specified polygon.
        /// </summary>
        /// <param name="polyRef">The polygon reference.</param>
        /// <param name="resultFlags">The polygon flags.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::getPolyFlags() in C++.</remarks>
        public DtStatus GetPolyFlags(DtPolyRef polyRef, ushort* resultFlags)
        {
            if (polyRef == 0 || resultFlags == null)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var status = this.GetTileAndPolyByRef(polyRef, out _, out var poly);
            if (Detour.StatusFailed(status))
            {
                return status;
            }

            *resultFlags = poly->flags;
            return DtStatus.Success;
        }

        /// <summary>
        /// Sets the user defined area for the specified polygon.
        /// </summary>
        /// <param name="polyRef">The polygon reference.</param>
        /// <param name="area">The new area id for the polygon.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::setPolyArea() in C++.</remarks>
        public DtStatus SetPolyArea(DtPolyRef polyRef, byte area)
        {
            if (polyRef == 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var status = this.GetTileAndPolyByRef(polyRef, out _, out var poly);
            if (Detour.StatusFailed(status))
            {
                return status;
            }

            poly->SetArea(area);
            return DtStatus.Success;
        }

        /// <summary>
        /// Gets the user defined area for the specified polygon.
        /// </summary>
        /// <param name="polyRef">The polygon reference.</param>
        /// <param name="resultArea">The area id for the polygon.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::getPolyArea() in C++.</remarks>
        public DtStatus GetPolyArea(DtPolyRef polyRef, byte* resultArea)
        {
            if (polyRef == 0 || resultArea == null)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var status = this.GetTileAndPolyByRef(polyRef, out _, out var poly);
            if (Detour.StatusFailed(status))
            {
                return status;
            }

            *resultArea = poly->GetArea();
            return DtStatus.Success;
        }

        /// <summary>
        /// Gets the size of the buffer required by storeTileState to store the specified tile's state.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <returns>The size of the buffer required to store the state.</returns>
        /// <remarks>Equivalent to dtNavMesh::getTileStateSize() in C++.</remarks>
        public int GetTileStateSize(DtMeshTile* tile)
        {
            if (tile == null)
            {
                return 0;
            }

            var headerSize = Align4(sizeof(DtTileState));
            var polyStateSize = Align4(sizeof(DtPolyState) * tile->header->polyCount);
            return headerSize + polyStateSize;
        }

        /// <summary>
        /// Stores the non-structural state of the tile in the specified buffer (Flags, area ids, etc.)
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="data">The buffer to store the tile's state in.</param>
        /// <param name="maxDataSize">The size of the data buffer.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::storeTileState() in C++.</remarks>
        public DtStatus StoreTileState(DtMeshTile* tile, byte* data, int maxDataSize)
        {
            // Make sure there is enough space to store the state
            var sizeReq = this.GetTileStateSize(tile);
            if (maxDataSize < sizeReq)
            {
                return DtStatus.Failure | DtStatus.BufferTooSmall;
            }

            var dataPointer = data;
            var tileState = (DtTileState*)dataPointer;
            dataPointer += Align4(sizeof(DtTileState));
            var polyStates = (DtPolyState*)dataPointer;

            tileState->magic = Detour.DTNavmeshStateMagic;
            tileState->version = Detour.DTNavmeshStateVersion;
            tileState->tileRef = this.GetTileRef(tile);

            // Store per-poly state.
            for (var i = 0; i < tile->header->polyCount; ++i)
            {
                polyStates[i].flags = tile->polys[i].flags;
                polyStates[i].area = tile->polys[i].GetArea();
            }

            return DtStatus.Success;
        }

        /// <summary>
        /// Restores the state of the tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <param name="data">The new state (Obtained from storeTileState.)</param>
        /// <param name="maxDataSize">The size of the state within the data buffer.</param>
        /// <returns>The status flags for the operation.</returns>
        /// <remarks>Equivalent to dtNavMesh::restoreTileState() in C++.</remarks>
        public DtStatus RestoreTileState(DtMeshTile* tile, byte* data, int maxDataSize)
        {
            // Make sure there is enough space to store the state
            var sizeReq = this.GetTileStateSize(tile);
            if (maxDataSize < sizeReq)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var dataPointer = data;
            var tileState = (DtTileState*)dataPointer;
            dataPointer += Align4(sizeof(DtTileState));
            var polyStates = (DtPolyState*)dataPointer;

            // Check that header magic and version are a match
            if (tileState->magic != Detour.DTNavmeshStateMagic)
            {
                return DtStatus.Failure | DtStatus.WrongMagic;
            }

            if (tileState->version != Detour.DTNavmeshStateVersion)
            {
                return DtStatus.Failure | DtStatus.WrongVersion;
            }

            if (!tileState->tileRef.Equals(this.GetTileRef(tile)))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Restore per-poly state.
            for (var i = 0; i < tile->header->polyCount; ++i)
            {
                tile->polys[i].flags = polyStates[i].flags;
                tile->polys[i].SetArea(polyStates[i].area);
            }

            return DtStatus.Success;
        }

        /// <summary>
        /// Checks the validity of a polygon reference.
        /// </summary>
        /// <param name="polyRef">The polygon reference to check.</param>
        /// <returns>True if polygon reference is valid for the navigation mesh.</returns>
        /// <remarks>Equivalent to dtNavMesh::isValidPolyRef() in C++.</remarks>
        public bool IsValidPolyRef(DtPolyRef polyRef)
        {
            if (polyRef == 0)
            {
                return false;
            }

            this.DecodePolyId(polyRef, out var salt, out var tileIndex, out var polyIndex);
            if (tileIndex >= (uint)this.maxTiles)
            {
                return false;
            }

            if (this.tiles[tileIndex].salt != salt || this.tiles[tileIndex].header == null)
            {
                return false;
            }

            if (polyIndex >= (uint)this.tiles[tileIndex].header->polyCount)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Derives a standard polygon reference.
        /// </summary>
        /// <param name="salt">The tile's salt value.</param>
        /// <param name="tileIndex">The index of the tile.</param>
        /// <param name="polyIndex">The index of the polygon within the tile.</param>
        /// <returns>The encoded polygon reference.</returns>
        /// <remarks>Equivalent to dtNavMesh::encodePolyId() in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DtPolyRef EncodePolyId(uint salt, uint tileIndex, uint polyIndex)
        {
#if DT_POLYREF32
            return ((DtPolyRef)salt << ((int)this.polyBits + (int)this.tileBits)) | ((DtPolyRef)tileIndex << (int)this.polyBits) | (DtPolyRef)polyIndex;
#else
            return ((DtPolyRef)salt << (Detour.DTPolyBits + Detour.DTTileBits)) | ((DtPolyRef)tileIndex << Detour.DTPolyBits) | (DtPolyRef)polyIndex;
#endif
        }

        /// <summary>
        /// Decodes a standard polygon reference.
        /// </summary>
        /// <param name="polyRef">The polygon reference to decode.</param>
        /// <param name="salt">The tile's salt value.</param>
        /// <param name="tileIndex">The index of the tile.</param>
        /// <param name="polyIndex">The index of the polygon within the tile.</param>
        /// <remarks>Equivalent to dtNavMesh::decodePolyId() in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecodePolyId(DtPolyRef polyRef, out uint salt, out uint tileIndex, out uint polyIndex)
        {
#if DT_POLYREF32
            DtPolyRef saltMask = (1u << (int)this.saltBits) - 1;
            DtPolyRef tileMask = (1u << (int)this.tileBits) - 1;
            DtPolyRef polyMask = (1u << (int)this.polyBits) - 1;
            salt = (polyRef >> ((int)this.polyBits + (int)this.tileBits)) & saltMask;
            tileIndex = (polyRef >> (int)this.polyBits) & tileMask;
            polyIndex = polyRef & polyMask;
#else
            DtPolyRef saltMask = (1ul << Detour.DTSaltBits) - 1;
            DtPolyRef tileMask = (1ul << Detour.DTTileBits) - 1;
            DtPolyRef polyMask = (1ul << Detour.DTPolyBits) - 1;
            salt = (uint)((polyRef >> (Detour.DTPolyBits + Detour.DTTileBits)) & saltMask);
            tileIndex = (uint)((polyRef >> Detour.DTPolyBits) & tileMask);
            polyIndex = (uint)(polyRef & polyMask);
#endif
        }

        /// <summary>
        /// Extracts a tile's salt value from the specified polygon reference.
        /// </summary>
        /// <param name="polyRef">The polygon reference.</param>
        /// <returns>The salt value.</returns>
        /// <remarks>Equivalent to dtNavMesh::decodePolyIdSalt() in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint DecodePolyIdSalt(DtPolyRef polyRef)
        {
#if DT_POLYREF32
            DtPolyRef saltMask = ((DtPolyRef)1 << (int)this.saltBits) - 1;
            return (uint)((polyRef >> ((int)this.polyBits + (int)this.tileBits)) & saltMask);
#else
            DtPolyRef saltMask = ((DtPolyRef)1 << Detour.DTSaltBits) - 1;
            return (uint)((polyRef >> (Detour.DTPolyBits + Detour.DTTileBits)) & saltMask);
#endif
        }

        /// <summary>
        /// Extracts the tile's index from the specified polygon reference.
        /// </summary>
        /// <param name="polyRef">The polygon reference.</param>
        /// <returns>The tile index.</returns>
        /// <remarks>Equivalent to dtNavMesh::decodePolyIdTile() in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint DecodePolyIdTile(DtPolyRef polyRef)
        {
#if DT_POLYREF32
            DtPolyRef tileMask = ((DtPolyRef)1 << (int)this.tileBits) - 1;
            return (uint)((polyRef >> (int)this.polyBits) & tileMask);
#else
            DtPolyRef tileMask = ((DtPolyRef)1 << Detour.DTTileBits) - 1;
            return (uint)((polyRef >> Detour.DTPolyBits) & tileMask);
#endif
        }

        /// <summary>
        /// Extracts the polygon's index (within its tile) from the specified polygon reference.
        /// </summary>
        /// <param name="polyRef">The polygon reference.</param>
        /// <returns>The poly index.</returns>
        /// <remarks>Equivalent to dtNavMesh::decodePolyIdPoly() in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint DecodePolyIdPoly(DtPolyRef polyRef)
        {
#if DT_POLYREF32
            DtPolyRef polyMask = ((DtPolyRef)1 << (int)this.polyBits) - 1;
            return (uint)(polyRef & polyMask);
#else
            DtPolyRef polyMask = ((DtPolyRef)1 << Detour.DTPolyBits) - 1;
            return (uint)(polyRef & polyMask);
#endif
        }

        /// <summary>
        /// Returns closest point on polygon.
        /// </summary>
        /// <param name="polyRef">The polygon reference.</param>
        /// <param name="pos">The position to check.</param>
        /// <param name="closest">The closest point on the polygon.</param>
        /// <param name="posOverPoly">True if the position is over the polygon.</param>
        /// <remarks>Equivalent to dtNavMesh::closestPointOnPoly() in C++.</remarks>
        public void ClosestPointOnPoly(DtPolyRef polyRef, in float3 pos, out float3 closest, out bool posOverPoly)
        {
            this.GetTileAndPolyByRefUnsafe(polyRef, out var tile, out var poly);

            closest = pos;
            if (this.GetPolyHeight(tile, poly, pos, out var height))
            {
                closest.y = height;
                posOverPoly = true;
                return;
            }

            posOverPoly = false;

            // Off-mesh connections don't have detail polygons.
            if (poly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
            {
                var v0 = tile->verts[poly->verts[0]];
                var v1 = tile->verts[poly->verts[1]];
                Detour.DistancePtSegSqr2D(pos, v0, v1, out var t);
                closest = math.lerp(v0, v1, t);
                return;
            }

            // Outside poly that is not an offmesh connection.
            this.ClosestPointOnDetailEdges(tile, poly, pos, out closest, true);
        }

        /// <summary>
        /// Finds the closest point on the detailed mesh edges of a polygon.
        /// </summary>
        private void ClosestPointOnDetailEdges(DtMeshTile* tile, DtPoly* poly, in float3 pos, out float3 closest, bool onlyBoundary)
        {
            var ip = (uint)(poly - tile->polys);
            var pd = &tile->detailMeshes[ip];

            var dmin = float.MaxValue;
            var tmin = 0f;

            float3 pmin = default;
            float3 pmax = default;
            var foundEdge = false;

            for (var i = 0; i < pd->triCount; i++)
            {
                var tri = tile->detailTris + pd->triBase + i;

                const byte anyBoundaryEdge = ((byte)DtDetailTriEdgeFlags.DetailEdgeBoundary << 0) | ((byte)DtDetailTriEdgeFlags.DetailEdgeBoundary << 2) |
                    ((byte)DtDetailTriEdgeFlags.DetailEdgeBoundary << 4);

                if (onlyBoundary && (tri->w & anyBoundaryEdge) == 0)
                {
                    continue;
                }

                float3x3 v = default;

                for (var j = 0; j < 3; ++j)
                {
                    var triIndex = j switch { 0 => tri->x, 1 => tri->y, _ => tri->z };
                    if (triIndex < poly->vertCount)
                    {
                        v[j] = tile->verts[poly->verts[triIndex]];
                    }
                    else
                    {
                        v[j] = tile->detailVerts[pd->vertBase + (triIndex - poly->vertCount)];
                    }
                }

                for (byte k = 0, jLoop = 2; k < 3; jLoop = k++)
                {
                    var edgeFlags = GetDetailTriEdgeFlags(tri->w, jLoop);

                    var kIndex = k switch { 0 => tri->x, 1 => tri->y, _ => tri->z };
                    var jIndex = jLoop switch { 0 => tri->x, 1 => tri->y, _ => tri->z };
                    if ((edgeFlags & (uint)DtDetailTriEdgeFlags.DetailEdgeBoundary) == 0 && (onlyBoundary || jIndex < kIndex))
                    {
                        continue;
                    }

                    var d = Detour.DistancePtSegSqr2D(pos, v[jLoop], v[k], out var t);

                    if (d < dmin)
                    {
                        dmin = d;
                        tmin = t;
                        pmin = v[jLoop];
                        pmax = v[k];
                        foundEdge = true;
                    }
                }
            }

            if (foundEdge)
            {
                closest = math.lerp(pmin, pmax, tmin);
            }
            else
            {
                // This case should not be hit with valid input data, but as a fallback,
                // find the closest vertex on the polygon's main boundary.
                var minVertDistSq = float.MaxValue;
                closest = default; // Must be assigned

                if (poly->vertCount > 0)
                {
                    for (var i = 0; i < poly->vertCount; i++)
                    {
                        ref readonly var vert = ref tile->verts[poly->verts[i]];
                        var d = math.distancesq(pos, vert);
                        if (d < minVertDistSq)
                        {
                            minVertDistSq = d;
                            closest = vert;
                        }
                    }
                }
                else
                {
                    closest = pos; // Should not happen
                }
            }
        }

        /// <summary>
        /// Returns whether position is over the poly and the height at the position if so.
        /// </summary>
        /// <param name="tile">The tile containing the polygon.</param>
        /// <param name="poly">The polygon.</param>
        /// <param name="pos">The position to check.</param>
        /// <param name="height">The height at the surface of the polygon.</param>
        /// <returns>True if the position is over the polygon.</returns>
        /// <remarks>Equivalent to dtNavMesh::getPolyHeight() in C++.</remarks>
        public bool GetPolyHeight(DtMeshTile* tile, DtPoly* poly, in float3 pos, out float height)
        {
            height = 0;

            // Off-mesh connections do not have detail polys and getting height
            // over them does not make sense.
            if (poly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
            {
                return false;
            }

            Span<float3> verts = stackalloc float3[poly->vertCount];
            var nv = poly->vertCount;
            for (var i = 0; i < nv; ++i)
            {
                verts[i] = tile->verts[poly->verts[i]];
            }

            if (!Detour.PointInPolygon(pos, verts))
            {
                return false;
            }

            // Find height using detail mesh if available
            var ip = (uint)(poly - tile->polys);
            var pd = &tile->detailMeshes[ip];

            for (var j = 0; j < pd->triCount; ++j)
            {
                var t = tile->detailTris + pd->triBase + j;
                var v = new float3x3();
                for (var k = 0; k < 3; ++k)
                {
                    var triIndex = k switch { 0 => t->x, 1 => t->y, _ => t->z };
                    if (triIndex < poly->vertCount)
                    {
                        v[k] = tile->verts[poly->verts[triIndex]];
                    }
                    else
                    {
                        v[k] = tile->detailVerts[pd->vertBase + (triIndex - poly->vertCount)];
                    }
                }

                if (Detour.ClosestHeightPointTriangle(pos, v[0], v[1], v[2], out var h))
                {
                    height = h;
                    return true;
                }
            }

            // If all triangle checks failed above (can happen with degenerate triangles
            // or larger floating point values) the point is on an edge, so just select
            // closest. This should almost never happen so the extra iteration here is ok.
            this.ClosestPointOnDetailEdges(tile, poly, pos, out var closest, false);
            height = closest.y;
            return true;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            for (var i = 0; i < this.maxTiles; ++i)
            {
                if ((this.tiles[i].flags & DtTileFlags.TileFreeData) != 0)
                {
                    AllocatorManager.Free(this.allocator, this.tiles[i].data);
                    this.tiles[i].data = null;
                    this.tiles[i].dataSize = 0;
                }
            }

            if (this.positionLookup != null)
            {
                AllocatorManager.Free(this.allocator, this.positionLookup);
            }

            if (this.remoteOffMeshLookupBuckets != null)
            {
                AllocatorManager.Free(this.allocator, this.remoteOffMeshLookupBuckets);
            }

            if (this.remoteOffMeshLookupEntries != null)
            {
                AllocatorManager.Free(this.allocator, this.remoteOffMeshLookupEntries);
            }

            if (this.tiles != null)
            {
                AllocatorManager.Free(this.allocator, this.tiles);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeTileHash(int x, int y, int mask)
        {
            const uint h1 = 0x8da6b343; // Large multiplicative constants
            const uint h2 = 0xd8163841; // here arbitrarily chosen primes
            var n = (h1 * (uint)x) + (h2 * (uint)y);
            return (int)(n & (uint)mask);
        }

        private void EnsureRemoteOffMeshLookupCapacity(int additionalCount)
        {
            if (additionalCount <= 0)
            {
                return;
            }

            var requiredCapacity = this.remoteOffMeshLookupEntryCount + additionalCount;
            if (requiredCapacity <= this.remoteOffMeshLookupEntryCapacity)
            {
                return;
            }

            var newCapacity = math.max(8, this.remoteOffMeshLookupEntryCapacity);
            while (newCapacity < requiredCapacity)
            {
                newCapacity *= 2;
            }

            var newEntries = (RemoteOffMeshLookupEntry*)AllocatorManager.Allocate(
                this.allocator,
                sizeof(RemoteOffMeshLookupEntry) * newCapacity,
                UnsafeUtility.AlignOf<RemoteOffMeshLookupEntry>());

            if (this.remoteOffMeshLookupEntries != null)
            {
                UnsafeUtility.MemCpy(
                    newEntries,
                    this.remoteOffMeshLookupEntries,
                    sizeof(RemoteOffMeshLookupEntry) * this.remoteOffMeshLookupEntryCount);

                AllocatorManager.Free(this.allocator, this.remoteOffMeshLookupEntries);
            }

            this.remoteOffMeshLookupEntries = newEntries;
            this.remoteOffMeshLookupEntryCapacity = newCapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AllocateRemoteOffMeshLookupEntry()
        {
            if (this.remoteOffMeshLookupFreeList != NullRemoteOffMeshEntry)
            {
                var entryIndex = this.remoteOffMeshLookupFreeList;
                this.remoteOffMeshLookupFreeList = this.remoteOffMeshLookupEntries[entryIndex].Next;
                return entryIndex;
            }

            this.EnsureRemoteOffMeshLookupCapacity(1);
            return this.remoteOffMeshLookupEntryCount++;
        }

        private void RegisterOffMeshLinks(DtMeshTile* tile)
        {
            if (tile == null || tile->header == null || tile->header->offMeshConCount == 0)
            {
                return;
            }

            var tileRef = this.GetTileRef(tile);

            for (var i = 0; i < tile->header->offMeshConCount; ++i)
            {
                var destination = this.GetOffMeshConnectionDestinationTile(tile->offMeshCons[i].EndPos);
                var hash = ComputeTileHash(destination.x, destination.y, this.remoteOffMeshLookupMask);
                var entryIndex = this.AllocateRemoteOffMeshLookupEntry();

                this.remoteOffMeshLookupEntries[entryIndex] = new RemoteOffMeshLookupEntry
                {
                    Destination = destination,
                    SourceTileRef = tileRef,
                    OffMeshConnectionIndex = i,
                    Next = this.remoteOffMeshLookupBuckets[hash],
                };

                this.remoteOffMeshLookupBuckets[hash] = entryIndex;
            }
        }

        private void UnregisterOffMeshLinks(DtMeshTile* tile)
        {
            if (tile == null || tile->header == null || tile->header->offMeshConCount == 0)
            {
                return;
            }

            var tileRef = this.GetTileRef(tile);
            for (var i = 0; i < tile->header->offMeshConCount; ++i)
            {
                var destination = this.GetOffMeshConnectionDestinationTile(tile->offMeshCons[i].EndPos);
                this.RemoveRemoteOffMeshLookupEntry(destination, tileRef, i);
            }
        }

        private void RemoveRemoteOffMeshLookupEntry(int2 destination, DtTileRef sourceTileRef, int offMeshConnectionIndex)
        {
            var hash = ComputeTileHash(destination.x, destination.y, this.remoteOffMeshLookupMask);
            var entryIndex = this.remoteOffMeshLookupBuckets[hash];
            var previousEntryIndex = NullRemoteOffMeshEntry;

            while (entryIndex != NullRemoteOffMeshEntry)
            {
                ref var entry = ref this.remoteOffMeshLookupEntries[entryIndex];
                if (entry.Destination.Equals(destination) &&
                    entry.SourceTileRef == sourceTileRef &&
                    entry.OffMeshConnectionIndex == offMeshConnectionIndex)
                {
                    if (previousEntryIndex == NullRemoteOffMeshEntry)
                    {
                        this.remoteOffMeshLookupBuckets[hash] = entry.Next;
                    }
                    else
                    {
                        this.remoteOffMeshLookupEntries[previousEntryIndex].Next = entry.Next;
                    }

                    entry.Next = this.remoteOffMeshLookupFreeList;
                    this.remoteOffMeshLookupFreeList = entryIndex;
                    return;
                }

                previousEntryIndex = entryIndex;
                entryIndex = entry.Next;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int2 GetOffMeshConnectionDestinationTile(in float3 endPosition)
        {
            this.CalculateTileLocation(endPosition, out var tileX, out var tileY);
            return new int2(tileX, tileY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AllocLink(DtMeshTile* tile)
        {
            if (tile->linksFreeList == Detour.DTNullLink)
            {
                return Detour.DTNullLink;
            }

            var link = tile->linksFreeList;
            tile->linksFreeList = tile->links[link].next;
            return link;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FreeLink(DtMeshTile* tile, uint link)
        {
            tile->links[link].next = tile->linksFreeList;
            tile->linksFreeList = link;
        }

        private void ConnectInternalLinks(DtMeshTile* tile)
        {
            if (tile == null)
            {
                return;
            }

            var polyRefBase = this.GetPolyRefBase(tile);

            for (var i = 0; i < tile->header->polyCount; ++i)
            {
                var poly = &tile->polys[i];
                poly->firstLink = Detour.DTNullLink;

                if (poly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
                {
                    continue;
                }

                // Build edge links backwards so that the links will be
                // in the linked list from lowest index to highest
                for (var j = poly->vertCount - 1; j >= 0; --j)
                {
                    // Skip hard and non-internal edges
                    if (poly->neis[j] == 0 || (poly->neis[j] & Detour.DTExtLink) != 0)
                    {
                        continue;
                    }

                    var linkIndex = AllocLink(tile);
                    if (linkIndex != Detour.DTNullLink)
                    {
                        var link = &tile->links[linkIndex];
                        link->polyRef = polyRefBase | (uint)(poly->neis[j] - 1);
                        link->edge = (byte)j;
                        link->side = 0xff;
                        link->bmin = link->bmax = 0;

                        // Add to linked list
                        link->next = poly->firstLink;
                        poly->firstLink = linkIndex;
                    }
                }
            }
        }

        private void BaseOffMeshLinks(DtMeshTile* tile)
        {
            if (tile == null)
            {
                return;
            }

            var polyRefBase = this.GetPolyRefBase(tile);

            // Base off-mesh connection start points
            for (var i = 0; i < tile->header->offMeshConCount; ++i)
            {
                var connection = &tile->offMeshCons[i];
                var poly = &tile->polys[connection->poly];

                var halfExtents = new float3(connection->rad, tile->header->walkableClimb, connection->rad);

                // Find polygon to connect to
                var startPosition = connection->StartPos; // First vertex
                var polyRef = this.FindNearestPolyInTile(tile, startPosition, halfExtents, out var nearestPoint);
                if (polyRef == 0)
                {
                    continue;
                }

                // Further check to make sure findNearestPoly didn't return too optimistic results
                var distanceSquared = math.distancesq(nearestPoint.xz, startPosition.xz);
                if (distanceSquared > connection->rad * connection->rad)
                {
                    continue;
                }

                // Make sure the location is on current mesh
                var vertexPosition = &tile->verts[poly->verts[0]];
                *vertexPosition = nearestPoint;

                // Link off-mesh connection to target poly
                var linkIndex = AllocLink(tile);
                if (linkIndex != Detour.DTNullLink)
                {
                    var link = &tile->links[linkIndex];
                    link->polyRef = polyRef;
                    link->edge = 0;
                    link->side = 0xff;
                    link->bmin = link->bmax = 0;

                    // Add to linked list
                    link->next = poly->firstLink;
                    poly->firstLink = linkIndex;
                }

                // Start end-point is always connected back to off-mesh connection
                var reverseLinkIndex = AllocLink(tile);
                if (reverseLinkIndex != Detour.DTNullLink)
                {
                    var landPolyIndex = (ushort)this.DecodePolyIdPoly(polyRef);
                    var landPoly = &tile->polys[landPolyIndex];
                    var reverseLink = &tile->links[reverseLinkIndex];
                    reverseLink->polyRef = polyRefBase | (DtPolyRef)(int)connection->poly;
                    reverseLink->edge = 0xff;
                    reverseLink->side = 0xff;
                    reverseLink->bmin = reverseLink->bmax = 0;

                    // Add to linked list
                    reverseLink->next = landPoly->firstLink;
                    landPoly->firstLink = reverseLinkIndex;
                }
            }
        }

        private void ConnectExternalLinks(DtMeshTile* tile, DtMeshTile* target, int side)
        {
            if (tile == null)
            {
                return;
            }

            var connections = stackalloc DtPolyRef[4];
            var connectionAreas = stackalloc float[4 * 2];

            // Connect border links.
            for (var i = 0; i < tile->header->polyCount; ++i)
            {
                var poly = &tile->polys[i];

                var nv = poly->vertCount;
                for (var j = 0; j < nv; ++j)
                {
                    // Skip non-portal edges
                    if ((poly->neis[j] & Detour.DTExtLink) == 0)
                    {
                        continue;
                    }

                    // Extract direction (4-bit mask is correct)
                    var dir = (byte)(poly->neis[j] & 0xf);
                    if (side != -1 && dir != side)
                    {
                        continue;
                    }

                    // Get edge vertices with proper stride
                    var va = tile->verts[poly->verts[j]];
                    var vb = tile->verts[poly->verts[(j + 1) % nv]];

                    // Find connecting polygons in target tile
                    var connectionCount = this.FindConnectingPolys(va, vb, target, Detour.OppositeTile(dir), connections, connectionAreas, 4);

                    for (var k = 0; k < connectionCount; ++k)
                    {
                        var linkIndex = AllocLink(tile);
                        if (linkIndex != Detour.DTNullLink)
                        {
                            var link = &tile->links[linkIndex];
                            link->polyRef = connections[k];
                            link->edge = (byte)j;
                            link->side = dir;

                            // Add to linked list
                            link->next = poly->firstLink;
                            poly->firstLink = linkIndex;

                            // Convert slab coordinates back to parametric coordinates along the edge
                            if (dir is 0 or 4)
                            {
                                // X-aligned edges, use Z-axis
                                var denominator = vb.z - va.z;
                                if (math.abs(denominator) > 1e-6f)
                                {
                                    var tmin = (connectionAreas[(k * 2) + 0] - va.z) / denominator;
                                    var tmax = (connectionAreas[(k * 2) + 1] - va.z) / denominator;

                                    // Ensure tmin <= tmax
                                    if (tmin > tmax)
                                    {
                                        (tmin, tmax) = (tmax, tmin);
                                    }

                                    link->bmin = (byte)math.round(math.clamp(tmin, 0.0f, 1.0f) * 255.0f);
                                    link->bmax = (byte)math.round(math.clamp(tmax, 0.0f, 1.0f) * 255.0f);
                                }
                                else
                                {
                                    // Degenerate edge - no portal limits
                                    link->bmin = link->bmax = 0;
                                }
                            }
                            else if (dir is 2 or 6)
                            {
                                // Z-aligned edges, use X-axis
                                var denominator = vb.x - va.x;
                                if (math.abs(denominator) > 1e-6f)
                                {
                                    var tmin = (connectionAreas[(k * 2) + 0] - va.x) / denominator;
                                    var tmax = (connectionAreas[(k * 2) + 1] - va.x) / denominator;

                                    // Ensure tmin <= tmax
                                    if (tmin > tmax)
                                    {
                                        (tmin, tmax) = (tmax, tmin);
                                    }

                                    link->bmin = (byte)math.round(math.clamp(tmin, 0.0f, 1.0f) * 255.0f);
                                    link->bmax = (byte)math.round(math.clamp(tmax, 0.0f, 1.0f) * 255.0f);
                                }
                                else
                                {
                                    // Degenerate edge - no portal limits
                                    link->bmin = link->bmax = 0;
                                }
                            }
                            else
                            {
                                // Other directions or invalid - no portal limits
                                link->bmin = link->bmax = 0;
                            }
                        }
                    }
                }
            }
        }

        private void ConnectOffMeshLinksFromTile(DtMeshTile* tile)
        {
            if (tile == null || tile->header == null || tile->header->offMeshConCount == 0)
            {
                return;
            }

            for (var i = 0; i < tile->header->offMeshConCount; ++i)
            {
                this.ReconnectOffMeshLink(tile, i);
            }
        }

        private void ConnectOffMeshLinksToTile(DtMeshTile* tile)
        {
            if (tile == null || tile->header == null)
            {
                return;
            }

            var tileRef = this.GetTileRef(tile);
            var destination = new int2(tile->header->x, tile->header->y);
            var hash = ComputeTileHash(destination.x, destination.y, this.remoteOffMeshLookupMask);
            var entryIndex = this.remoteOffMeshLookupBuckets[hash];

            while (entryIndex != NullRemoteOffMeshEntry)
            {
                ref var entry = ref this.remoteOffMeshLookupEntries[entryIndex];
                if (entry.Destination.Equals(destination) &&
                    entry.SourceTileRef != tileRef &&
                    this.TryGetOffMeshLookupSourceTile(entry.SourceTileRef, entry.OffMeshConnectionIndex, out var sourceTile))
                {
                    this.ReconnectOffMeshLink(sourceTile, entry.OffMeshConnectionIndex);
                }

                entryIndex = entry.Next;
            }
        }

        private void ReconnectOffMeshLink(DtMeshTile* sourceTile, int offMeshConnectionIndex)
        {
            if (sourceTile == null || sourceTile->header == null)
            {
                return;
            }

            if (offMeshConnectionIndex < 0 || offMeshConnectionIndex >= sourceTile->header->offMeshConCount)
            {
                return;
            }

            var sourceConnection = &sourceTile->offMeshCons[offMeshConnectionIndex];
            var sourcePoly = &sourceTile->polys[sourceConnection->poly];
            if (!TryGetOffMeshBasePolyRef(sourceTile, sourcePoly, out var basePolyRef))
            {
                return;
            }

            var destinationRefs = stackalloc DtPolyRef[MaxNeighbourTiles];
            var destinationRefCount = RemoveOffMeshLandingLinks(sourceTile, sourcePoly, destinationRefs, MaxNeighbourTiles);
            if (destinationRefCount > 0)
            {
                var sourceOffMeshPolyRef = this.GetPolyRefBase(sourceTile) | (DtPolyRef)(int)sourceConnection->poly;
                for (var i = 0; i < destinationRefCount; ++i)
                {
                    if (Detour.StatusFailed(this.GetTileAndPolyByRef(destinationRefs[i], out var previousDestinationTile, out var previousDestinationPoly)))
                    {
                        continue;
                    }

                    var keepCount = previousDestinationTile == sourceTile && destinationRefs[i] == basePolyRef ? 1 : 0;
                    RemoveLinksToPolyRef(previousDestinationTile, previousDestinationPoly, sourceOffMeshPolyRef, keepCount);
                }
            }

            if (!this.TryFindBestOffMeshLanding(sourceTile, sourceConnection, out var destinationTile, out var polyRef, out var nearestPoint))
            {
                return;
            }

            this.ConnectOffMeshLink(sourceTile, sourceConnection, sourcePoly, destinationTile, polyRef, nearestPoint);
        }

        private bool TryFindBestOffMeshLanding(
            DtMeshTile* sourceTile,
            DtOffMeshConnection* sourceConnection,
            out DtMeshTile* destinationTile,
            out DtPolyRef destinationPolyRef,
            out float3 nearestPoint)
        {
            destinationTile = null;
            destinationPolyRef = 0;
            nearestPoint = sourceConnection->EndPos;

            if (sourceTile == null || sourceTile->header == null || sourceConnection == null)
            {
                return false;
            }

            var halfExtents = new float3(sourceConnection->rad, sourceTile->header->walkableClimb, sourceConnection->rad);
            var endPosition = sourceConnection->EndPos;
            var destination = this.GetOffMeshConnectionDestinationTile(endPosition);
            var destinationTiles = stackalloc DtMeshTile*[MaxNeighbourTiles];
            var destinationCount = this.GetTilesAt(destination.x, destination.y, destinationTiles, MaxNeighbourTiles);

            var bestDistanceSq = float.MaxValue;
            DtTileRef bestTileRef = 0;
            for (var i = 0; i < destinationCount; ++i)
            {
                var candidateTile = destinationTiles[i];
                var candidatePolyRef = this.FindNearestPolyInTile(candidateTile, endPosition, halfExtents, out var candidatePoint);
                if (candidatePolyRef == 0 || math.distancesq(candidatePoint.xz, endPosition.xz) > sourceConnection->rad * sourceConnection->rad)
                {
                    continue;
                }

                var candidateDistanceSq = math.lengthsq(candidatePoint - endPosition);
                var candidateTileRef = this.GetTileRef(candidateTile);
                if (candidateDistanceSq < bestDistanceSq ||
                    (candidateDistanceSq == bestDistanceSq && (bestTileRef == 0 || candidateTileRef < bestTileRef)))
                {
                    destinationTile = candidateTile;
                    destinationPolyRef = candidatePolyRef;
                    nearestPoint = candidatePoint;
                    bestDistanceSq = candidateDistanceSq;
                    bestTileRef = candidateTileRef;
                }
            }

            return destinationTile != null;
        }

        private void ConnectOffMeshLink(
            DtMeshTile* sourceTile,
            DtOffMeshConnection* sourceConnection,
            DtPoly* sourcePoly,
            DtMeshTile* destinationTile,
            DtPolyRef polyRef,
            in float3 nearestPoint)
        {
            if (sourceTile == null || sourceTile->header == null ||
                sourceConnection == null ||
                sourcePoly == null ||
                destinationTile == null ||
                destinationTile->header == null ||
                polyRef == 0)
            {
                return;
            }

            sourceTile->verts[sourcePoly->verts[1]] = nearestPoint;

            var linkIndex = AllocLink(sourceTile);
            if (linkIndex != Detour.DTNullLink)
            {
                var link = &sourceTile->links[linkIndex];
                link->polyRef = polyRef;
                link->edge = 1;
                link->side = byte.MaxValue;
                link->bmin = link->bmax = 0;
                link->next = sourcePoly->firstLink;
                sourcePoly->firstLink = linkIndex;
            }

            if ((sourceConnection->flags & Detour.DTOffMeshConBidir) == 0)
            {
                return;
            }

            var reverseLinkIndex = AllocLink(destinationTile);
            if (reverseLinkIndex == Detour.DTNullLink)
            {
                return;
            }

            var landPolyIndex = (ushort)this.DecodePolyIdPoly(polyRef);
            var landPoly = &destinationTile->polys[landPolyIndex];
            var reverseLink = &destinationTile->links[reverseLinkIndex];
            reverseLink->polyRef = this.GetPolyRefBase(sourceTile) | (DtPolyRef)(int)sourceConnection->poly;
            reverseLink->edge = 0xff;
            reverseLink->side = byte.MaxValue;
            reverseLink->bmin = reverseLink->bmax = 0;
            reverseLink->next = landPoly->firstLink;
            landPoly->firstLink = reverseLinkIndex;
        }

        private bool TryGetOffMeshLookupSourceTile(DtTileRef sourceTileRef, int offMeshConnectionIndex, out DtMeshTile* sourceTile)
        {
            sourceTile = this.GetTileByRef(sourceTileRef);
            return sourceTile != null &&
                   sourceTile->header != null &&
                   offMeshConnectionIndex >= 0 &&
                   offMeshConnectionIndex < sourceTile->header->offMeshConCount;
        }

        private void UnconnectRemoteIncomingOffMeshLinks(DtMeshTile* tile)
        {
            if (tile == null || tile->header == null)
            {
                return;
            }

            var destination = new int2(tile->header->x, tile->header->y);
            var hash = ComputeTileHash(destination.x, destination.y, this.remoteOffMeshLookupMask);
            var entryIndex = this.remoteOffMeshLookupBuckets[hash];

            while (entryIndex != NullRemoteOffMeshEntry)
            {
                ref var entry = ref this.remoteOffMeshLookupEntries[entryIndex];
                if (entry.Destination.Equals(destination) &&
                    this.TryGetOffMeshLookupSourceTile(entry.SourceTileRef, entry.OffMeshConnectionIndex, out var sourceTile))
                {
                    this.ReconnectOffMeshLink(sourceTile, entry.OffMeshConnectionIndex);
                }

                entryIndex = entry.Next;
            }
        }

        private void UnconnectRemoteOutgoingOffMeshLinks(DtMeshTile* tile)
        {
            if (tile == null || tile->header == null || tile->header->offMeshConCount == 0)
            {
                return;
            }

            var destinationTiles = stackalloc DtMeshTile*[MaxNeighbourTiles];
            for (var i = 0; i < tile->header->offMeshConCount; ++i)
            {
                var destination = this.GetOffMeshConnectionDestinationTile(tile->offMeshCons[i].EndPos);
                var destinationCount = this.GetTilesAt(destination.x, destination.y, destinationTiles, MaxNeighbourTiles);
                for (var j = 0; j < destinationCount; ++j)
                {
                    this.UnconnectLinks(destinationTiles[j], tile);
                }
            }
        }

        private void UnconnectLinks(DtMeshTile* tile, DtMeshTile* target)
        {
            if (tile == null || target == null)
            {
                return;
            }

            var targetTileNumber = this.DecodePolyIdTile(this.GetTileRef(target));

            for (var i = 0; i < tile->header->polyCount; ++i)
            {
                var poly = &tile->polys[i];
                var linkIndex = poly->firstLink;
                var prevLinkIndex = Detour.DTNullLink;

                while (linkIndex != Detour.DTNullLink)
                {
                    if (this.DecodePolyIdTile(tile->links[linkIndex].polyRef) == targetTileNumber)
                    {
                        // Remove link
                        var nextLinkIndex = tile->links[linkIndex].next;
                        if (prevLinkIndex == Detour.DTNullLink)
                        {
                            poly->firstLink = nextLinkIndex;
                        }
                        else
                        {
                            tile->links[prevLinkIndex].next = nextLinkIndex;
                        }

                        FreeLink(tile, linkIndex);
                        linkIndex = nextLinkIndex;
                    }
                    else
                    {
                        // Advance
                        prevLinkIndex = linkIndex;
                        linkIndex = tile->links[linkIndex].next;
                    }
                }
            }
        }

        private static int RemoveOffMeshLandingLinks(DtMeshTile* tile, DtPoly* poly, DtPolyRef* removedRefs, int maxRemovedRefs)
        {
            if (tile == null || poly == null)
            {
                return 0;
            }

            var removedCount = 0;
            var linkIndex = poly->firstLink;
            var prevLinkIndex = Detour.DTNullLink;

            while (linkIndex != Detour.DTNullLink)
            {
                var link = &tile->links[linkIndex];
                var nextLinkIndex = link->next;
                if (link->edge == 1)
                {
                    if (removedCount < maxRemovedRefs)
                    {
                        removedRefs[removedCount++] = link->polyRef;
                    }

                    if (prevLinkIndex == Detour.DTNullLink)
                    {
                        poly->firstLink = nextLinkIndex;
                    }
                    else
                    {
                        tile->links[prevLinkIndex].next = nextLinkIndex;
                    }

                    FreeLink(tile, linkIndex);
                }
                else
                {
                    prevLinkIndex = linkIndex;
                }

                linkIndex = nextLinkIndex;
            }

            return removedCount;
        }

        private static void RemoveLinksToPolyRef(DtMeshTile* tile, DtPoly* poly, DtPolyRef polyRef, int keepCount)
        {
            if (tile == null || poly == null)
            {
                return;
            }

            var matchingLinks = 0;
            for (var linkIndex = poly->firstLink; linkIndex != Detour.DTNullLink; linkIndex = tile->links[linkIndex].next)
            {
                if (tile->links[linkIndex].polyRef == polyRef)
                {
                    matchingLinks++;
                }
            }

            var linksToRemove = matchingLinks - keepCount;
            if (linksToRemove <= 0)
            {
                return;
            }

            var linkIndexToRemove = poly->firstLink;
            var prevLinkIndex = Detour.DTNullLink;
            while (linkIndexToRemove != Detour.DTNullLink && linksToRemove > 0)
            {
                var nextLinkIndex = tile->links[linkIndexToRemove].next;
                if (tile->links[linkIndexToRemove].polyRef == polyRef)
                {
                    if (prevLinkIndex == Detour.DTNullLink)
                    {
                        poly->firstLink = nextLinkIndex;
                    }
                    else
                    {
                        tile->links[prevLinkIndex].next = nextLinkIndex;
                    }

                    FreeLink(tile, linkIndexToRemove);
                    linksToRemove--;
                }
                else
                {
                    prevLinkIndex = linkIndexToRemove;
                }

                linkIndexToRemove = nextLinkIndex;
            }
        }

        private static bool TryGetOffMeshBasePolyRef(DtMeshTile* tile, DtPoly* poly, out DtPolyRef basePolyRef)
        {
            basePolyRef = 0;

            if (tile == null || poly == null)
            {
                return false;
            }

            for (var linkIndex = poly->firstLink; linkIndex != Detour.DTNullLink; linkIndex = tile->links[linkIndex].next)
            {
                var link = &tile->links[linkIndex];
                if (link->edge == 0)
                {
                    basePolyRef = link->polyRef;
                    return true;
                }
            }

            return false;
        }

        private DtPolyRef FindNearestPolyInTile(DtMeshTile* tile, float3 center, float3 halfExtents, out float3 nearestPoint)
        {
            nearestPoint = center;

            if (tile == null || tile->header == null)
            {
                return 0;
            }

            var bmin = center - halfExtents;
            var bmax = center + halfExtents;
            var polys = stackalloc DtPolyRef[128];
            var polyCount = this.QueryPolygonsInTile(tile, bmin, bmax, polys, 128);

            DtPolyRef nearest = 0;
            var nearestDistanceSq = float.MaxValue;
            for (var i = 0; i < polyCount; ++i)
            {
                var polyRef = polys[i];
                this.ClosestPointOnPoly(polyRef, center, out var closestPoint, out var posOverPoly);

                var diff = center - closestPoint;
                float distanceSq;
                if (posOverPoly)
                {
                    var verticalDistance = math.abs(diff.y) - tile->header->walkableClimb;
                    distanceSq = verticalDistance > 0 ? verticalDistance * verticalDistance : 0;
                }
                else
                {
                    distanceSq = math.lengthsq(diff);
                }

                if (distanceSq < nearestDistanceSq)
                {
                    nearestPoint = closestPoint;
                    nearestDistanceSq = distanceSq;
                    nearest = polyRef;
                }
            }

            return nearest;
        }

        private int QueryPolygonsInTile(DtMeshTile* tile, in float3 qmin, in float3 qmax, DtPolyRef* polys, int maxPolys)
        {
            if (tile->bvTree != null)
            {
                var node = &tile->bvTree[0];
                var end = &tile->bvTree[tile->header->bvNodeCount];
                var tbmin = tile->header->bmin;
                var tbmax = tile->header->bmax;
                var qfac = tile->header->bvQuantFactor;

                var minx = math.clamp(qmin.x, tbmin.x, tbmax.x) - tbmin.x;
                var miny = math.clamp(qmin.y, tbmin.y, tbmax.y) - tbmin.y;
                var minz = math.clamp(qmin.z, tbmin.z, tbmax.z) - tbmin.z;
                var maxx = math.clamp(qmax.x, tbmin.x, tbmax.x) - tbmin.x;
                var maxy = math.clamp(qmax.y, tbmin.y, tbmax.y) - tbmin.y;
                var maxz = math.clamp(qmax.z, tbmin.z, tbmax.z) - tbmin.z;

                var bmin = new ushort3(
                    (ushort)((ushort)(qfac * minx) & 0xfffe),
                    (ushort)((ushort)(qfac * miny) & 0xfffe),
                    (ushort)((ushort)(qfac * minz) & 0xfffe));
                var bmax = new ushort3(
                    (ushort)((ushort)((qfac * maxx) + 1) | 1),
                    (ushort)((ushort)((qfac * maxy) + 1) | 1),
                    (ushort)((ushort)((qfac * maxz) + 1) | 1));

                var polyRefBase = this.GetPolyRefBase(tile);
                var n = 0;
                while (node < end)
                {
                    var overlap = Detour.OverlapQuantBounds(bmin, bmax, node->bmin, node->bmax);
                    var isLeafNode = node->i >= 0;

                    if (isLeafNode && overlap)
                    {
                        if (n < maxPolys)
                        {
                            polys[n++] = polyRefBase | (DtPolyRef)node->i;
                        }
                    }

                    if (overlap || isLeafNode)
                    {
                        node++;
                    }
                    else
                    {
                        node += -node->i;
                    }
                }

                return n;
            }

            var count = 0;
            var polyRefBaseLinear = this.GetPolyRefBase(tile);
            for (var i = 0; i < tile->header->polyCount; ++i)
            {
                var poly = &tile->polys[i];
                if (poly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
                {
                    continue;
                }

                var boundsMin = tile->verts[poly->verts[0]];
                var boundsMax = boundsMin;
                for (var j = 1; j < poly->vertCount; ++j)
                {
                    var vertex = tile->verts[poly->verts[j]];
                    boundsMin = math.min(boundsMin, vertex);
                    boundsMax = math.max(boundsMax, vertex);
                }

                if (Detour.OverlapBounds(qmin, qmax, boundsMin, boundsMax))
                {
                    if (count < maxPolys)
                    {
                        polys[count++] = polyRefBaseLinear | (DtPolyRef)i;
                    }
                }
            }

            return count;
        }

        private int FindConnectingPolys(float3 va, float3 vb, DtMeshTile* tile, int side,
            DtPolyRef* connections, float* connectionAreas, int maxConnections)
        {
            if (tile == null)
            {
                return 0;
            }

            // Calculate slab coordinates for the input segment
            CalcSlabEndPoints(va, vb, out var amin, out var amax, side);
            var apos = GetSlabCoord(va, side);

            ushort m = (ushort)(Detour.DTExtLink | side);
            int n = 0;

            var polyRefBase = this.GetPolyRefBase(tile);

            for (uint i = 0; i < tile->header->polyCount; ++i)
            {
                var poly = &tile->polys[i];
                var nv = poly->vertCount;
                for (var j = 0; j < nv; ++j)
                {
                    // CRITICAL: Check for exact match of side
                    if (poly->neis[j] != m)
                    {
                        continue;
                    }

                    var vc = tile->verts[poly->verts[j]];
                    var vd = tile->verts[poly->verts[(j + 1) % nv]];
                    var bpos = GetSlabCoord(vc, side);

                    // Position alignment check with tolerance
                    if (math.abs(apos - bpos) > 0.01f)
                    {
                        continue;
                    }

                    // Check segment overlap using slab coordinates
                    CalcSlabEndPoints(vc, vd, out var bmin, out var bmax, side);

                    if (!OverlapSlabs(amin, amax, bmin, bmax, 0.01f, tile->header->walkableClimb))
                    {
                        continue;
                    }

                    // Store the overlapping area in slab coordinates
                    if (n < maxConnections)
                    {
                        connectionAreas[(n * 2) + 0] = math.max(amin.x, bmin.x);
                        connectionAreas[(n * 2) + 1] = math.min(amax.x, bmax.x);
                        connections[n] = polyRefBase | i;
                        n++;
                    }

                    break; // Only one connection per polygon
                }
            }

            return n;
        }

        private static void CalcSlabEndPoints(float3 va, float3 vb, out float2 bmin, out float2 bmax, int side)
        {
            bmin = float2.zero;
            bmax = float2.zero;

            if (side is 0 or 4)
            {
                // X-aligned edges, project to Z-Y
                if (va.z < vb.z)
                {
                    bmin.x = va.z;
                    bmin.y = va.y;
                    bmax.x = vb.z;
                    bmax.y = vb.y;
                }
                else
                {
                    bmin.x = vb.z;
                    bmin.y = vb.y;
                    bmax.x = va.z;
                    bmax.y = va.y;
                }

                return;
            }

            // Z-aligned edges, project to X-Y
            if (side is 2 or 6)
            {
                if (va.x < vb.x)
                {
                    bmin.x = va.x;
                    bmin.y = va.y;
                    bmax.x = vb.x;
                    bmax.y = vb.y;
                }
                else
                {
                    bmin.x = vb.x;
                    bmin.y = vb.y;
                    bmax.x = va.x;
                    bmax.y = va.y;
                }
            }
        }

        private static float GetSlabCoord(float3 va, int side)
        {
            // X-aligned edges
            if (side is 0 or 4)
            {
                return va.x;
            }

            // Z-aligned edges
            if (side is 2 or 6)
            {
                return va.z;
            }

            return 0;
        }

        private static bool OverlapSlabs(float2 amin, float2 amax, float2 bmin, float2 bmax, float px, float py)
        {
            // Check for horizontal overlap with shrinkage
            var minx = math.max(amin.x + px, bmin.x + px);
            var maxx = math.min(amax.x - px, bmax.x - px);
            if (minx > maxx)
            {
                return false;
            }

            // Check vertical overlap using linear interpolation
            var ad = (amax.y - amin.y) / (amax.x - amin.x);
            var ak = amin.y - (ad * amin.x);
            var bd = (bmax.y - bmin.y) / (bmax.x - bmin.x);
            var bk = bmin.y - (bd * bmin.x);
            var aminy = (ad * minx) + ak;
            var amaxy = (ad * maxx) + ak;
            var bminy = (bd * minx) + bk;
            var bmaxy = (bd * maxx) + bk;
            var dmin = bminy - aminy;
            var dmax = bmaxy - amaxy;

            // Crossing segments always overlap
            if (dmin * dmax < 0)
            {
                return true;
            }

            // Check for overlap at endpoints with walkableClimb tolerance
            var thr = py * py * 4; // dtSqr(py*2)
            if (dmin * dmin <= thr || dmax * dmax <= thr)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetDetailTriEdgeFlags(byte triFlags, byte edgeIndex)
        {
            return (byte)((triFlags >> (edgeIndex * 2)) & 0x3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Align4(int value)
        {
            return (value + 3) & ~3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DtTileState
        {
            public int magic;
            public int version;
            public DtTileRef tileRef;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DtPolyState
        {
            public ushort flags;
            public byte area;
        }
    }
}
