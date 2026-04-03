// <copyright file="Recast.MeshDetail.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Recast detailed mesh building functions for generating detailed triangle meshes from polygon meshes.
    /// </summary>
    public static unsafe partial class Recast
    {
        private const uint RCUnsetHeight = 0xffff;
        private const int EdgeUndefined = -1;
        private const int EdgeHull = -2;

        /// <summary>
        /// Builds a detailed mesh from the provided polygon mesh.
        /// </summary>
        /// <param name="mesh">A fully built polygon mesh.</param>
        /// <param name="compactHeightfield">The compact heightfield used to build the polygon mesh.</param>
        /// <param name="sampleDist">Sets the distance to use when sampling the heightfield [Limit: >=0] [Units: wu].</param>
        /// <param name="sampleMaxError">The maximum distance the detail mesh surface should deviate from heightfield data [Limit: >=0] [Units: wu].</param>
        /// <param name="detailMesh">The resulting detail mesh.</param>
        /// <returns>True if the operation completed successfully.</returns>
        public static void BuildPolyMeshDetail(
            RcPolyMesh* mesh, RcCompactHeightfield* compactHeightfield, float sampleDist, float sampleMaxError, RcPolyMeshDetail* detailMesh)
        {
            if (mesh->NVerts == 0 || mesh->NPolys == 0)
            {
                return;
            }

            var nvp = mesh->Nvp;
            var cs = mesh->CellSize;
            var ch = mesh->CellHeight;
            var orig = mesh->BMin;
            var borderSize = mesh->BorderSize;
            var heightSearchRadius = math.max(1, (int)math.ceil(mesh->MaxEdgeError));

            using var edges = new NativeList<int>(64, Allocator.Temp);
            using var tris = new NativeList<int>(512, Allocator.Temp);
            using var arr = new NativeList<int3>(512, Allocator.Temp);
            using var samples = new NativeList<int>(512, Allocator.Temp);
            var verts = stackalloc float[256 * 3];
            var hp = new HeightPatch();

            var nPolyVerts = 0;
            int maxhw = 0, maxhh = 0;

            var bounds = (int*)AllocatorManager.Allocate(Allocator.Temp, sizeof(int) * mesh->NPolys * 4, UnsafeUtility.AlignOf<int>());
            var poly = (float*)AllocatorManager.Allocate(Allocator.Temp, sizeof(float) * nvp * 3, UnsafeUtility.AlignOf<float>());

            // Find max size for a polygon area
            for (var i = 0; i < mesh->NPolys; ++i)
            {
                var p = &mesh->Polys[i * nvp * 2];
                var xmin = &bounds[(i * 4) + 0];
                var xmax = &bounds[(i * 4) + 1];
                var ymin = &bounds[(i * 4) + 2];
                var ymax = &bounds[(i * 4) + 3];
                *xmin = compactHeightfield->Width;
                *xmax = 0;
                *ymin = compactHeightfield->Height;
                *ymax = 0;
                for (var j = 0; j < nvp; ++j)
                {
                    if (p[j] == RCMeshNullIdx)
                    {
                        break;
                    }

                    var v = mesh->Verts[p[j]];
                    *xmin = math.min(*xmin, v.x);
                    *xmax = math.max(*xmax, v.x);
                    *ymin = math.min(*ymin, v.z);
                    *ymax = math.max(*ymax, v.z);
                    nPolyVerts++;
                }

                *xmin = math.max(0, *xmin - 1);
                *xmax = math.min(compactHeightfield->Width, *xmax + 1);
                *ymin = math.max(0, *ymin - 1);
                *ymax = math.min(compactHeightfield->Height, *ymax + 1);
                if (*xmin >= *xmax || *ymin >= *ymax)
                {
                    continue;
                }

                maxhw = math.max(maxhw, *xmax - *xmin);
                maxhh = math.max(maxhh, *ymax - *ymin);
            }

            hp.Data = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * maxhw * maxhh, UnsafeUtility.AlignOf<ushort>());

            detailMesh->NMeshes = mesh->NPolys;
            detailMesh->NVerts = 0;
            detailMesh->NTris = 0;
            detailMesh->Meshes = (uint4*)AllocatorManager.Allocate(detailMesh->Allocator, sizeof(uint4) * detailMesh->NMeshes, UnsafeUtility.AlignOf<uint4>());

            var vcap = nPolyVerts + (nPolyVerts / 2);
            var tcap = vcap * 2;

            detailMesh->NVerts = 0;
            detailMesh->Verts = (float3*)AllocatorManager.Allocate(detailMesh->Allocator, sizeof(float3) * vcap, UnsafeUtility.AlignOf<float3>());

            detailMesh->NTris = 0;
            detailMesh->Tris = (byte4*)AllocatorManager.Allocate(detailMesh->Allocator, sizeof(byte4) * tcap, UnsafeUtility.AlignOf<byte4>());

            for (var i = 0; i < mesh->NPolys; ++i)
            {
                var p = &mesh->Polys[i * nvp * 2];

                // Store polygon vertices for processing
                var npoly = 0;
                for (var j = 0; j < nvp; ++j)
                {
                    if (p[j] == RCMeshNullIdx)
                    {
                        break;
                    }

                    var v = mesh->Verts[p[j]];
                    poly[(j * 3) + 0] = v.x * cs;
                    poly[(j * 3) + 1] = v.y * ch;
                    poly[(j * 3) + 2] = v.z * cs;
                    npoly++;
                }

                // Get the height data from the area of the polygon
                hp.Xmin = bounds[(i * 4) + 0];
                hp.Ymin = bounds[(i * 4) + 2];
                hp.Width = bounds[(i * 4) + 1] - bounds[(i * 4) + 0];
                hp.Height = bounds[(i * 4) + 3] - bounds[(i * 4) + 2];
                GetHeightData(compactHeightfield, p, npoly, mesh->Verts, borderSize, hp, arr, mesh->Regs[i]);

                // Build detail mesh
                BuildPolyDetail(poly, npoly, sampleDist, sampleMaxError, heightSearchRadius, compactHeightfield, hp, verts, out var nverts, tris, edges, samples);

                // Move detail verts to world space
                for (var j = 0; j < nverts; ++j)
                {
                    verts[(j * 3) + 0] += orig.x;
                    verts[(j * 3) + 1] += orig.y + compactHeightfield->CellHeight; // Is this offset necessary?
                    verts[(j * 3) + 2] += orig.z;
                }

                // Offset poly too, will be used to flag checking
                for (var j = 0; j < npoly; ++j)
                {
                    poly[(j * 3) + 0] += orig.x;
                    poly[(j * 3) + 1] += orig.y;
                    poly[(j * 3) + 2] += orig.z;
                }

                // Store detail submesh
                var ntris = tris.Length / 4;

                detailMesh->Meshes[i] = new uint4(
                    (uint)detailMesh->NVerts,
                    (uint)nverts,
                    (uint)detailMesh->NTris,
                    (uint)ntris);

                // Store vertices, allocate more memory if necessary
                if (detailMesh->NVerts + nverts > vcap)
                {
                    while (detailMesh->NVerts + nverts > vcap)
                    {
                        vcap += 256;
                    }

                    var newv = (float3*)AllocatorManager.Allocate(detailMesh->Allocator, sizeof(float3) * vcap, UnsafeUtility.AlignOf<float3>());

                    if (detailMesh->NVerts > 0)
                    {
                        UnsafeUtility.MemCpy(newv, detailMesh->Verts, sizeof(float3) * detailMesh->NVerts);
                    }

                    AllocatorManager.Free(detailMesh->Allocator, detailMesh->Verts);
                    detailMesh->Verts = newv;
                }

                for (var j = 0; j < nverts; ++j)
                {
                    detailMesh->Verts[detailMesh->NVerts] = new float3(
                        verts[(j * 3) + 0],
                        verts[(j * 3) + 1],
                        verts[(j * 3) + 2]);
                    detailMesh->NVerts++;
                }

                // Store triangles, allocate more memory if necessary
                if (detailMesh->NTris + ntris > tcap)
                {
                    while (detailMesh->NTris + ntris > tcap)
                    {
                        tcap += 256;
                    }

                    var newt = (byte4*)AllocatorManager.Allocate(detailMesh->Allocator, sizeof(byte4) * tcap, UnsafeUtility.AlignOf<byte4>());

                    if (detailMesh->NTris > 0)
                    {
                        UnsafeUtility.MemCpy(newt, detailMesh->Tris, sizeof(byte4) * detailMesh->NTris);
                    }

                    AllocatorManager.Free(detailMesh->Allocator, detailMesh->Tris);
                    detailMesh->Tris = newt;
                }

                for (var j = 0; j < ntris; ++j)
                {
                    var t = tris.GetUnsafePtr() + (j * 4);
                    detailMesh->Tris[detailMesh->NTris] = new byte4((byte)t[0], (byte)t[1], (byte)t[2], (byte)t[3]);
                    detailMesh->NTris++;
                }
            }
        }

        /// <summary>
        /// Merges detail meshes into a single mesh.
        /// </summary>
        /// <param name="meshes">An array of detail meshes to merge [Size: nmeshes].</param>
        /// <param name="nmeshes">The number of detail meshes in the meshes array.</param>
        /// <param name="mesh">The resulting merged detail mesh.</param>
        /// <returns>True if the operation completed successfully.</returns>
        public static bool MergePolyMeshDetails(RcPolyMeshDetail** meshes, int nmeshes, RcPolyMeshDetail* mesh)
        {
            var maxVerts = 0;
            var maxTris = 0;
            var maxMeshes = 0;

            for (var i = 0; i < nmeshes; ++i)
            {
                if (meshes[i] == null)
                {
                    continue;
                }

                maxVerts += meshes[i]->NVerts;
                maxTris += meshes[i]->NTris;
                maxMeshes += meshes[i]->NMeshes;
            }

            mesh->NMeshes = 0;
            mesh->Meshes = (uint4*)AllocatorManager.Allocate(mesh->Allocator, sizeof(uint4) * maxMeshes, UnsafeUtility.AlignOf<uint4>());

            mesh->NTris = 0;
            mesh->Tris = (byte4*)AllocatorManager.Allocate(mesh->Allocator, sizeof(byte4) * maxTris, UnsafeUtility.AlignOf<byte4>());

            mesh->NVerts = 0;
            mesh->Verts = (float3*)AllocatorManager.Allocate(mesh->Allocator, sizeof(float3) * maxVerts, UnsafeUtility.AlignOf<float3>());

            // Merge datas
            for (var i = 0; i < nmeshes; ++i)
            {
                var dm = meshes[i];
                if (dm == null)
                {
                    continue;
                }

                for (var j = 0; j < dm->NMeshes; ++j)
                {
                    var src = dm->Meshes[j];
                    mesh->Meshes[mesh->NMeshes] = new uint4(
                        (uint)mesh->NVerts + src.x,
                        src.y,
                        (uint)mesh->NTris + src.z,
                        src.w);
                    mesh->NMeshes++;
                }

                for (var k = 0; k < dm->NVerts; ++k)
                {
                    mesh->Verts[mesh->NVerts] = dm->Verts[k];
                    mesh->NVerts++;
                }

                for (var k = 0; k < dm->NTris; ++k)
                {
                    mesh->Tris[mesh->NTris] = dm->Tris[k];
                    mesh->NTris++;
                }
            }

            return true;
        }

        private static float VDot2(float* a, float* b)
        {
            return (a[0] * b[0]) + (a[2] * b[2]);
        }

        private static float VDistSq2(float* p, float* q)
        {
            var dx = q[0] - p[0];
            var dy = q[2] - p[2];
            return (dx * dx) + (dy * dy);
        }

        private static float VDist2(float* p, float* q)
        {
            return math.sqrt(VDistSq2(p, q));
        }

        private static float VCross2(float* p1, float* p2, float* p3)
        {
            var u1 = p2[0] - p1[0];
            var v1 = p2[2] - p1[2];
            var u2 = p3[0] - p1[0];
            var v2 = p3[2] - p1[2];
            return (u1 * v2) - (v1 * u2);
        }

        private static bool CircumCircle(float* p1, float* p2, float* p3, float* c, out float r)
        {
            const float eps = 1e-6f;

            // Calculate the circle relative to p1, to avoid some precision issues
            var v1 = new float3(0, 0, 0);
            float3 v2, v3;
            v2.x = p2[0] - p1[0];
            v2.y = p2[1] - p1[1];
            v2.z = p2[2] - p1[2];
            v3.x = p3[0] - p1[0];
            v3.y = p3[1] - p1[1];
            v3.z = p3[2] - p1[2];

            var cp = VCross2((float*)&v1, (float*)&v2, (float*)&v3);
            if (math.abs(cp) > eps)
            {
                var v1Sq = VDot2((float*)&v1, (float*)&v1);
                var v2Sq = VDot2((float*)&v2, (float*)&v2);
                var v3Sq = VDot2((float*)&v3, (float*)&v3);
                c[0] = ((v1Sq * (v2.z - v3.z)) + (v2Sq * (v3.z - v1.z)) + (v3Sq * (v1.z - v2.z))) / (2 * cp);
                c[1] = 0;
                c[2] = ((v1Sq * (v3.x - v2.x)) + (v2Sq * (v1.x - v3.x)) + (v3Sq * (v2.x - v1.x))) / (2 * cp);
                r = VDist2(c, (float*)&v1);
                c[0] += p1[0];
                c[1] += p1[1];
                c[2] += p1[2];
                return true;
            }

            c[0] = p1[0];
            c[1] = p1[1];
            c[2] = p1[2];
            r = 0;
            return false;
        }

        private static float DistPtTri(float* p, float* a, float* b, float* c)
        {
            float3 v0, v1, v2;
            v0.x = c[0] - a[0];
            v0.y = c[1] - a[1];
            v0.z = c[2] - a[2];
            v1.x = b[0] - a[0];
            v1.y = b[1] - a[1];
            v1.z = b[2] - a[2];
            v2.x = p[0] - a[0];
            v2.y = p[1] - a[1];
            v2.z = p[2] - a[2];

            var dot00 = VDot2((float*)&v0, (float*)&v0);
            var dot01 = VDot2((float*)&v0, (float*)&v1);
            var dot02 = VDot2((float*)&v0, (float*)&v2);
            var dot11 = VDot2((float*)&v1, (float*)&v1);
            var dot12 = VDot2((float*)&v1, (float*)&v2);

            // Compute barycentric coordinates
            var invDenom = 1.0f / ((dot00 * dot11) - (dot01 * dot01));
            var u = ((dot11 * dot02) - (dot01 * dot12)) * invDenom;
            var v = ((dot00 * dot12) - (dot01 * dot02)) * invDenom;

            // If point lies inside the triangle, return interpolated y-coord
            const float eps = 1e-4f;
            if (u >= -eps && v >= -eps && u + v <= 1 + eps)
            {
                var y = a[1] + (v0.y * u) + (v1.y * v);
                return math.abs(y - p[1]);
            }

            return float.MaxValue;
        }

        private static float DistancePtSeg(float* pt, float* p, float* q)
        {
            var pqx = q[0] - p[0];
            var pqy = q[1] - p[1];
            var pqz = q[2] - p[2];
            var dx = pt[0] - p[0];
            var dy = pt[1] - p[1];
            var dz = pt[2] - p[2];
            var d = (pqx * pqx) + (pqy * pqy) + (pqz * pqz);
            var t = (pqx * dx) + (pqy * dy) + (pqz * dz);
            if (d > 0)
            {
                t /= d;
            }

            if (t < 0)
            {
                t = 0;
            }
            else if (t > 1)
            {
                t = 1;
            }

            dx = p[0] + (t * pqx) - pt[0];
            dy = p[1] + (t * pqy) - pt[1];
            dz = p[2] + (t * pqz) - pt[2];

            return (dx * dx) + (dy * dy) + (dz * dz);
        }

        private static float DistancePtSeg2d(float* pt, float* p, float* q)
        {
            var pqx = q[0] - p[0];
            var pqz = q[2] - p[2];
            var dx = pt[0] - p[0];
            var dz = pt[2] - p[2];
            var d = (pqx * pqx) + (pqz * pqz);
            var t = (pqx * dx) + (pqz * dz);
            if (d > 0)
            {
                t /= d;
            }

            if (t < 0)
            {
                t = 0;
            }
            else if (t > 1)
            {
                t = 1;
            }

            dx = p[0] + (t * pqx) - pt[0];
            dz = p[2] + (t * pqz) - pt[2];

            return (dx * dx) + (dz * dz);
        }

        private static float DistToTriMesh(float* p, float* verts, int nverts, int* tris, int ntris)
        {
            var dmin = float.MaxValue;
            for (var i = 0; i < ntris; ++i)
            {
                var va = &verts[tris[(i * 4) + 0] * 3];
                var vb = &verts[tris[(i * 4) + 1] * 3];
                var vc = &verts[tris[(i * 4) + 2] * 3];
                var d = DistPtTri(p, va, vb, vc);
                if (d < dmin)
                {
                    dmin = d;
                }
            }

            if (dmin == float.MaxValue)
            {
                return -1;
            }

            return dmin;
        }

        private static float DistToPoly(int nvert, float* verts, float* p)
        {
            var dmin = float.MaxValue;
            int i, j, c = 0;
            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                var vi = &verts[i * 3];
                var vj = &verts[j * 3];
                if ((vi[2] > p[2]) != (vj[2] > p[2]) && p[0] < ((vj[0] - vi[0]) * (p[2] - vi[2]) / (vj[2] - vi[2])) + vi[0])
                {
                    c = c == 0 ? 1 : 0;
                }

                dmin = math.min(dmin, DistancePtSeg2d(p, vj, vi));
            }

            return c != 0 ? -dmin : dmin;
        }

        private static ushort GetHeight(float fx, float fy, float fz, float cs, float ics, float ch, int radius, HeightPatch hp)
        {
            var ix = (int)math.floor((fx * ics) + 0.01f);
            var iz = (int)math.floor((fz * ics) + 0.01f);
            ix = math.clamp(ix - hp.Xmin, 0, hp.Width - 1);
            iz = math.clamp(iz - hp.Ymin, 0, hp.Height - 1);
            var h = hp.Data[ix + (iz * hp.Width)];
            if (h == RCUnsetHeight)
            {
                // Special case when data might be bad
                // Walk adjacent cells in a spiral up to 'radius', and look
                // for a pixel which has a valid height
                int x = 1, z = 0, dx = 1, dz = 0;
                var maxSize = (radius * 2) + 1;
                var maxIter = (maxSize * maxSize) - 1;

                var nextRingIterStart = 8;
                var nextRingIters = 16;

                var dmin = float.MaxValue;
                for (var i = 0; i < maxIter; i++)
                {
                    var nx = ix + x;
                    var nz = iz + z;

                    if (nx >= 0 && nz >= 0 && nx < hp.Width && nz < hp.Height)
                    {
                        var nh = hp.Data[nx + (nz * hp.Width)];
                        if (nh != RCUnsetHeight)
                        {
                            var d = math.abs((nh * ch) - fy);
                            if (d < dmin)
                            {
                                h = nh;
                                dmin = d;
                            }
                        }
                    }

                    // We are searching in a grid which looks approximately like this:
                    //  __________
                    // |2 ______ 2|
                    // | |1 __ 1| |
                    // | | |__| | |
                    // | |______| |
                    // |__________|
                    // We want to find the best height as close to the center cell as possible. This means that
                    // if we find a height in one of the neighbor cells to the center, we don't want to
                    // expand further out than the 8 neighbors - we want to limit our search to the closest
                    // of these "rings", but the best height in the ring.
                    // For example, the center is just 1 cell. We checked that at the entrance to the function.
                    // The next "ring" contains 8 cells (marked 1 above). Those are all the neighbors to the center cell.
                    // The next one again contains 16 cells (marked 2). In general each ring has 8 additional cells, which
                    // can be thought of as adding 2 cells around the "center" of each side when we expand the ring.
                    // Here we detect if we are about to enter the next ring, and if we are and we have found
                    // a height, we abort the search.
                    if (i + 1 == nextRingIterStart)
                    {
                        if (h != RCUnsetHeight)
                        {
                            break;
                        }

                        nextRingIterStart += nextRingIters;
                        nextRingIters += 8;
                    }

                    if (x == z || (x < 0 && x == -z) || (x > 0 && x == 1 - z))
                    {
                        var tmp = dx;
                        dx = -dz;
                        dz = tmp;
                    }

                    x += dx;
                    z += dz;
                }
            }

            return h;
        }

        private static void SeedArrayWithPolyCenter(
            RcCompactHeightfield* chf, ushort* poly, int npoly, ushort3* verts, int bs, HeightPatch hp, NativeList<int3> array)
        {
            // Note: Reads to the compact heightfield are offset by border size (bs)
            // since border size offset is already removed from the polymesh vertices
            var offset = stackalloc int[9 * 2] { 0, 0, -1, -1, 0, -1, 1, -1, 1, 0, 1, 1, 0, 1, -1, 1, -1, 0 };

            // Find cell closest to a poly vertex
            int startCellX = 0, startCellY = 0, startSpanIndex = -1;
            var dmin = (int)RCUnsetHeight;
            for (var j = 0; j < npoly && dmin > 0; ++j)
            {
                for (var k = 0; k < 9 && dmin > 0; ++k)
                {
                    var v = verts[poly[j]];
                    var ax = v.x + offset[(k * 2) + 0];
                    int ay = v.y;
                    var az = v.z + offset[(k * 2) + 1];
                    if (ax < hp.Xmin || ax >= hp.Xmin + hp.Width || az < hp.Ymin || az >= hp.Ymin + hp.Height)
                    {
                        continue;
                    }

                    var c = chf->Cells[ax + bs + ((az + bs) * chf->Width)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni && dmin > 0; ++i)
                    {
                        var s = chf->Spans[i];
                        var d = math.abs(ay - s.Y);
                        if (d < dmin)
                        {
                            startCellX = ax;
                            startCellY = az;
                            startSpanIndex = i;
                            dmin = d;
                        }
                    }
                }
            }

            // Find center of the polygon
            int pcx = 0, pcy = 0;
            for (var j = 0; j < npoly; ++j)
            {
                var v = verts[poly[j]];
                pcx += v.x;
                pcy += v.z;
            }

            pcx /= npoly;
            pcy /= npoly;

            // Use seeds array as a stack for DFS
            array.Clear();
            array.Add(new int3(startCellX, startCellY, startSpanIndex));

            var dirs = stackalloc int[4] { 0, 1, 2, 3 };
            UnsafeUtility.MemSet(hp.Data, 0, sizeof(ushort) * hp.Width * hp.Height);

            // DFS to move to the center. Note that we need a DFS here and can not just move
            // directly towards the center without recording intermediate nodes, even though the polygons
            // are convex. In very rare we can get stuck due to contour simplification if we do not
            // record nodes.
            int cx = -1, cy = -1, ci = -1;
            while (true)
            {
                if (array.Length < 1)
                {
                    break;
                }

                var c = array[array.Length - 1];
                array.RemoveAt(array.Length - 1);

                cx = c.x;  // x coordinate
                cy = c.y;  // y coordinate
                ci = c.z;  // span index

                if (cx == pcx && cy == pcy)
                {
                    break;
                }

                // If we are already at the correct X-position, prefer direction
                // directly towards the center in the Y-axis; otherwise prefer
                // direction in the X-axis
                int directDir;
                if (cx == pcx)
                {
                    directDir = GetDirForOffset(0, pcy > cy ? 1 : -1);
                }
                else
                {
                    directDir = GetDirForOffset(pcx > cx ? 1 : -1, 0);
                }

                // Push the direct dir last so we start with this on next iteration
                (dirs[directDir], dirs[3]) = (dirs[3], dirs[directDir]);

                var cs = chf->Spans[ci];
                for (var i = 0; i < 4; i++)
                {
                    var dir = dirs[i];
                    if (GetCon(cs, dir) == RCNotConnected)
                    {
                        continue;
                    }

                    var newX = cx + GetDirOffsetX(dir);
                    var newY = cy + GetDirOffsetY(dir);

                    var hpx = newX - hp.Xmin;
                    var hpy = newY - hp.Ymin;
                    if (hpx < 0 || hpx >= hp.Width || hpy < 0 || hpy >= hp.Height)
                    {
                        continue;
                    }

                    if (hp.Data[hpx + (hpy * hp.Width)] != 0)
                    {
                        continue;
                    }

                    hp.Data[hpx + (hpy * hp.Width)] = 1;
                    array.Add(new int3(newX, newY, (int)chf->Cells[newX + bs + ((newY + bs) * chf->Width)].Index + GetCon(cs, dir)));
                }

                (dirs[directDir], dirs[3]) = (dirs[3], dirs[directDir]);
            }

            array.Clear();

            // getHeightData seeds are given in coordinates with borders
            array.Add(new int3(cx + bs, cy + bs, ci));

            UnsafeUtility.MemSet(hp.Data, 0xff, sizeof(ushort) * hp.Width * hp.Height);
            var cs2 = chf->Spans[ci];
            hp.Data[cx - hp.Xmin + ((cy - hp.Ymin) * hp.Width)] = cs2.Y;
        }

        private static void GetHeightData(
            RcCompactHeightfield* chf, ushort* poly, int npoly, ushort3* verts, int bs, HeightPatch hp, NativeList<int3> queue, int region)
        {
            // Note: Reads to the compact heightfield are offset by border size (bs)
            // since border size offset is already removed from the polymesh vertices
            queue.Clear();

            // Set all heights to RC_UNSET_HEIGHT
            UnsafeUtility.MemSet(hp.Data, 0xff, sizeof(ushort) * hp.Width * hp.Height);

            var empty = true;

            // We cannot sample from this poly if it was created from polys
            // of different regions. If it was then it could potentially be overlapping
            // with polys of that region and the heights sampled here could be wrong.
            if (region != RCMultipleRegs)
            {
                // Copy the height from the same region, and mark region borders
                // as seed points to fill the rest
                for (var hy = 0; hy < hp.Height; hy++)
                {
                    var y = hp.Ymin + hy + bs;
                    for (var hx = 0; hx < hp.Width; hx++)
                    {
                        var x = hp.Xmin + hx + bs;
                        var c = chf->Cells[x + (y * chf->Width)];
                        for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                        {
                            var s = chf->Spans[i];
                            if (s.Reg == region)
                            {
                                // Store height
                                hp.Data[hx + (hy * hp.Width)] = s.Y;
                                empty = false;

                                // If any of the neighbours is not in same region,
                                // add the current location as flood fill start
                                var border = false;
                                for (var dir = 0; dir < 4; ++dir)
                                {
                                    if (GetCon(s, dir) != RCNotConnected)
                                    {
                                        var ax = x + GetDirOffsetX(dir);
                                        var ay = y + GetDirOffsetY(dir);
                                        var ai = (int)chf->Cells[ax + (ay * chf->Width)].Index + GetCon(s, dir);
                                        var aSpan = chf->Spans[ai];
                                        if (aSpan.Reg != region)
                                        {
                                            border = true;
                                            break;
                                        }
                                    }
                                }

                                if (border)
                                {
                                    queue.Add(new int3(x, y, i));
                                }

                                break;
                            }
                        }
                    }
                }
            }

            // if the polygon does not contain any points from the current region (rare, but happens)
            // or if it could potentially be overlapping polygons of the same region,
            // then use the center as the seed point.
            if (empty)
            {
                SeedArrayWithPolyCenter(chf, poly, npoly, verts, bs, hp, queue);
            }

            // const int RETRACT_SIZE = 256;
            var head = 0;

            // We assume the seed is centered in the polygon, so a BFS to collect
            // height data will ensure we do not move onto overlapping polygons and
            // sample wrong heights.
            while (head < queue.Length)
            {
                var c = queue[head];
                var cx = c.x;
                var cy = c.y;
                var ci = c.z;
                head++;

                var cs = chf->Spans[ci];
                for (var dir = 0; dir < 4; ++dir)
                {
                    if (GetCon(cs, dir) == RCNotConnected)
                    {
                        continue;
                    }

                    var ax = cx + GetDirOffsetX(dir);
                    var ay = cy + GetDirOffsetY(dir);
                    var hx = ax - hp.Xmin - bs;
                    var hy = ay - hp.Ymin - bs;

                    if ((uint)hx >= (uint)hp.Width || (uint)hy >= (uint)hp.Height)
                    {
                        continue;
                    }

                    if (hp.Data[hx + (hy * hp.Width)] != RCUnsetHeight)
                    {
                        continue;
                    }

                    var ai = (int)chf->Cells[ax + (ay * chf->Width)].Index + GetCon(cs, dir);
                    var aSpan = chf->Spans[ai];

                    hp.Data[hx + (hy * hp.Width)] = aSpan.Y;

                    queue.Add(new int3(ax, ay, ai));
                }
            }
        }

        private static int FindEdge(int* edges, int nedges, int s, int t)
        {
            for (var i = 0; i < nedges; ++i)
            {
                var edge = &edges[i * 4];
                if ((edge[0] == s && edge[1] == t) || (edge[0] == t && edge[1] == s))
                {
                    return i;
                }
            }

            return EdgeUndefined;
        }

        private static int AddEdge(int* edges, ref int nedges, int maxEdges, int s, int t, int leftFace, int rightFace)
        {
            if (nedges >= maxEdges)
            {
                return EdgeUndefined;
            }

            var edgeIndex = FindEdge(edges, nedges, s, t);
            if (edgeIndex != EdgeUndefined)
            {
                return EdgeUndefined;
            }

            var edge = &edges[nedges * 4];
            edge[0] = s;
            edge[1] = t;
            edge[2] = leftFace;
            edge[3] = rightFace;
            return nedges++;
        }

        private static void UpdateLeftFace(int* edge, int s, int t, int face)
        {
            if (edge[0] == s && edge[1] == t && edge[2] == EdgeUndefined)
            {
                edge[2] = face;
            }
            else if (edge[1] == s && edge[0] == t && edge[3] == EdgeUndefined)
            {
                edge[3] = face;
            }
        }

        private static int OverlapSegSeg2d(float* a, float* b, float* c, float* d)
        {
            var a1 = VCross2(a, b, d);
            var a2 = VCross2(a, b, c);
            if (a1 * a2 < 0.0f)
            {
                var a3 = VCross2(c, d, a);
                var a4 = a3 + a2 - a1;
                if (a3 * a4 < 0.0f)
                {
                    return 1;
                }
            }

            return 0;
        }

        private static bool OverlapEdges(float* pts, int* edges, int nedges, int s1, int t1)
        {
            for (var i = 0; i < nedges; ++i)
            {
                var s0 = edges[(i * 4) + 0];
                var t0 = edges[(i * 4) + 1];

                // Same or connected edges do not overlap.
                if (s0 == s1 || s0 == t1 || t0 == s1 || t0 == t1)
                {
                    continue;
                }

                if (OverlapSegSeg2d(&pts[s0 * 3], &pts[t0 * 3], &pts[s1 * 3], &pts[t1 * 3]) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CompleteFacet(float* pts, int npts, int* edges, ref int nedges, int maxEdges, ref int nfaces, int edgeIndex)
        {
            const float eps = 1e-5f;

            var edge = &edges[edgeIndex * 4];
            int s;
            int t;
            if (edge[2] == EdgeUndefined)
            {
                s = edge[0];
                t = edge[1];
            }
            else if (edge[3] == EdgeUndefined)
            {
                s = edge[1];
                t = edge[0];
            }
            else
            {
                return;
            }

            var pt = npts;
            var c = stackalloc float[3];
            float r = -1;

            for (var u = 0; u < npts; ++u)
            {
                if (u == s || u == t)
                {
                    continue;
                }

                if (VCross2(&pts[s * 3], &pts[t * 3], &pts[u * 3]) <= eps)
                {
                    continue;
                }

                if (r < 0)
                {
                    pt = u;
                    CircumCircle(&pts[s * 3], &pts[t * 3], &pts[u * 3], c, out r);
                    continue;
                }

                var d = VDist2(c, &pts[u * 3]);
                const float tol = 0.001f;
                if (d > r * (1 + tol))
                {
                    continue;
                }

                if (d < r * (1 - tol))
                {
                    pt = u;
                    CircumCircle(&pts[s * 3], &pts[t * 3], &pts[u * 3], c, out r);
                    continue;
                }

                // Inside epsilon circum circle, do extra tests to make sure the edge is valid.
                if (OverlapEdges(pts, edges, nedges, s, u) || OverlapEdges(pts, edges, nedges, t, u))
                {
                    continue;
                }

                pt = u;
                CircumCircle(&pts[s * 3], &pts[t * 3], &pts[u * 3], c, out r);
            }

            if (pt < npts)
            {
                UpdateLeftFace(&edges[edgeIndex * 4], s, t, nfaces);

                edgeIndex = FindEdge(edges, nedges, pt, s);
                if (edgeIndex == EdgeUndefined)
                {
                    AddEdge(edges, ref nedges, maxEdges, pt, s, nfaces, EdgeUndefined);
                }
                else
                {
                    UpdateLeftFace(&edges[edgeIndex * 4], pt, s, nfaces);
                }

                edgeIndex = FindEdge(edges, nedges, t, pt);
                if (edgeIndex == EdgeUndefined)
                {
                    AddEdge(edges, ref nedges, maxEdges, t, pt, nfaces, EdgeUndefined);
                }
                else
                {
                    UpdateLeftFace(&edges[edgeIndex * 4], t, pt, nfaces);
                }

                nfaces++;
            }
            else
            {
                UpdateLeftFace(&edges[edgeIndex * 4], s, t, EdgeHull);
            }
        }

        private static void DelaunayHull(int npts, float* pts, int nhull, int* hull, NativeList<int> tris, NativeList<int> edges)
        {
            var nfaces = 0;
            var nedges = 0;
            var maxEdges = npts * 10;

            edges.Resize(maxEdges * 4, NativeArrayOptions.ClearMemory);
            var edgeData = edges.GetUnsafePtr();

            for (int i = 0, j = nhull - 1; i < nhull; j = i++)
            {
                AddEdge(edgeData, ref nedges, maxEdges, hull[j], hull[i], EdgeHull, EdgeUndefined);
            }

            var currentEdge = 0;
            while (currentEdge < nedges)
            {
                if (edgeData[(currentEdge * 4) + 2] == EdgeUndefined)
                {
                    CompleteFacet(pts, npts, edgeData, ref nedges, maxEdges, ref nfaces, currentEdge);
                }

                if (edgeData[(currentEdge * 4) + 3] == EdgeUndefined)
                {
                    CompleteFacet(pts, npts, edgeData, ref nedges, maxEdges, ref nfaces, currentEdge);
                }

                currentEdge++;
            }

            tris.Resize(nfaces * 4, NativeArrayOptions.ClearMemory);
            for (var i = 0; i < tris.Length; ++i)
            {
                tris[i] = -1;
            }

            for (var i = 0; i < nedges; ++i)
            {
                var current = &edgeData[i * 4];
                if (current[3] >= 0)
                {
                    var triangle = current[3] * 4;
                    if (tris[triangle + 0] == -1)
                    {
                        tris[triangle + 0] = current[0];
                        tris[triangle + 1] = current[1];
                    }
                    else if (tris[triangle + 0] == current[1])
                    {
                        tris[triangle + 2] = current[0];
                    }
                    else if (tris[triangle + 1] == current[0])
                    {
                        tris[triangle + 2] = current[1];
                    }
                }

                if (current[2] >= 0)
                {
                    var triangle = current[2] * 4;
                    if (tris[triangle + 0] == -1)
                    {
                        tris[triangle + 0] = current[1];
                        tris[triangle + 1] = current[0];
                    }
                    else if (tris[triangle + 0] == current[0])
                    {
                        tris[triangle + 2] = current[1];
                    }
                    else if (tris[triangle + 1] == current[1])
                    {
                        tris[triangle + 2] = current[0];
                    }
                }
            }

            for (var i = 0; i < tris.Length / 4; ++i)
            {
                var triangle = i * 4;
                if (tris[triangle + 0] == -1 || tris[triangle + 1] == -1 || tris[triangle + 2] == -1)
                {
                    var last = tris.Length - 4;
                    tris[triangle + 0] = tris[last + 0];
                    tris[triangle + 1] = tris[last + 1];
                    tris[triangle + 2] = tris[last + 2];
                    tris[triangle + 3] = tris[last + 3];
                    tris.Resize(last, NativeArrayOptions.ClearMemory);
                    --i;
                }
            }
        }

        // Calculate minimum extend of the polygon
        private static float PolyMinExtent(float* verts, int nverts)
        {
            var minDist = float.MaxValue;
            for (var i = 0; i < nverts; i++)
            {
                var ni = (i + 1) % nverts;
                var p1 = &verts[i * 3];
                var p2 = &verts[ni * 3];
                float maxEdgeDist = 0;
                for (var j = 0; j < nverts; j++)
                {
                    if (j == i || j == ni)
                    {
                        continue;
                    }

                    var d = DistancePtSeg2d(&verts[j * 3], p1, p2);
                    maxEdgeDist = math.max(maxEdgeDist, d);
                }

                minDist = math.min(minDist, maxEdgeDist);
            }

            return math.sqrt(minDist);
        }

        private static void TriangulateHull(int nverts, float* verts, int nhull, int* hull, int nin, NativeList<int> tris)
        {
            int start = 0, left = 1, right = nhull - 1;

            // Start from an ear with shortest perimeter
            // This tends to favor well formed triangles as starting point
            var dmin = float.MaxValue;
            for (var i = 0; i < nhull; i++)
            {
                if (hull[i] >= nin)
                {
                    continue; // Ears are triangles with original vertices as middle vertex while others are actually line segments on edges
                }

                var pi = Prev(i, nhull);
                var ni = Next(i, nhull);
                var pv = &verts[hull[pi] * 3];
                var cv = &verts[hull[i] * 3];
                var nv = &verts[hull[ni] * 3];
                var d = VDist2(pv, cv) + VDist2(cv, nv) + VDist2(nv, pv);
                if (d < dmin)
                {
                    start = i;
                    left = ni;
                    right = pi;
                    dmin = d;
                }
            }

            // Add first triangle
            tris.Add(hull[start]);
            tris.Add(hull[left]);
            tris.Add(hull[right]);
            tris.Add(0);

            // Triangulate the polygon by moving left or right,
            // depending on which triangle has shorter perimeter.
            // This heuristic was chose empirically, since it seems
            // handle tessellated straight edges well.
            while (Next(left, nhull) != right)
            {
                // Check to see if se should advance left or right
                var nleft = Next(left, nhull);
                var nright = Prev(right, nhull);

                var cvleft = &verts[hull[left] * 3];
                var nvleft = &verts[hull[nleft] * 3];
                var cvright = &verts[hull[right] * 3];
                var nvright = &verts[hull[nright] * 3];
                var dleft = VDist2(cvleft, nvleft) + VDist2(nvleft, cvright);
                var dright = VDist2(cvright, nvright) + VDist2(cvleft, nvright);

                if (dleft < dright)
                {
                    tris.Add(hull[left]);
                    tris.Add(hull[nleft]);
                    tris.Add(hull[right]);
                    tris.Add(0);
                    left = nleft;
                }
                else
                {
                    tris.Add(hull[left]);
                    tris.Add(hull[nright]);
                    tris.Add(hull[right]);
                    tris.Add(0);
                    right = nright;
                }
            }
        }

        private static float GetJitterX(int i)
        {
            return (((i * 0x8da6b343) & 0xffff) / 65535.0f * 2.0f) - 1.0f;
        }

        private static float GetJitterY(int i)
        {
            return (((i * 0xd8163841) & 0xffff) / 65535.0f * 2.0f) - 1.0f;
        }

        private static void BuildPolyDetail(
            float* @in, int nin, float sampleDist, float sampleMaxError, int heightSearchRadius, RcCompactHeightfield* chf, HeightPatch hp, float* verts,
            out int nverts, NativeList<int> tris, NativeList<int> edges, NativeList<int> samples)
        {
            const int maxVerts = 127;
            const int maxTris = 255; // Max tris for delaunay is 2n-2-k (n=num verts, k=num hull verts)
            const int maxVertsPerEdge = 32;
            var edge = stackalloc float[(maxVertsPerEdge + 1) * 3];
            var hull = stackalloc int[maxVerts];
            var idx = stackalloc int[maxVertsPerEdge];
            var nhull = 0;

            nverts = nin;

            for (var i = 0; i < nin; ++i)
            {
                verts[(i * 3) + 0] = @in[(i * 3) + 0];
                verts[(i * 3) + 1] = @in[(i * 3) + 1];
                verts[(i * 3) + 2] = @in[(i * 3) + 2];
            }

            edges.Clear();
            tris.Clear();

            var cs = chf->CellSize;
            var ics = 1.0f / cs;

            // Calculate minimum extents of the polygon based on input data
            var minExtent = PolyMinExtent(verts, nverts);

            // Tessellate outlines
            // This is done in separate pass in order to ensure
            // seamless height values across the ply boundaries
            if (sampleDist > 0)
            {
                for (int i = 0, j = nin - 1; i < nin; j = i++)
                {
                    var vj = &@in[j * 3];
                    var vi = &@in[i * 3];
                    var swapped = false;

                    // Make sure the segments are always handled in same order
                    // using lexological sort or else there will be seams
                    if (math.abs(vj[0] - vi[0]) < 1e-6f)
                    {
                        if (vj[2] > vi[2])
                        {
                            var temp = vj;
                            vj = vi;
                            vi = temp;
                            swapped = true;
                        }
                    }
                    else
                    {
                        if (vj[0] > vi[0])
                        {
                            var temp = vj;
                            vj = vi;
                            vi = temp;
                            swapped = true;
                        }
                    }

                    // Create samples along the edge
                    var dx = vi[0] - vj[0];
                    var dy = vi[1] - vj[1];
                    var dz = vi[2] - vj[2];
                    var d = math.sqrt((dx * dx) + (dz * dz));
                    var nn = 1 + (int)math.floor(d / sampleDist);
                    if (nn >= maxVertsPerEdge)
                    {
                        nn = maxVertsPerEdge - 1;
                    }

                    if (nverts + nn >= maxVerts)
                    {
                        nn = maxVerts - 1 - nverts;
                    }

                    for (var k = 0; k <= nn; ++k)
                    {
                        var u = k / (float)nn;
                        var pos = &edge[k * 3];
                        pos[0] = vj[0] + (dx * u);
                        pos[1] = vj[1] + (dy * u);
                        pos[2] = vj[2] + (dz * u);
                        pos[1] = GetHeight(pos[0], pos[1], pos[2], cs, ics, chf->CellHeight, heightSearchRadius, hp) * chf->CellHeight;
                    }

                    // Simplify samples
                    idx[0] = 0;
                    idx[1] = nn;
                    var nidx = 2;
                    for (var k = 0; k < nidx - 1;)
                    {
                        var a = idx[k];
                        var b = idx[k + 1];
                        var va = &edge[a * 3];
                        var vb = &edge[b * 3];

                        // Find maximum deviation along the segment
                        float maxd = 0;
                        var maxi = -1;
                        for (var m = a + 1; m < b; ++m)
                        {
                            var dev = DistancePtSeg(&edge[m * 3], va, vb);
                            if (dev > maxd)
                            {
                                maxd = dev;
                                maxi = m;
                            }
                        }

                        // If the max deviation is larger than accepted error,
                        // add new point, else continue to next segment
                        if (maxi != -1 && maxd > sampleMaxError * sampleMaxError)
                        {
                            for (var m = nidx; m > k; --m)
                            {
                                idx[m] = idx[m - 1];
                            }

                            idx[k + 1] = maxi;
                            nidx++;
                        }
                        else
                        {
                            ++k;
                        }
                    }

                    hull[nhull++] = j;

                    // Add new vertices
                    if (swapped)
                    {
                        for (var k = nidx - 2; k > 0; --k)
                        {
                            verts[(nverts * 3) + 0] = edge[(idx[k] * 3) + 0];
                            verts[(nverts * 3) + 1] = edge[(idx[k] * 3) + 1];
                            verts[(nverts * 3) + 2] = edge[(idx[k] * 3) + 2];
                            hull[nhull++] = nverts;
                            nverts++;
                        }
                    }
                    else
                    {
                        for (var k = 1; k < nidx - 1; ++k)
                        {
                            verts[(nverts * 3) + 0] = edge[(idx[k] * 3) + 0];
                            verts[(nverts * 3) + 1] = edge[(idx[k] * 3) + 1];
                            verts[(nverts * 3) + 2] = edge[(idx[k] * 3) + 2];
                            hull[nhull++] = nverts;
                            nverts++;
                        }
                    }
                }
            }

            // If the polygon minimum extent is small (sliver or small triangle), do not try to add internal points
            if (minExtent < sampleDist * 2)
            {
                TriangulateHull(nverts, verts, nhull, hull, nin, tris);
                SetTriFlags(tris, nhull, hull);
                return;
            }

            // Tessellate the base mesh
            // We're using the triangulateHull instead of delaunayHull as it tends to
            // create a bit better triangulation for long thin triangles when there
            // are no internal points
            TriangulateHull(nverts, verts, nhull, hull, nin, tris);

            if (tris.Length == 0)
            {
                // Could not triangulate the poly, make sure there is some valid data there
                return;
            }

            if (sampleDist > 0)
            {
                // Create sample locations in a grid
                var bmin = new float3(@in[0], @in[1], @in[2]);
                var bmax = bmin;
                for (var i = 1; i < nin; ++i)
                {
                    bmin = math.min(bmin, new float3(@in[i * 3], @in[(i * 3) + 1], @in[(i * 3) + 2]));
                    bmax = math.max(bmax, new float3(@in[i * 3], @in[(i * 3) + 1], @in[(i * 3) + 2]));
                }

                var x0 = (int)math.floor(bmin.x / sampleDist);
                var x1 = (int)math.ceil(bmax.x / sampleDist);
                var z0 = (int)math.floor(bmin.z / sampleDist);
                var z1 = (int)math.ceil(bmax.z / sampleDist);
                samples.Clear();
                for (var z = z0; z < z1; ++z)
                {
                    for (var x = x0; x < x1; ++x)
                    {
                        var pt = new float3(x * sampleDist, (bmax.y + bmin.y) * 0.5f, z * sampleDist);

                        // Make sure the samples are not too close to the edges
                        if (DistToPoly(nin, @in, (float*)&pt) > -sampleDist / 2)
                        {
                            continue;
                        }

                        samples.Add(x);
                        samples.Add(GetHeight(pt.x, pt.y, pt.z, cs, ics, chf->CellHeight, heightSearchRadius, hp));
                        samples.Add(z);
                        samples.Add(0); // Not added
                    }
                }

                // Add the samples starting from the one that has the most
                // error. The procedure stops when all samples are added
                // or when the max error is within treshold.
                var nsamples = samples.Length / 4;
                for (var iter = 0; iter < nsamples; ++iter)
                {
                    if (nverts >= maxVerts)
                    {
                        break;
                    }

                    // Find sample with most error
                    var bestpt = float3.zero;
                    float bestd = 0;
                    var besti = -1;
                    for (var i = 0; i < nsamples; ++i)
                    {
                        var s = samples.GetUnsafePtr() + (i * 4);
                        if (s[3] != 0)
                        {
                            continue; // skip added
                        }

                        var pt = new float3((s[0] * sampleDist) + (GetJitterX(i) * cs * 0.1f), s[1] * chf->CellHeight, (s[2] * sampleDist) + (GetJitterY(i) * cs * 0.1f));
                        if (tris.Length == 0)
                        {
                            continue;
                        }

                        var d = DistToTriMesh((float*)&pt, verts, nverts, tris.GetUnsafePtr(), tris.Length / 4);
                        if (d < 0)
                        {
                            continue; // did not hit the mesh
                        }

                        if (d > bestd)
                        {
                            bestd = d;
                            besti = i;
                            bestpt = pt;
                        }
                    }

                    // If the max error is within accepted threshold, stop tesselating
                    if (bestd <= sampleMaxError || besti == -1)
                    {
                        break;
                    }

                    // Mark sample as added
                    samples.GetUnsafePtr()[(besti * 4) + 3] = 1;

                    // Add the new sample point
                    verts[(nverts * 3) + 0] = bestpt.x;
                    verts[(nverts * 3) + 1] = bestpt.y;
                    verts[(nverts * 3) + 2] = bestpt.z;
                    nverts++;

                    // Create new triangulation
                    // TODO: Incremental add instead of full rebuild
                    edges.Clear();
                    tris.Clear();

                    DelaunayHull(nverts, verts, nhull, hull, tris, edges);
                }
            }

            var ntris = tris.Length / 4;
            if (ntris > maxTris)
            {
                tris.Resize(maxTris * 4, NativeArrayOptions.ClearMemory);
            }

            SetTriFlags(tris, nhull, hull);
        }

        private static bool OnHull(int a, int b, int nhull, int* hull)
        {
            // All internal sampled points come after the hull so we can early out for those.
            if (a >= nhull || b >= nhull)
            {
                return false;
            }

            for (int j = nhull - 1, i = 0; i < nhull; j = i++)
            {
                if (a == hull[j] && b == hull[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetTriFlags(NativeList<int> tris, int nhull, int* hull)
        {
            // Matches DT_DETAIL_EDGE_BOUNDARY
            const int detailEdgeBoundary = 0x1;

            for (var i = 0; i < tris.Length; i += 4)
            {
                var a = tris[i + 0];
                var b = tris[i + 1];
                var c = tris[i + 2];
                var flags = 0;
                flags |= (OnHull(a, b, nhull, hull) ? detailEdgeBoundary : 0) << 0;
                flags |= (OnHull(b, c, nhull, hull) ? detailEdgeBoundary : 0) << 2;
                flags |= (OnHull(c, a, nhull, hull) ? detailEdgeBoundary : 0) << 4;
                tris[i + 3] = flags;
            }
        }

        private struct HeightPatch
        {
            public ushort* Data;
            public int Xmin;
            public int Ymin;
            public int Width;
            public int Height;
        }
    }
}
