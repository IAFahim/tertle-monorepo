// <copyright file="Recast.Mesh.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Recast polygon mesh building functions for generating navigation meshes from contours.
    /// </summary>
    public static unsafe partial class Recast
    {
        private const int VertexBucketCount = 1 << 12;

        /// <summary>
        /// Builds a polygon mesh from the provided contour set.
        /// </summary>
        /// <param name="contourSet">A fully built contour set.</param>
        /// <param name="nvp">The maximum number of vertices per polygon [Limit: >= 3].</param>
        /// <param name="mesh">The resulting polygon mesh.</param>
        /// <returns>True if the operation completed successfully.</returns>
        public static bool BuildPolyMesh(RcContourSet* contourSet, int nvp, RcPolyMesh* mesh)
        {
            mesh->BMin = contourSet->Bmin;
            mesh->BMax = contourSet->Bmax;
            mesh->CellSize = contourSet->Cs;
            mesh->CellHeight = contourSet->Ch;
            mesh->BorderSize = contourSet->BorderSize;
            mesh->MaxEdgeError = contourSet->MaxError;

            var maxVertices = 0;
            var maxTris = 0;
            var maxVertsPerCont = 0;
            for (var i = 0; i < contourSet->Nconts; ++i)
            {
                // Skip null contours
                if (contourSet->Conts[i].NVerts < 3)
                {
                    continue;
                }

                maxVertices += contourSet->Conts[i].NVerts;
                maxTris += contourSet->Conts[i].NVerts - 2;
                maxVertsPerCont = math.max(maxVertsPerCont, contourSet->Conts[i].NVerts);
            }

            if (maxVertices >= 0xfffe)
            {
                return false;
            }

            var vflags = (byte*)AllocatorManager.Allocate(Allocator.Temp, sizeof(byte) * maxVertices, UnsafeUtility.AlignOf<byte>());
            UnsafeUtility.MemClear(vflags, maxVertices);

            mesh->Verts = (ushort3*)AllocatorManager.Allocate(mesh->Allocator, sizeof(ushort3) * maxVertices, UnsafeUtility.AlignOf<ushort3>());
            mesh->Polys = (ushort*)AllocatorManager.Allocate(mesh->Allocator, sizeof(ushort) * maxTris * nvp * 2, UnsafeUtility.AlignOf<ushort>());
            mesh->Regs = (ushort*)AllocatorManager.Allocate(mesh->Allocator, sizeof(ushort) * maxTris, UnsafeUtility.AlignOf<ushort>());
            mesh->Areas = (byte*)AllocatorManager.Allocate(mesh->Allocator, sizeof(byte) * maxTris, UnsafeUtility.AlignOf<byte>());

            mesh->NVerts = 0;
            mesh->NPolys = 0;
            mesh->Nvp = nvp;
            mesh->MaxPolys = maxTris;

            UnsafeUtility.MemClear(mesh->Verts, sizeof(ushort3) * maxVertices);
            UnsafeUtility.MemSet(mesh->Polys, 0xff, sizeof(ushort) * maxTris * nvp * 2);
            UnsafeUtility.MemClear(mesh->Regs, sizeof(ushort) * maxTris);
            UnsafeUtility.MemClear(mesh->Areas, sizeof(byte) * maxTris);

            var nextVert = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * maxVertices, UnsafeUtility.AlignOf<int>());
            UnsafeUtility.MemClear(nextVert, sizeof(int) * maxVertices);

            var firstVert = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * VertexBucketCount, UnsafeUtility.AlignOf<int>());

            for (var i = 0; i < VertexBucketCount; ++i)
            {
                firstVert[i] = -1;
            }

            var indices = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * maxVertsPerCont, UnsafeUtility.AlignOf<int>());
            var tris = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * maxVertsPerCont * 3, UnsafeUtility.AlignOf<int>());
            var polys = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * (maxVertsPerCont + 1) * nvp, UnsafeUtility.AlignOf<ushort>());

            var tmpPoly = &polys[maxVertsPerCont * nvp];

            for (var i = 0; i < contourSet->Nconts; ++i)
            {
                var cont = contourSet->Conts[i];

                // Skip null contours
                if (cont.NVerts < 3)
                {
                    continue;
                }

                // Triangulate contour
                for (var j = 0; j < cont.NVerts; ++j)
                {
                    indices[j] = j;
                }

                var ntris = Triangulate(cont.NVerts, (int*)cont.Verts, indices, tris);
                if (ntris <= 0)
                {
                    // Bad triangulation, should not happen
                    ntris = -ntris;
                }

                // Add and merge vertices
                for (var j = 0; j < cont.NVerts; ++j)
                {
                    var v = cont.Verts + j;
                    indices[j] = AddVertex((ushort)v->x, (ushort)v->y, (ushort)v->z,
                        mesh->Verts, firstVert, nextVert, ref mesh->NVerts);
                    if ((v->w & RCBorderVertex) != 0)
                    {
                        // This vertex should be removed
                        vflags[indices[j]] = 1;
                    }
                }

                // Build initial polygons
                var npolys = 0;
                UnsafeUtility.MemSet(polys, 0xff, maxVertsPerCont * nvp * sizeof(ushort));
                for (var j = 0; j < ntris; ++j)
                {
                    var t = &tris[j * 3];
                    if (t[0] != t[1] && t[0] != t[2] && t[1] != t[2])
                    {
                        polys[(npolys * nvp) + 0] = (ushort)indices[t[0]];
                        polys[(npolys * nvp) + 1] = (ushort)indices[t[1]];
                        polys[(npolys * nvp) + 2] = (ushort)indices[t[2]];
                        npolys++;
                    }
                }

                if (npolys == 0)
                {
                    continue;
                }

                // Merge polygons
                if (nvp > 3)
                {
                    while (true)
                    {
                        // Find best polygons to merge
                        var bestMergeVal = 0;
                        int bestPa = 0, bestPb = 0, bestEa = 0, bestEb = 0;

                        for (var j = 0; j < npolys - 1; ++j)
                        {
                            var pj = &polys[j * nvp];
                            for (var k = j + 1; k < npolys; ++k)
                            {
                                var pk = &polys[k * nvp];
                                int ea, eb;
                                var v = GetPolyMergeValue(pj, pk, mesh->Verts, out ea, out eb, nvp);
                                if (v > bestMergeVal)
                                {
                                    bestMergeVal = v;
                                    bestPa = j;
                                    bestPb = k;
                                    bestEa = ea;
                                    bestEb = eb;
                                }
                            }
                        }

                        if (bestMergeVal > 0)
                        {
                            // Found best, merge
                            var pa = &polys[bestPa * nvp];
                            var pb = &polys[bestPb * nvp];
                            MergePolyVerts(pa, pb, bestEa, bestEb, tmpPoly, nvp);
                            var lastPoly = &polys[(npolys - 1) * nvp];
                            if (pb != lastPoly)
                            {
                                UnsafeUtility.MemCpy(pb, lastPoly, sizeof(ushort) * nvp);
                            }

                            npolys--;
                        }
                        else
                        {
                            // Could not merge any polygons, stop
                            break;
                        }
                    }
                }

                // Store polygons
                for (var j = 0; j < npolys; ++j)
                {
                    var p = &mesh->Polys[mesh->NPolys * nvp * 2];
                    var q = &polys[j * nvp];
                    for (var k = 0; k < nvp; ++k)
                    {
                        p[k] = q[k];
                    }

                    mesh->Regs[mesh->NPolys] = cont.Reg;
                    mesh->Areas[mesh->NPolys] = cont.Area;
                    mesh->NPolys++;
                    if (mesh->NPolys > maxTris)
                    {
                        return false;
                    }
                }
            }

            // Remove edge vertices
            for (var i = 0; i < mesh->NVerts; ++i)
            {
                if (vflags[i] != 0)
                {
                    if (!CanRemoveVertex(mesh, (ushort)i))
                    {
                        continue;
                    }

                    if (!RemoveVertex(mesh, (ushort)i, maxTris))
                    {
                        // Failed to remove vertex
                        return false;
                    }

                    // Remove vertex
                    // Note: mesh.nverts is already decremented inside RemoveVertex()!
                    // Fixup vertex flags
                    for (var j = i; j < mesh->NVerts; ++j)
                    {
                        vflags[j] = vflags[j + 1];
                    }

                    --i;
                }
            }

            // Calculate adjacency
            if (!BuildMeshAdjacency(mesh->Polys, mesh->NPolys, mesh->NVerts, nvp))
            {
                return false;
            }

            // Find portal edges
            if (mesh->BorderSize > 0)
            {
                var w = contourSet->Width;
                var h = contourSet->Height;
                for (var i = 0; i < mesh->NPolys; ++i)
                {
                    var p = &mesh->Polys[i * 2 * nvp];
                    for (var j = 0; j < nvp; ++j)
                    {
                        if (p[j] == RCMeshNullIdx)
                        {
                            break;
                        }

                        // Skip connected edges
                        if (p[nvp + j] != RCMeshNullIdx)
                        {
                            continue;
                        }

                        var nj = j + 1;
                        if (nj >= nvp || p[nj] == RCMeshNullIdx)
                        {
                            nj = 0;
                        }

                        var va = mesh->Verts[p[j]];
                        var vb = mesh->Verts[p[nj]];

                        if (va.x == 0 && vb.x == 0)
                        {
                            p[nvp + j] = 0x8000 | 0;
                        }
                        else if (va.z == h && vb.z == h)
                        {
                            p[nvp + j] = 0x8000 | 1;
                        }
                        else if (va.x == w && vb.x == w)
                        {
                            p[nvp + j] = 0x8000 | 2;
                        }
                        else if (va.z == 0 && vb.z == 0)
                        {
                            p[nvp + j] = 0x8000 | 3;
                        }
                    }
                }
            }

            // Just allocate the mesh flags array. The user is responsible to fill it
            mesh->Flags = (ushort*)AllocatorManager.Allocate(mesh->Allocator, sizeof(ushort) * mesh->NPolys, UnsafeUtility.AlignOf<ushort>());
            UnsafeUtility.MemClear(mesh->Flags, sizeof(ushort) * mesh->NPolys);

            return true;
        }

        /// <summary>
        /// Merges polygon meshes into a single mesh.
        /// </summary>
        /// <param name="meshes">An array of polygon meshes to merge [Size: nmeshes].</param>
        /// <param name="nmeshes">The number of polygon meshes in the meshes array.</param>
        /// <param name="mesh">The resulting merged polygon mesh.</param>
        /// <returns>True if the operation completed successfully.</returns>
        public static bool MergePolyMeshes(RcPolyMesh** meshes, int nmeshes, RcPolyMesh* mesh)
        {
            if (nmeshes == 0 || meshes == null)
            {
                return true;
            }

            mesh->Nvp = meshes[0]->Nvp;
            mesh->CellSize = meshes[0]->CellSize;
            mesh->CellHeight = meshes[0]->CellHeight;
            mesh->BMin = meshes[0]->BMin;
            mesh->BMax = meshes[0]->BMax;

            var maxVerts = 0;
            var maxPolys = 0;
            var maxVertsPerMesh = 0;
            for (var i = 0; i < nmeshes; ++i)
            {
                mesh->BMin = math.min(mesh->BMin, meshes[i]->BMin);
                mesh->BMax = math.max(mesh->BMax, meshes[i]->BMax);
                maxVertsPerMesh = math.max(maxVertsPerMesh, meshes[i]->NVerts);
                maxVerts += meshes[i]->NVerts;
                maxPolys += meshes[i]->NPolys;
            }

            mesh->NVerts = 0;
            mesh->Verts = (ushort3*)AllocatorManager.Allocate(mesh->Allocator, sizeof(ushort3) * maxVerts, UnsafeUtility.AlignOf<ushort3>());

            mesh->NPolys = 0;
            mesh->Polys = (ushort*)AllocatorManager.Allocate(mesh->Allocator, sizeof(ushort) * maxPolys * 2 * mesh->Nvp, UnsafeUtility.AlignOf<ushort>());
            UnsafeUtility.MemSet(mesh->Polys, 0xff, sizeof(ushort) * maxPolys * 2 * mesh->Nvp);

            mesh->Regs = (ushort*)AllocatorManager.Allocate(mesh->Allocator, sizeof(ushort) * maxPolys, UnsafeUtility.AlignOf<ushort>());
            UnsafeUtility.MemClear(mesh->Regs, sizeof(ushort) * maxPolys);

            mesh->Areas = (byte*)AllocatorManager.Allocate(mesh->Allocator, sizeof(byte) * maxPolys, UnsafeUtility.AlignOf<byte>());
            UnsafeUtility.MemClear(mesh->Areas, sizeof(byte) * maxPolys);

            mesh->Flags = (ushort*)AllocatorManager.Allocate(mesh->Allocator, sizeof(ushort) * maxPolys, UnsafeUtility.AlignOf<ushort>());
            UnsafeUtility.MemClear(mesh->Flags, sizeof(ushort) * maxPolys);

            var nextVert = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * maxVerts, UnsafeUtility.AlignOf<int>());
            UnsafeUtility.MemClear(nextVert, sizeof(int) * maxVerts);

            var firstVert = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * VertexBucketCount, UnsafeUtility.AlignOf<int>());

            for (var i = 0; i < VertexBucketCount; ++i)
            {
                firstVert[i] = -1;
            }

            var vremap = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * maxVertsPerMesh, UnsafeUtility.AlignOf<ushort>());

            UnsafeUtility.MemClear(vremap, sizeof(ushort) * maxVertsPerMesh);

            for (var i = 0; i < nmeshes; ++i)
            {
                var pmesh = meshes[i];

                var ox = (ushort)math.floor(((pmesh->BMin.x - mesh->BMin.x) / mesh->CellSize) + 0.5f);
                var oz = (ushort)math.floor(((pmesh->BMin.z - mesh->BMin.z) / mesh->CellSize) + 0.5f);

                var isMinX = ox == 0;
                var isMinZ = oz == 0;
                var isMaxX = (ushort)math.floor(((mesh->BMax.x - pmesh->BMax.x) / mesh->CellSize) + 0.5f) == 0;
                var isMaxZ = (ushort)math.floor(((mesh->BMax.z - pmesh->BMax.z) / mesh->CellSize) + 0.5f) == 0;
                var isOnBorder = isMinX || isMinZ || isMaxX || isMaxZ;

                for (var j = 0; j < pmesh->NVerts; ++j)
                {
                    var v = pmesh->Verts[j];
                    vremap[j] = AddVertex((ushort)(v.x + ox), v.y, (ushort)(v.z + oz),
                        mesh->Verts, firstVert, nextVert, ref mesh->NVerts);
                }

                for (var j = 0; j < pmesh->NPolys; ++j)
                {
                    var tgt = &mesh->Polys[mesh->NPolys * 2 * mesh->Nvp];
                    var src = &pmesh->Polys[j * 2 * mesh->Nvp];
                    mesh->Regs[mesh->NPolys] = pmesh->Regs[j];
                    mesh->Areas[mesh->NPolys] = pmesh->Areas[j];
                    mesh->Flags[mesh->NPolys] = pmesh->Flags[j];
                    mesh->NPolys++;
                    for (var k = 0; k < mesh->Nvp; ++k)
                    {
                        if (src[k] == RCMeshNullIdx)
                        {
                            break;
                        }

                        tgt[k] = vremap[src[k]];
                    }

                    if (isOnBorder)
                    {
                        for (var k = mesh->Nvp; k < mesh->Nvp * 2; ++k)
                        {
                            if ((src[k] & 0x8000) != 0 && src[k] != 0xffff)
                            {
                                var dir = (ushort)(src[k] & 0xf);
                                switch (dir)
                                {
                                    case 0: // Portal x-
                                        if (isMinX)
                                        {
                                            tgt[k] = src[k];
                                        }

                                        break;
                                    case 1: // Portal z+
                                        if (isMaxZ)
                                        {
                                            tgt[k] = src[k];
                                        }

                                        break;
                                    case 2: // Portal x+
                                        if (isMaxX)
                                        {
                                            tgt[k] = src[k];
                                        }

                                        break;
                                    case 3: // Portal z-
                                        if (isMinZ)
                                        {
                                            tgt[k] = src[k];
                                        }

                                        break;
                                }
                            }
                        }
                    }
                }
            }

            // Calculate adjacency
            if (!BuildMeshAdjacency(mesh->Polys, mesh->NPolys, mesh->NVerts, mesh->Nvp))
            {
                return false;
            }

            return true;
        }

        private static bool BuildMeshAdjacency(ushort* polys, int npolys, int nverts, int vertsPerPoly)
        {
            // Based on code by Eric Lengyel from:
            // https://web.archive.org/web/20080704083314/http://www.terathon.com/code/edges.php
            var maxEdgeCount = npolys * vertsPerPoly;
            var firstEdge = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * (nverts + maxEdgeCount), UnsafeUtility.AlignOf<ushort>());

            var nextEdge = firstEdge + nverts;
            var edgeCount = 0;

            var edges = (Edge*)AllocatorManager.Allocate(Allocator.Temp, sizeof(Edge) * maxEdgeCount, UnsafeUtility.AlignOf<Edge>());

            for (var i = 0; i < nverts; i++)
            {
                firstEdge[i] = RCMeshNullIdx;
            }

            for (var i = 0; i < npolys; ++i)
            {
                var t = &polys[i * vertsPerPoly * 2];
                for (var j = 0; j < vertsPerPoly; ++j)
                {
                    if (t[j] == RCMeshNullIdx)
                    {
                        break;
                    }

                    var v0 = t[j];
                    var v1 = j + 1 >= vertsPerPoly || t[j + 1] == RCMeshNullIdx ? t[0] : t[j + 1];
                    if (v0 < v1)
                    {
                        var edge = &edges[edgeCount];
                        edge->Vert[0] = v0;
                        edge->Vert[1] = v1;
                        edge->Poly[0] = (ushort)i;
                        edge->PolyEdge[0] = (ushort)j;
                        edge->Poly[1] = (ushort)i;
                        edge->PolyEdge[1] = 0;
                        nextEdge[edgeCount] = firstEdge[v0];
                        firstEdge[v0] = (ushort)edgeCount;
                        edgeCount++;
                    }
                }
            }

            for (var i = 0; i < npolys; ++i)
            {
                var t = &polys[i * vertsPerPoly * 2];
                for (var j = 0; j < vertsPerPoly; ++j)
                {
                    if (t[j] == RCMeshNullIdx)
                    {
                        break;
                    }

                    var v0 = t[j];
                    var v1 = j + 1 >= vertsPerPoly || t[j + 1] == RCMeshNullIdx ? t[0] : t[j + 1];
                    if (v0 > v1)
                    {
                        for (var e = firstEdge[v1]; e != RCMeshNullIdx; e = nextEdge[e])
                        {
                            var edge = &edges[e];
                            if (edge->Vert[1] == v0 && edge->Poly[0] == edge->Poly[1])
                            {
                                edge->Poly[1] = (ushort)i;
                                edge->PolyEdge[1] = (ushort)j;
                                break;
                            }
                        }
                    }
                }
            }

            // Store adjacency
            for (var i = 0; i < edgeCount; ++i)
            {
                var e = &edges[i];
                if (e->Poly[0] != e->Poly[1])
                {
                    var p0 = &polys[e->Poly[0] * vertsPerPoly * 2];
                    var p1 = &polys[e->Poly[1] * vertsPerPoly * 2];
                    p0[vertsPerPoly + e->PolyEdge[0]] = e->Poly[1];
                    p1[vertsPerPoly + e->PolyEdge[1]] = e->Poly[0];
                }
            }

            return true;
        }

        private static int ComputeVertexHash(int x, int y, int z)
        {
            var h1 = 0x8da6b343; // Large multiplicative constants;
            var h2 = 0xd8163841; // here arbitrarily chosen primes
            var h3 = 0xcb1ab31f;
            var n = (h1 * (uint)x) + (h2 * (uint)y) + (h3 * (uint)z);
            return (int)(n & (VertexBucketCount - 1));
        }

        private static ushort AddVertex(ushort x, ushort y, ushort z, ushort3* verts, int* firstVert, int* nextVert, ref int nv)
        {
            var bucket = ComputeVertexHash(x, 0, z);
            var i = firstVert[bucket];

            while (i != -1)
            {
                var v = verts[i];
                if (v.x == x && math.abs(v.y - y) <= 2 && v.z == z)
                {
                    return (ushort)i;
                }

                i = nextVert[i]; // next
            }

            // Could not find, create new
            i = nv;
            nv++;
            verts[i] = new ushort3(x, y, z);
            nextVert[i] = firstVert[bucket];
            firstVert[bucket] = i;

            return (ushort)i;
        }

        // Returns T iff (v_i, v_j) is a proper internal *or* external
        // diagonal of P, *ignoring edges incident to v_i and v_j*.
        private static bool Diagonalie(int i, int j, int n, int* verts, int* indices)
        {
            var d0 = &verts[(indices[i] & 0x0fffffff) * 4];
            var d1 = &verts[(indices[j] & 0x0fffffff) * 4];

            // For each edge (k,k+1) of P
            for (var k = 0; k < n; k++)
            {
                var k1 = Next(k, n);

                // Skip edges incident to i or j
                if (!(k == i || k1 == i || k == j || k1 == j))
                {
                    var p0 = &verts[(indices[k] & 0x0fffffff) * 4];
                    var p1 = &verts[(indices[k1] & 0x0fffffff) * 4];

                    if (VEqual(d0, p0) || VEqual(d1, p0) || VEqual(d0, p1) || VEqual(d1, p1))
                    {
                        continue;
                    }

                    if (Intersect(d0, d1, p0, p1))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Returns true iff the diagonal (i,j) is strictly internal to the
        // polygon P in the neighborhood of the i endpoint.
        private static bool InCone(int i, int j, int n, int* verts, int* indices)
        {
            var pi = &verts[(indices[i] & 0x0fffffff) * 4];
            var pj = &verts[(indices[j] & 0x0fffffff) * 4];
            var pi1 = &verts[(indices[Next(i, n)] & 0x0fffffff) * 4];
            var pin1 = &verts[(indices[Prev(i, n)] & 0x0fffffff) * 4];

            // If P[i] is a convex vertex [ i+1 left or on (i-1,i) ].
            if (LeftOn(pin1, pi, pi1))
            {
                return Left(pi, pj, pin1) && Left(pj, pi, pi1);
            }

            // Assume (i-1,i,i+1) not collinear.
            // else P[i] is reflex.
            return !(LeftOn(pi, pj, pi1) && LeftOn(pj, pi, pin1));
        }

        // Returns T iff (v_i, v_j) is a proper internal
        // diagonal of P.
        private static bool Diagonal(int i, int j, int n, int* verts, int* indices)
        {
            return InCone(i, j, n, verts, indices) && Diagonalie(i, j, n, verts, indices);
        }

        private static int Triangulate(int n, int* verts, int* indices, int* tris)
        {
            var ntris = 0;
            var dst = tris;

            // The last bit of the index is used to indicate if the vertex can be removed
            for (var i = 0; i < n; i++)
            {
                var i1 = Next(i, n);
                var i2 = Next(i1, n);
                if (Diagonal(i, i2, n, verts, indices))
                {
                    indices[i1] |= unchecked((int)0x80000000);
                }
            }

            while (n > 3)
            {
                var minLen = -1;
                var mini = -1;
                for (var i = 0; i < n; i++)
                {
                    var i1 = Next(i, n);
                    if ((indices[i1] & unchecked((int)0x80000000)) != 0)
                    {
                        var p0 = &verts[(indices[i] & 0x0fffffff) * 4];
                        var p2 = &verts[(indices[Next(i1, n)] & 0x0fffffff) * 4];

                        var dx = p2[0] - p0[0];
                        var dy = p2[2] - p0[2];
                        var len = (dx * dx) + (dy * dy);

                        if (minLen < 0 || len < minLen)
                        {
                            minLen = len;
                            mini = i;
                        }
                    }
                }

                if (mini == -1)
                {
                    // The contour is messed up. This sometimes happens
                    // if the contour simplification is too aggressive.
                    return -ntris;
                }

                var j = mini;
                var j1 = Next(j, n);
                var j2 = Next(j1, n);

                *dst++ = indices[j] & 0x0fffffff;
                *dst++ = indices[j1] & 0x0fffffff;
                *dst++ = indices[j2] & 0x0fffffff;
                ntris++;

                // Removes P[i1] by copying P[i+1]...P[n-1] left one index
                n--;
                for (var k = j1; k < n; k++)
                {
                    indices[k] = indices[k + 1];
                }

                if (j1 >= n)
                {
                    j1 = 0;
                }

                j = Prev(j1, n);

                // Update diagonal flags
                if (Diagonal(Prev(j, n), j1, n, verts, indices))
                {
                    indices[j] |= unchecked((int)0x80000000);
                }
                else
                {
                    indices[j] &= 0x0fffffff;
                }

                if (Diagonal(j, Next(j1, n), n, verts, indices))
                {
                    indices[j1] |= unchecked((int)0x80000000);
                }
                else
                {
                    indices[j1] &= 0x0fffffff;
                }
            }

            // Append the remaining triangle
            *dst++ = indices[0] & 0x0fffffff;
            *dst++ = indices[1] & 0x0fffffff;
            *dst = indices[2] & 0x0fffffff;
            ntris++;

            return ntris;
        }

        private static int CountPolyVerts(ushort* p, int nvp)
        {
            for (var i = 0; i < nvp; ++i)
            {
                if (p[i] == RCMeshNullIdx)
                {
                    return i;
                }
            }

            return nvp;
        }

        private static bool Uleft(ushort3* a, ushort3* b, ushort3* c)
        {
            return ((b->x - a->x) * (c->z - a->z)) -
                   ((c->x - a->x) * (b->z - a->z)) < 0;
        }

        private static int GetPolyMergeValue(ushort* pa, ushort* pb, ushort3* verts, out int ea, out int eb, int nvp)
        {
            ea = -1;
            eb = -1;

            var na = CountPolyVerts(pa, nvp);
            var nb = CountPolyVerts(pb, nvp);

            // If the merged polygon would be too big, do not merge
            if (na + nb - 2 > nvp)
            {
                return -1;
            }

            // Check if the polygons share an edge
            for (var i = 0; i < na; ++i)
            {
                var va0 = pa[i];
                var va1 = pa[(i + 1) % na];
                if (va0 > va1)
                {
                    (va0, va1) = (va1, va0);
                }

                for (var j = 0; j < nb; ++j)
                {
                    var vb0 = pb[j];
                    var vb1 = pb[(j + 1) % nb];
                    if (vb0 > vb1)
                    {
                        (vb0, vb1) = (vb1, vb0);
                    }

                    if (va0 == vb0 && va1 == vb1)
                    {
                        ea = i;
                        eb = j;
                        break;
                    }
                }
            }

            // No common edge, cannot merge
            if (ea == -1 || eb == -1)
            {
                return -1;
            }

            // Check to see if the merged polygon would be convex
            ushort va, vb, vc;

            va = pa[(ea + na - 1) % na];
            vb = pa[ea];
            vc = pb[(eb + 2) % nb];
            if (!Uleft(verts + va, verts + vb, verts + vc))
            {
                return -1;
            }

            va = pb[(eb + nb - 1) % nb];
            vb = pb[eb];
            vc = pa[(ea + 2) % na];
            if (!Uleft(verts + va, verts + vb, verts + vc))
            {
                return -1;
            }

            va = pa[ea];
            vb = pa[(ea + 1) % na];

            var vaVertex = verts[va];
            var vbVertex = verts[vb];

            var dx = vaVertex.x - vbVertex.x;
            var dy = vaVertex.z - vbVertex.z;

            return (dx * dx) + (dy * dy);
        }

        private static void MergePolyVerts(ushort* pa, ushort* pb, int ea, int eb, ushort* tmp, int nvp)
        {
            var na = CountPolyVerts(pa, nvp);
            var nb = CountPolyVerts(pb, nvp);

            // Merge polygons
            UnsafeUtility.MemSet(tmp, 0xff, sizeof(ushort) * nvp);
            var n = 0;

            // Add pa
            for (var i = 0; i < na - 1; ++i)
            {
                tmp[n++] = pa[(ea + 1 + i) % na];
            }

            // Add pb
            for (var i = 0; i < nb - 1; ++i)
            {
                tmp[n++] = pb[(eb + 1 + i) % nb];
            }

            UnsafeUtility.MemCpy(pa, tmp, sizeof(ushort) * nvp);
        }

        private static bool CanRemoveVertex(RcPolyMesh* mesh, ushort rem)
        {
            var nvp = mesh->Nvp;

            // Count number of polygons to remove
            var numTouchedVerts = 0;
            var numRemainingEdges = 0;
            for (var i = 0; i < mesh->NPolys; ++i)
            {
                var p = &mesh->Polys[i * nvp * 2];
                var nv = CountPolyVerts(p, nvp);
                var numRemoved = 0;
                var numVerts = 0;
                for (var j = 0; j < nv; ++j)
                {
                    if (p[j] == rem)
                    {
                        numTouchedVerts++;
                        numRemoved++;
                    }

                    numVerts++;
                }

                if (numRemoved != 0)
                {
                    numRemainingEdges += numVerts - (numRemoved + 1);
                }
            }

            // There would be too few edges remaining to create a polygon
            // This can happen for example when a tip of a triangle is marked
            // as deletion, but there are no other polys that share the vertex.
            // In this case, the vertex should not be removed.
            if (numRemainingEdges <= 2)
            {
                return false;
            }

            // Find edges which share the removed vertex
            var maxEdges = numTouchedVerts * 2;
            var nedges = 0;
            var edges = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * maxEdges * 3, UnsafeUtility.AlignOf<int>());

            for (var i = 0; i < mesh->NPolys; ++i)
            {
                var p = &mesh->Polys[i * nvp * 2];
                var nv = CountPolyVerts(p, nvp);

                // Collect edges which touches the removed vertex
                for (int j = 0, k = nv - 1; j < nv; k = j++)
                {
                    if (p[j] == rem || p[k] == rem)
                    {
                        // Arrange edge so that a=rem
                        int a = p[j], b = p[k];
                        if (b == rem)
                        {
                            (a, b) = (b, a);
                        }

                        // Check if the edge exists
                        var exists = false;
                        for (var m = 0; m < nedges; ++m)
                        {
                            var e = &edges[m * 3];
                            if (e[1] == b)
                            {
                                // Exists, increment vertex share count
                                e[2]++;
                                exists = true;
                            }
                        }

                        // Add new edge
                        if (!exists)
                        {
                            var e = &edges[nedges * 3];
                            e[0] = a;
                            e[1] = b;
                            e[2] = 1;
                            nedges++;
                        }
                    }
                }
            }

            // There should be no more than 2 open edges
            // This catches the case that two non-adjacent polygons
            // share the removed vertex. In that case, do not remove the vertex.
            var numOpenEdges = 0;
            for (var i = 0; i < nedges; ++i)
            {
                if (edges[(i * 3) + 2] < 2)
                {
                    numOpenEdges++;
                }
            }

            if (numOpenEdges > 2)
            {
                return false;
            }

            return true;
        }

        private static bool RemoveVertex(RcPolyMesh* mesh, ushort rem, int maxTris)
        {
            var nvp = mesh->Nvp;

            // Count number of polygons to remove
            var numRemovedVerts = 0;
            for (var i = 0; i < mesh->NPolys; ++i)
            {
                var p = &mesh->Polys[i * nvp * 2];
                var nv = CountPolyVerts(p, nvp);
                for (var j = 0; j < nv; ++j)
                {
                    if (p[j] == rem)
                    {
                        numRemovedVerts++;
                    }
                }
            }

            var nedges = 0;
            var edges = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * numRemovedVerts * nvp * 4, UnsafeUtility.AlignOf<int>());

            var nhole = 0;
            var hole = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * numRemovedVerts * nvp, UnsafeUtility.AlignOf<int>());

            var nhreg = 0;
            var hreg = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * numRemovedVerts * nvp, UnsafeUtility.AlignOf<int>());

            var nharea = 0;
            var harea = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * numRemovedVerts * nvp, UnsafeUtility.AlignOf<int>());

            for (var i = 0; i < mesh->NPolys; ++i)
            {
                var p = &mesh->Polys[i * nvp * 2];
                var nv = CountPolyVerts(p, nvp);
                var hasRem = false;
                for (var j = 0; j < nv; ++j)
                {
                    if (p[j] == rem)
                    {
                        hasRem = true;
                    }
                }

                if (hasRem)
                {
                    // Collect edges which does not touch the removed vertex
                    for (int j = 0, k = nv - 1; j < nv; k = j++)
                    {
                        if (p[j] != rem && p[k] != rem)
                        {
                            var e = &edges[nedges * 4];
                            e[0] = p[k];
                            e[1] = p[j];
                            e[2] = mesh->Regs[i];
                            e[3] = mesh->Areas[i];
                            nedges++;
                        }
                    }

                    // Remove the polygon
                    var p2 = &mesh->Polys[(mesh->NPolys - 1) * nvp * 2];
                    if (p != p2)
                    {
                        UnsafeUtility.MemCpy(p, p2, sizeof(ushort) * nvp);
                    }

                    UnsafeUtility.MemSet(p + nvp, 0xff, sizeof(ushort) * nvp);
                    mesh->Regs[i] = mesh->Regs[mesh->NPolys - 1];
                    mesh->Areas[i] = mesh->Areas[mesh->NPolys - 1];
                    mesh->NPolys--;
                    --i;
                }
            }

            // Remove vertex
            for (int i = rem; i < mesh->NVerts - 1; ++i)
            {
                mesh->Verts[i] = mesh->Verts[i + 1];
            }

            mesh->NVerts--;

            // Adjust indices to match the removed vertex layout
            for (var i = 0; i < mesh->NPolys; ++i)
            {
                var p = &mesh->Polys[i * nvp * 2];
                var nv = CountPolyVerts(p, nvp);
                for (var j = 0; j < nv; ++j)
                {
                    if (p[j] > rem)
                    {
                        p[j]--;
                    }
                }
            }

            for (var i = 0; i < nedges; ++i)
            {
                if (edges[(i * 4) + 0] > rem)
                {
                    edges[(i * 4) + 0]--;
                }

                if (edges[(i * 4) + 1] > rem)
                {
                    edges[(i * 4) + 1]--;
                }
            }

            if (nedges == 0)
            {
                return true;
            }

            // Start with one vertex, keep appending connected
            // segments to the start and end of the hole
            PushBack(edges[0], hole, ref nhole);
            PushBack(edges[2], hreg, ref nhreg);
            PushBack(edges[3], harea, ref nharea);

            while (nedges > 0)
            {
                var match = false;

                for (var i = 0; i < nedges; ++i)
                {
                    var ea = edges[(i * 4) + 0];
                    var eb = edges[(i * 4) + 1];
                    var r = edges[(i * 4) + 2];
                    var a = edges[(i * 4) + 3];
                    var add = false;
                    if (hole[0] == eb)
                    {
                        // The segment matches the beginning of the hole boundary
                        PushFront(ea, hole, ref nhole);
                        PushFront(r, hreg, ref nhreg);
                        PushFront(a, harea, ref nharea);
                        add = true;
                    }
                    else if (hole[nhole - 1] == ea)
                    {
                        // The segment matches the end of the hole boundary
                        PushBack(eb, hole, ref nhole);
                        PushBack(r, hreg, ref nhreg);
                        PushBack(a, harea, ref nharea);
                        add = true;
                    }

                    if (add)
                    {
                        // The edge segment was added, remove it
                        edges[(i * 4) + 0] = edges[((nedges - 1) * 4) + 0];
                        edges[(i * 4) + 1] = edges[((nedges - 1) * 4) + 1];
                        edges[(i * 4) + 2] = edges[((nedges - 1) * 4) + 2];
                        edges[(i * 4) + 3] = edges[((nedges - 1) * 4) + 3];
                        --nedges;
                        match = true;
                        --i;
                    }
                }

                if (!match)
                {
                    break;
                }
            }

            var tris = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * nhole * 3, UnsafeUtility.AlignOf<int>());
            var tverts = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * nhole * 4, UnsafeUtility.AlignOf<int>());
            var thole = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * nhole, UnsafeUtility.AlignOf<int>());

            // Generate temp vertex array for triangulation
            for (var i = 0; i < nhole; ++i)
            {
                var pi = hole[i];
                var vert = mesh->Verts[pi];
                tverts[(i * 4) + 0] = vert.x;
                tverts[(i * 4) + 1] = vert.y;
                tverts[(i * 4) + 2] = vert.z;
                tverts[(i * 4) + 3] = 0;
                thole[i] = i;
            }

            // Triangulate the hole
            var ntris = Triangulate(nhole, tverts, thole, tris);
            if (ntris < 0)
            {
                ntris = -ntris;
            }

            // Merge the hole triangles back to polygons
            var polys = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * (ntris + 1) * nvp, UnsafeUtility.AlignOf<ushort>());
            var pregs = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * ntris, UnsafeUtility.AlignOf<ushort>());
            var pareas = (byte*)AllocatorManager.Allocate(Allocator.Temp, sizeof(byte) * ntris, UnsafeUtility.AlignOf<byte>());

            var tmpPoly = &polys[ntris * nvp];

            // Build initial polygons
            var npolys = 0;
            UnsafeUtility.MemSet(polys, 0xff, ntris * nvp * sizeof(ushort));
            for (var j = 0; j < ntris; ++j)
            {
                var t = &tris[j * 3];
                if (t[0] != t[1] && t[0] != t[2] && t[1] != t[2])
                {
                    polys[(npolys * nvp) + 0] = (ushort)hole[t[0]];
                    polys[(npolys * nvp) + 1] = (ushort)hole[t[1]];
                    polys[(npolys * nvp) + 2] = (ushort)hole[t[2]];

                    // If this polygon covers multiple region types then mark it as such
                    if (hreg[t[0]] != hreg[t[1]] || hreg[t[1]] != hreg[t[2]])
                    {
                        pregs[npolys] = RCMultipleRegs;
                    }
                    else
                    {
                        pregs[npolys] = (ushort)hreg[t[0]];
                    }

                    pareas[npolys] = (byte)harea[t[0]];
                    npolys++;
                }
            }

            if (npolys == 0)
            {
                return true;
            }

            // Merge polygons
            if (nvp > 3)
            {
                while (true)
                {
                    // Find best polygons to merge
                    var bestMergeVal = 0;
                    int bestPa = 0, bestPb = 0, bestEa = 0, bestEb = 0;

                    for (var j = 0; j < npolys - 1; ++j)
                    {
                        var pj = &polys[j * nvp];
                        for (var k = j + 1; k < npolys; ++k)
                        {
                            var pk = &polys[k * nvp];
                            int ea, eb;
                            var v = GetPolyMergeValue(pj, pk, mesh->Verts, out ea, out eb, nvp);
                            if (v > bestMergeVal)
                            {
                                bestMergeVal = v;
                                bestPa = j;
                                bestPb = k;
                                bestEa = ea;
                                bestEb = eb;
                            }
                        }
                    }

                    if (bestMergeVal > 0)
                    {
                        // Found best, merge
                        var pa = &polys[bestPa * nvp];
                        var pb = &polys[bestPb * nvp];
                        MergePolyVerts(pa, pb, bestEa, bestEb, tmpPoly, nvp);
                        if (pregs[bestPa] != pregs[bestPb])
                        {
                            pregs[bestPa] = RCMultipleRegs;
                        }

                        var last = &polys[(npolys - 1) * nvp];
                        if (pb != last)
                        {
                            UnsafeUtility.MemCpy(pb, last, sizeof(ushort) * nvp);
                        }

                        pregs[bestPb] = pregs[npolys - 1];
                        pareas[bestPb] = pareas[npolys - 1];
                        npolys--;
                    }
                    else
                    {
                        // Could not merge any polygons, stop
                        break;
                    }
                }
            }

            // Store polygons
            for (var i = 0; i < npolys; ++i)
            {
                if (mesh->NPolys >= maxTris)
                {
                    break;
                }

                var p = &mesh->Polys[mesh->NPolys * nvp * 2];
                UnsafeUtility.MemSet(p, 0xff, sizeof(ushort) * nvp * 2);
                for (var j = 0; j < nvp; ++j)
                {
                    p[j] = polys[(i * nvp) + j];
                }

                mesh->Regs[mesh->NPolys] = pregs[i];
                mesh->Areas[mesh->NPolys] = pareas[i];
                mesh->NPolys++;
                if (mesh->NPolys > maxTris)
                {
                    return false;
                }
            }

            return true;
        }

        private static void PushFront(int v, int* arr, ref int an)
        {
            an++;
            for (var i = an - 1; i > 0; --i)
            {
                arr[i] = arr[i - 1];
            }

            arr[0] = v;
        }

        private static void PushBack(int v, int* arr, ref int an)
        {
            arr[an] = v;
            an++;
        }

        private struct Edge
        {
            public fixed ushort Vert[2];
            public fixed ushort PolyEdge[2];
            public fixed ushort Poly[2];
        }
    }
}
