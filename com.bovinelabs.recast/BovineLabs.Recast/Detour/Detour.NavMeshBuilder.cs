// <copyright file="Detour.NavMeshBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>Creates Detour navigation mesh data from polygon mesh data.</summary>
    public static unsafe partial class Detour
    {
        /// <summary>
        /// Creates navigation mesh data from the specified polygon mesh data.
        /// </summary>
        /// <param name="createParams">Mesh creation parameters.</param>
        /// <param name="outData">The navigation mesh data.</param>
        /// <param name="outDataSize">Size of the navigation mesh data.</param>
        /// <param name="allocator">Allocator to use.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool CreateNavMeshData(DtNavMeshCreateParams* createParams, out DtNavMeshData outData, out int outDataSize,
            AllocatorManager.AllocatorHandle allocator)
        {
            outData = null;
            outDataSize = 0;

            if (createParams->Nvp > DTVertsPerPolygon)
            {
                return false;
            }

            if (createParams->VertCount >= 0xffff)
            {
                return false;
            }

            if (createParams->VertCount == 0 || createParams->Verts == null)
            {
                return false;
            }

            if (createParams->PolyCount == 0 || createParams->Polys == null)
            {
                return false;
            }

            var nvp = createParams->Nvp;

            // Classify off-mesh connection points. We store only the connections
            // whose start point is inside the tile.
            byte* offMeshConClass = null;
            var storedOffMeshConCount = 0;
            var offMeshBaseLinkCount = 0;
            var offMeshLandingLinkCount = 0;
            var offMeshLandingReverseLinkCount = 0;

            if (createParams->OffMeshConCount > 0)
            {
                offMeshConClass = (byte*)AllocatorManager.Allocate(Allocator.Temp, sizeof(byte) * createParams->OffMeshConCount * 2, UnsafeUtility.AlignOf<byte>());

                // Find tight height bounds, used for culling out off-mesh start locations.
                var hmin = float.MaxValue;
                var hmax = -float.MaxValue;

                if (createParams->DetailVerts != null && createParams->DetailVertsCount > 0)
                {
                    for (var i = 0; i < createParams->DetailVertsCount; ++i)
                    {
                        var h = createParams->DetailVerts[i].y;
                        hmin = math.min(hmin, h);
                        hmax = math.max(hmax, h);
                    }
                }
                else
                {
                    for (var i = 0; i < createParams->VertCount; ++i)
                    {
                        var iv = createParams->Verts[i];
                        var h = createParams->Bmin.y + (iv.y * createParams->Ch);
                        hmin = math.min(hmin, h);
                        hmax = math.max(hmax, h);
                    }
                }

                hmin -= createParams->WalkableClimb;
                hmax += createParams->WalkableClimb;
                var bmin = createParams->Bmin;
                var bmax = createParams->Bmax;
                bmin.y = hmin;
                bmax.y = hmax;

                for (var i = 0; i < createParams->OffMeshConCount; ++i)
                {
                    var connection = &createParams->OffMeshConVerts[i];
                    var start = connection->c0;
                    var end = connection->c1;
                    offMeshConClass[(i * 2) + 0] = ClassifyOffMeshPoint(start, bmin, bmax);
                    offMeshConClass[(i * 2) + 1] = ClassifyOffMeshPoint(end, bmin, bmax);

                    // Zero out off-mesh start positions which are not even potentially touching the mesh.
                    // And count how many links should be allocated for off-mesh connections.
                    if (offMeshConClass[(i * 2) + 0] == 0xff)
                    {
                        if (connection->c0.y < bmin.y || connection->c0.y > bmax.y)
                        {
                            offMeshConClass[(i * 2) + 0] = 0;
                        }
                        else
                        {
                            storedOffMeshConCount++;
                            offMeshBaseLinkCount += 2;
                            offMeshLandingLinkCount++;
                        }
                    }

                    if (offMeshConClass[(i * 2) + 1] == 0xff && createParams->OffMeshConDir[i] != 0)
                    {
                        offMeshLandingReverseLinkCount++;
                    }
                }
            }

            // Off-mesh connections are stored as polygons, adjust values.
            var totPolyCount = createParams->PolyCount + storedOffMeshConCount;
            var totVertCount = createParams->VertCount + (storedOffMeshConCount * 2);

            // Find portal edges which are at tile borders.
            var edgeCount = 0;
            var portalCount = 0;
            for (var i = 0; i < createParams->PolyCount; ++i)
            {
                var p = &createParams->Polys[i * 2 * nvp];
                for (var j = 0; j < nvp; ++j)
                {
                    if (p[j] == MeshNullIDX)
                    {
                        break;
                    }

                    edgeCount++;

                    if ((p[nvp + j] & 0x8000) != 0)
                    {
                        var dir = p[nvp + j] & 0xf;
                        if (dir != 0xf)
                        {
                            portalCount++;
                        }
                    }
                }
            }

            var maxLinkCount = edgeCount + (portalCount * 2) + offMeshBaseLinkCount + offMeshLandingLinkCount + offMeshLandingReverseLinkCount;

            // Find unique detail vertices.
            var uniqueDetailVertCount = 0;
            int detailTriCount;
            if (createParams->DetailMeshes != null)
            {
                // Has detail mesh, count unique detail vertex count and use input detail tri count.
                detailTriCount = createParams->DetailTriCount;
                for (var i = 0; i < createParams->PolyCount; ++i)
                {
                    var p = &createParams->Polys[i * nvp * 2];
                    var ndv = (int)createParams->DetailMeshes[i].y;
                    var nv = 0;
                    for (var j = 0; j < nvp; ++j)
                    {
                        if (p[j] == MeshNullIDX)
                        {
                            break;
                        }

                        nv++;
                    }

                    ndv -= nv;
                    uniqueDetailVertCount += ndv;
                }
            }
            else
            {
                // No input detail mesh, build detail mesh from nav polys.
                uniqueDetailVertCount = 0; // No extra detail verts.
                detailTriCount = 0;
                for (var i = 0; i < createParams->PolyCount; ++i)
                {
                    var p = &createParams->Polys[i * nvp * 2];
                    var nv = 0;
                    for (var j = 0; j < nvp; ++j)
                    {
                        if (p[j] == MeshNullIDX)
                        {
                            break;
                        }

                        nv++;
                    }

                    detailTriCount += nv - 2;
                }
            }

            // Calculate data size (no alignment padding needed)
            var headerSize = sizeof(DtMeshHeader);
            var vertsSize = sizeof(float3) * totVertCount;
            var polysSize = sizeof(DtPoly) * totPolyCount;
            var linksSize = sizeof(DtLink) * maxLinkCount;
            var detailMeshesSize = sizeof(DtPolyDetail) * createParams->PolyCount;
            var detailVertsSize = sizeof(float3) * uniqueDetailVertCount;
            var detailTrisSize = sizeof(byte4) * detailTriCount;
            var bvTreeSize = createParams->BuildBvTree ? sizeof(DtBVNode) * createParams->PolyCount * 2 : 0;
            var offMeshConsSize = sizeof(DtOffMeshConnection) * storedOffMeshConCount;

            var dataSize = headerSize + vertsSize + polysSize + linksSize +
                          detailMeshesSize + detailVertsSize + detailTrisSize +
                          bvTreeSize + offMeshConsSize;

            var data = (byte*)AllocatorManager.Allocate(allocator, dataSize, UnsafeUtility.AlignOf<byte>());
            UnsafeUtility.MemClear(data, dataSize);

            // Create wrapper and write header immediately
            var navMeshData = new DtNavMeshData(data);
            var header = navMeshData.Header;

            // Store header first (move this section up)
            header->magic = DTNavmeshMagic;
            header->version = DTNavmeshVersion;
            header->x = createParams->TileX;
            header->y = createParams->TileY;
            header->layer = createParams->TileLayer;
            header->userId = createParams->UserId;
            header->polyCount = totPolyCount;
            header->vertCount = totVertCount;
            header->maxLinkCount = maxLinkCount;
            header->bmin = createParams->Bmin;
            header->bmax = createParams->Bmax;
            header->detailMeshCount = createParams->PolyCount;
            header->detailVertCount = uniqueDetailVertCount;
            header->detailTriCount = detailTriCount;
            header->bvQuantFactor = 1.0f / createParams->Cs;
            header->offMeshBase = createParams->PolyCount;
            header->walkableHeight = createParams->WalkableHeight;
            header->walkableRadius = createParams->WalkableRadius;
            header->walkableClimb = createParams->WalkableClimb;
            header->offMeshConCount = storedOffMeshConCount;
            header->bvNodeCount = createParams->BuildBvTree ? createParams->PolyCount * 2 : 0;

            // Now use wrapper to get typed pointers to sections
            var navVerts = navMeshData.Vertices;
            var navPolys = navMeshData.Polygons;
            var navDMeshes = navMeshData.DetailMeshes;
            var navDVerts = navMeshData.DetailVertices;
            var navDTris = navMeshData.DetailTriangles;
            var navBvtree = navMeshData.BVTree;
            var offMeshCons = navMeshData.OffMeshConnections;

            var offMeshVertsBase = createParams->VertCount;
            var offMeshPolyBase = createParams->PolyCount;

            // Store vertices
            // Mesh vertices
            for (var i = 0; i < createParams->VertCount; ++i)
            {
                var iv = createParams->Verts[i];
                navVerts[i] = new float3(
                    createParams->Bmin.x + (iv.x * createParams->Cs),
                    createParams->Bmin.y + (iv.y * createParams->Ch),
                    createParams->Bmin.z + (iv.z * createParams->Cs));
            }

            // Off-mesh link vertices.
            var n = 0;
            for (var i = 0; i < createParams->OffMeshConCount; ++i)
            {
                // Only store connections which start from this tile.
                if (offMeshConClass[(i * 2) + 0] == 0xff)
                {
                    var link = createParams->OffMeshConVerts[i];
                    var vertIndex = offMeshVertsBase + (n * 2);
                    navVerts[vertIndex] = link.c0;
                    navVerts[vertIndex + 1] = link.c1;
                    n++;
                }
            }

            // Store polygons
            // Mesh polys
            var src = createParams->Polys;
            for (var i = 0; i < createParams->PolyCount; ++i)
            {
                var p = &navPolys[i];
                p->vertCount = 0;
                p->flags = createParams->PolyFlags[i];
                p->SetArea(createParams->PolyAreas[i]);
                p->SetType((byte)DtPolyTypes.PolytypeGround);
                for (var j = 0; j < nvp; ++j)
                {
                    if (src[j] == MeshNullIDX)
                    {
                        break;
                    }

                    p->verts[j] = src[j];
                    if ((src[nvp + j] & 0x8000) != 0)
                    {
                        // Border or portal edge.
                        var dir = src[nvp + j] & 0xf;
                        if (dir == 0xf) // Border
                        {
                            p->neis[j] = 0;
                        }
                        else if (dir == 0) // Portal x-
                        {
                            p->neis[j] = DTExtLink | 4;
                        }
                        else if (dir == 1) // Portal z+
                        {
                            p->neis[j] = DTExtLink | 2;
                        }
                        else if (dir == 2) // Portal x+
                        {
                            p->neis[j] = DTExtLink | 0;
                        }
                        else if (dir == 3) // Portal z-
                        {
                            p->neis[j] = DTExtLink | 6;
                        }
                    }
                    else
                    {
                        // Normal connection
                        p->neis[j] = (ushort)(src[nvp + j] + 1);
                    }

                    p->vertCount++;
                }

                src += nvp * 2;
            }

            // Off-mesh connection vertices.
            n = 0;
            for (var i = 0; i < createParams->OffMeshConCount; ++i)
            {
                // Only store connections which start from this tile.
                if (offMeshConClass[(i * 2) + 0] == 0xff)
                {
                    var p = &navPolys[offMeshPolyBase + n];
                    p->vertCount = 2;
                    p->verts[0] = (ushort)(offMeshVertsBase + (n * 2) + 0);
                    p->verts[1] = (ushort)(offMeshVertsBase + (n * 2) + 1);
                    p->flags = createParams->OffMeshConFlags[i];
                    p->SetArea(createParams->OffMeshConAreas[i]);
                    p->SetType((byte)DtPolyTypes.PolytypeOffMeshConnection);
                    n++;
                }
            }

            // Store detail meshes and vertices.
            // The nav polygon vertices are stored as the first vertices on each mesh.
            // We compress the mesh data by skipping them and using the navmesh coordinates.
            if (createParams->DetailMeshes != null)
            {
                var vbase = 0;
                for (var i = 0; i < createParams->PolyCount; ++i)
                {
                    var dtl = &navDMeshes[i];
                    var meshHeader = createParams->DetailMeshes[i];
                    var vb = (int)meshHeader.x;
                    var ndv = (int)meshHeader.y;
                    var nv = navPolys[i].vertCount;
                    dtl->vertBase = (uint)vbase;
                    dtl->vertCount = (byte)(ndv - nv);
                    dtl->triBase = meshHeader.z;
                    dtl->triCount = (byte)meshHeader.w;

                    // Copy vertices except the first 'nv' verts which are equal to nav poly verts.
                    if (ndv - nv > 0)
                    {
                        UnsafeUtility.MemCpy(navDVerts + vbase, createParams->DetailVerts + vb + nv, sizeof(float3) * (ndv - nv));
                        vbase += ndv - nv;
                    }
                }

                // Store triangles.
                UnsafeUtility.MemCpy(navDTris, createParams->DetailTris, sizeof(byte4) * createParams->DetailTriCount);
            }
            else
            {
                // Create dummy detail mesh by triangulating polys.
                var tbase = 0;
                for (var i = 0; i < createParams->PolyCount; ++i)
                {
                    var dtl = &navDMeshes[i];
                    var nv = navPolys[i].vertCount;
                    dtl->vertBase = 0;
                    dtl->vertCount = 0;
                    dtl->triBase = (uint)tbase;
                    dtl->triCount = (byte)(nv - 2);

                    // Triangulate polygon (local indices).
                    for (var j = 2; j < nv; ++j)
                    {
                        var t = navDTris + tbase;
                        t->x = 0;
                        t->y = (byte)(j - 1);
                        t->z = (byte)j;

                        // Bit for each edge that belongs to poly boundary.
                        t->w = 1 << 2;
                        if (j == 2)
                        {
                            t->w |= 1 << 0;
                        }

                        if (j == nv - 1)
                        {
                            t->w |= 1 << 4;
                        }

                        tbase++;
                    }
                }
            }

            // Store and create BVtree.
            if (createParams->BuildBvTree)
            {
                CreateBVTree(createParams, navBvtree);
            }

            // Store Off-Mesh connections.
            n = 0;
            for (var i = 0; i < createParams->OffMeshConCount; ++i)
            {
                // Only store connections which start from this tile.
                if (offMeshConClass[(i * 2) + 0] == 0xff)
                {
                    var con = &offMeshCons[n];
                    con->poly = (ushort)(offMeshPolyBase + n);

                    // Copy connection end-points.
                    var endPts = createParams->OffMeshConVerts[i];
                    con->StartPos = endPts.c0;
                    con->EndPos = endPts.c1;
                    con->rad = createParams->OffMeshConRad[i];
                    con->flags = (byte)(createParams->OffMeshConDir[i] != 0 ? DTOffMeshConBidir : 0);
                    con->side = offMeshConClass[(i * 2) + 1];
                    if (createParams->OffMeshConUserID != null)
                    {
                        con->userId = createParams->OffMeshConUserID[i];
                    }

                    n++;
                }
            }

            outData = navMeshData;
            outDataSize = dataSize;

            return true;
        }

        public static void FreeNavMeshData(DtNavMeshData outData, AllocatorManager.AllocatorHandle allocator)
        {
            if (outData.RawData == null)
            {
                return;
            }

            AllocatorManager.Free(allocator, outData.RawData);
        }

        /// <summary>
        /// Classifies a point based on which side of the tile boundary it lies.
        /// </summary>
        private static byte ClassifyOffMeshPoint(float3 pt, float3 bmin, float3 bmax)
        {
            const byte xp = 1 << 0;
            const byte zp = 1 << 1;
            const byte xm = 1 << 2;
            const byte zm = 1 << 3;

            byte outcode = 0;
            outcode |= (byte)(pt.x >= bmax.x ? xp : 0);
            outcode |= (byte)(pt.z >= bmax.z ? zp : 0);
            outcode |= (byte)(pt.x < bmin.x ? xm : 0);
            outcode |= (byte)(pt.z < bmin.z ? zm : 0);

            switch (outcode)
            {
                case xp: return 0;
                case xp | zp: return 1;
                case zp: return 2;
                case xm | zp: return 3;
                case xm: return 4;
                case xm | zm: return 5;
                case zm: return 6;
                case xp | zm: return 7;
            }

            return 0xff;
        }

        /// <summary>
        /// Creates a bounding volume tree for fast polygon queries.
        /// </summary>
        private static void CreateBVTree(DtNavMeshCreateParams* createParams, DtBVNode* nodes)
        {
            // Build tree
            var quantFactor = 1f / createParams->Cs;
            var items = (BVItem*)AllocatorManager.Allocate(Allocator.Temp, sizeof(BVItem) * createParams->PolyCount, UnsafeUtility.AlignOf<BVItem>());

            for (var i = 0; i < createParams->PolyCount; i++)
            {
                var it = &items[i];
                it->I = i;

                // Calc polygon bounds. Use detail meshes if available.
                if (createParams->DetailMeshes != null)
                {
                    var meshHeader = createParams->DetailMeshes[i];
                    var vb = (int)meshHeader.x;
                    var ndv = (int)meshHeader.y;

                    var bMin = createParams->DetailVerts[vb];
                    var bMax = bMin;

                    for (var j = 1; j < ndv; j++)
                    {
                        var v = createParams->DetailVerts[vb + j];
                        bMin = math.min(bMin, v);
                        bMax = math.max(bMax, v);
                    }

                    // BV-tree uses cs for all dimensions
                    it->BMin.x = (ushort)math.clamp((int)((bMin.x - createParams->Bmin.x) * quantFactor), 0, 0xffff);
                    it->BMin.y = (ushort)math.clamp((int)((bMin.y - createParams->Bmin.y) * quantFactor), 0, 0xffff);
                    it->BMin.z = (ushort)math.clamp((int)((bMin.z - createParams->Bmin.z) * quantFactor), 0, 0xffff);

                    it->BMax.x = (ushort)math.clamp((int)((bMax.x - createParams->Bmin.x) * quantFactor), 0, 0xffff);
                    it->BMax.y = (ushort)math.clamp((int)((bMax.y - createParams->Bmin.y) * quantFactor), 0, 0xffff);
                    it->BMax.z = (ushort)math.clamp((int)((bMax.z - createParams->Bmin.z) * quantFactor), 0, 0xffff);
                }
                else
                {
                    var p = &createParams->Polys[i * createParams->Nvp * 2];
                    var first = createParams->Verts[p[0]];
                    it->BMin.x = it->BMax.x = first.x;
                    it->BMin.y = it->BMax.y = first.y;
                    it->BMin.z = it->BMax.z = first.z;

                    for (var j = 1; j < createParams->Nvp; ++j)
                    {
                        if (p[j] == MeshNullIDX)
                        {
                            break;
                        }

                        var v = createParams->Verts[p[j]];
                        var x = v.x;
                        var y = v.y;
                        var z = v.z;

                        if (x < it->BMin.x)
                        {
                            it->BMin.x = x;
                        }

                        if (y < it->BMin.y)
                        {
                            it->BMin.y = y;
                        }

                        if (z < it->BMin.z)
                        {
                            it->BMin.z = z;
                        }

                        if (x > it->BMax.x)
                        {
                            it->BMax.x = x;
                        }

                        if (y > it->BMax.y)
                        {
                            it->BMax.y = y;
                        }

                        if (z > it->BMax.z)
                        {
                            it->BMax.z = z;
                        }
                    }

                    // Remap y
                    it->BMin.y = (ushort)math.floor(it->BMin.y * createParams->Ch / createParams->Cs);
                    it->BMax.y = (ushort)math.ceil(it->BMax.y * createParams->Ch / createParams->Cs);
                }
            }

            var curNode = 0;
            Subdivide(items, 0, createParams->PolyCount, ref curNode, nodes);
        }

        /// <summary>
        /// Recursively subdivides BV-tree items.
        /// </summary>
        private static void Subdivide(BVItem* items, int imin, int imax, ref int curNode, DtBVNode* nodes)
        {
            var inum = imax - imin;
            var icur = curNode;

            var node = &nodes[curNode++];

            if (inum == 1)
            {
                // Leaf
                node->bmin.x = items[imin].BMin.x;
                node->bmin.y = items[imin].BMin.y;
                node->bmin.z = items[imin].BMin.z;

                node->bmax.x = items[imin].BMax.x;
                node->bmax.y = items[imin].BMax.y;
                node->bmax.z = items[imin].BMax.z;

                node->i = items[imin].I;
            }
            else
            {
                // Split
                CalcExtends(items, imin, imax, out node->bmin, out node->bmax);

                var axis = LongestAxis(
                    (ushort)(node->bmax.x - node->bmin.x),
                    (ushort)(node->bmax.y - node->bmin.y),
                    (ushort)(node->bmax.z - node->bmin.z));

                // Sort using simple insertion sort
                if (axis == 0)
                {
                    SortItemsX(items + imin, inum);
                }
                else if (axis == 1)
                {
                    SortItemsY(items + imin, inum);
                }
                else
                {
                    SortItemsZ(items + imin, inum);
                }

                var isplit = imin + (inum / 2);

                // Left
                Subdivide(items, imin, isplit, ref curNode, nodes);

                // Right
                Subdivide(items, isplit, imax, ref curNode, nodes);

                var iescape = curNode - icur;

                // Negative index means escape.
                node->i = -iescape;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LongestAxis(ushort x, ushort y, ushort z)
        {
            var axis = 0;
            var maxVal = x;
            if (y > maxVal)
            {
                axis = 1;
                maxVal = y;
            }

            if (z > maxVal)
            {
                axis = 2;
            }

            return axis;
        }

        private static void CalcExtends(BVItem* items, int imin, int imax,
                                       out ushort3 bmin, out ushort3 bmax)
        {
            bmin.x = items[imin].BMin.x;
            bmin.y = items[imin].BMin.y;
            bmin.z = items[imin].BMin.z;

            bmax.x = items[imin].BMax.x;
            bmax.y = items[imin].BMax.y;
            bmax.z = items[imin].BMax.z;

            for (var i = imin + 1; i < imax; ++i)
            {
                ref readonly var it = ref items[i];
                if (it.BMin.x < bmin.x)
                {
                    bmin.x = it.BMin.x;
                }

                if (it.BMin.y < bmin.y)
                {
                    bmin.y = it.BMin.y;
                }

                if (it.BMin.z < bmin.z)
                {
                    bmin.z = it.BMin.z;
                }

                if (it.BMax.x > bmax.x)
                {
                    bmax.x = it.BMax.x;
                }

                if (it.BMax.y > bmax.y)
                {
                    bmax.y = it.BMax.y;
                }

                if (it.BMax.z > bmax.z)
                {
                    bmax.z = it.BMax.z;
                }
            }
        }

        private static void SortItemsX(BVItem* items, int num)
        {
            // Simple insertion sort
            for (var i = 1; i < num; i++)
            {
                var temp = items[i];
                var j = i - 1;
                while (j >= 0 && items[j].BMin.x > temp.BMin.x)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = temp;
            }
        }

        private static void SortItemsY(BVItem* items, int num)
        {
            // Simple insertion sort
            for (var i = 1; i < num; i++)
            {
                var temp = items[i];
                var j = i - 1;
                while (j >= 0 && items[j].BMin.y > temp.BMin.y)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = temp;
            }
        }

        private static void SortItemsZ(BVItem* items, int num)
        {
            // Simple insertion sort
            for (var i = 1; i < num; i++)
            {
                var temp = items[i];
                var j = i - 1;
                while (j >= 0 && items[j].BMin.z > temp.BMin.z)
                {
                    items[j + 1] = items[j];
                    j--;
                }

                items[j + 1] = temp;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BVItem
        {
            public ushort3 BMin;
            public ushort3 BMax;
            public int I;
        }
    }
}
