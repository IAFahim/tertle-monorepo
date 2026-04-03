// <copyright file="Recast.Contour.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Recast contour building functions for generating polygon contours from regions.
    /// </summary>
    public static unsafe partial class Recast
    {
        /// <summary>
        /// Builds contours for the regions in the specified compact heightfield.
        /// </summary>
        /// <param name="compactHeightfield">A fully built compact heightfield.</param>
        /// <param name="maxError">The maximum distance a simplified edge may deviate from the original edge [Limit: >=0] [Units: wu].</param>
        /// <param name="maxEdgeLen">The maximum allowed length for contour edges along the border of the mesh [Limit: >=0] [Units: vx].</param>
        /// <param name="contourSet">The resulting contour set.</param>
        /// <param name="buildFlags">The build flags.</param>
        public static void BuildContours(
            RcCompactHeightfield* compactHeightfield, float maxError, int maxEdgeLen, RcContourSet* contourSet, RcBuildContoursFlags buildFlags = RcBuildContoursFlags.RCContourTessWallEdges)
        {
            var w = compactHeightfield->Width;
            var h = compactHeightfield->Height;
            var borderSize = compactHeightfield->BorderSize;

            contourSet->Bmin = compactHeightfield->BMin;
            contourSet->Bmax = compactHeightfield->BMax;
            if (borderSize > 0)
            {
                // If the heightfield was built with bordersize, remove the offset
                var pad = borderSize * compactHeightfield->CellSize;
                contourSet->Bmin.x += pad;
                contourSet->Bmin.z += pad;
                contourSet->Bmax.x -= pad;
                contourSet->Bmax.z -= pad;
            }

            contourSet->Cs = compactHeightfield->CellSize;
            contourSet->Ch = compactHeightfield->CellHeight;
            contourSet->Width = compactHeightfield->Width - (compactHeightfield->BorderSize * 2);
            contourSet->Height = compactHeightfield->Height - (compactHeightfield->BorderSize * 2);
            contourSet->BorderSize = compactHeightfield->BorderSize;
            contourSet->MaxError = maxError;

            var maxContours = math.max(compactHeightfield->MaxRegions, 8);
            contourSet->Conts = (RcContour*)AllocatorManager.Allocate(contourSet->Allocator, sizeof(RcContour) * maxContours, UnsafeUtility.AlignOf<RcContour>());

            UnsafeUtility.MemClear(contourSet->Conts, sizeof(RcContour) * maxContours);
            contourSet->Nconts = 0;

            var flags = (byte*)AllocatorManager.Allocate(Allocator.Temp, sizeof(byte) * compactHeightfield->SpanCount, UnsafeUtility.AlignOf<byte>());

            // Mark boundaries
            for (var y = 0; y < h; ++y)
            {
                for (var x = 0; x < w; ++x)
                {
                    var c = compactHeightfield->Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        byte res = 0;
                        var s = compactHeightfield->Spans[i];
                        if (compactHeightfield->Spans[i].Reg == 0 || (compactHeightfield->Spans[i].Reg & RCBorderReg) != 0)
                        {
                            flags[i] = 0;
                            continue;
                        }

                        for (var dir = 0; dir < 4; ++dir)
                        {
                            ushort r = 0;
                            if (GetCon(s, dir) != RCNotConnected)
                            {
                                var ax = x + GetDirOffsetX(dir);
                                var ay = y + GetDirOffsetY(dir);
                                var ai = (int)compactHeightfield->Cells[ax + (ay * w)].Index + GetCon(s, dir);
                                r = compactHeightfield->Spans[ai].Reg;
                            }

                            if (r == compactHeightfield->Spans[i].Reg)
                            {
                                res |= (byte)(1 << dir);
                            }
                        }

                        flags[i] = (byte)(res ^ 0xf); // Inverse, mark non connected edges
                    }
                }
            }

            using var verts = new NativeList<int>(256, Allocator.Temp);
            using var simplified = new NativeList<int>(64, Allocator.Temp);

            for (var y = 0; y < h; ++y)
            {
                for (var x = 0; x < w; ++x)
                {
                    var c = compactHeightfield->Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        if (flags[i] == 0 || flags[i] == 0xf)
                        {
                            flags[i] = 0;
                            continue;
                        }

                        var reg = compactHeightfield->Spans[i].Reg;
                        if (reg == 0 || (reg & RCBorderReg) != 0)
                        {
                            continue;
                        }

                        var area = compactHeightfield->Areas[i];

                        verts.Clear();
                        simplified.Clear();

                        WalkContour(x, y, i, compactHeightfield, flags, verts);
                        SimplifyContour(verts, simplified, maxError, maxEdgeLen, buildFlags);
                        RemoveDegenerateSegments(simplified);

                        // Store region->contour remap info
                        // Create contour
                        if (simplified.Length / 4 >= 3)
                        {
                            if (contourSet->Nconts >= maxContours)
                            {
                                // Allocate more contours
                                maxContours *= 2;
                                var newConts = (RcContour*)AllocatorManager.Allocate(contourSet->Allocator, sizeof(RcContour) * maxContours,
                                    UnsafeUtility.AlignOf<RcContour>());

                                for (var j = 0; j < contourSet->Nconts; ++j)
                                {
                                    newConts[j] = contourSet->Conts[j];

                                    // Reset source pointers to prevent data deletion
                                    contourSet->Conts[j].Verts = null;
                                    contourSet->Conts[j].RVerts = null;
                                }

                                AllocatorManager.Free(contourSet->Allocator, contourSet->Conts);
                                contourSet->Conts = newConts;
                            }

                            var cont = &contourSet->Conts[contourSet->Nconts++];
                            *cont = new RcContour(contourSet->Allocator);

                            cont->NVerts = simplified.Length / 4;
                            cont->Verts = (int4*)AllocatorManager.Allocate(contourSet->Allocator, sizeof(int4) * cont->NVerts, UnsafeUtility.AlignOf<int4>());

                            UnsafeUtility.MemCpy(cont->Verts, simplified.GetUnsafePtr(), sizeof(int4) * cont->NVerts);
                            if (borderSize > 0)
                            {
                                // If the heightfield was built with bordersize, remove the offset
                                for (var j = 0; j < cont->NVerts; ++j)
                                {
                                    var v = cont->Verts + j;
                                    v->x -= borderSize;
                                    v->z -= borderSize;
                                }
                            }

                            cont->NRVerts = verts.Length / 4;
                            cont->RVerts = (int4*)AllocatorManager.Allocate(contourSet->Allocator, sizeof(int4) * cont->NRVerts, UnsafeUtility.AlignOf<int4>());

                            UnsafeUtility.MemCpy(cont->RVerts, verts.GetUnsafePtr(), sizeof(int4) * cont->NRVerts);
                            if (borderSize > 0)
                            {
                                // If the heightfield was built with bordersize, remove the offset
                                for (var j = 0; j < cont->NRVerts; ++j)
                                {
                                    var v = cont->RVerts + j;
                                    v->x -= borderSize;
                                    v->z -= borderSize;
                                }
                            }

                            cont->Reg = reg;
                            cont->Area = area;
                        }
                    }
                }
            }

            // Merge holes if needed
            if (contourSet->Nconts > 0)
            {
                // Calculate winding of all polygons
                var winding = (sbyte*)AllocatorManager.Allocate(Allocator.Temp, sizeof(sbyte) * contourSet->Nconts, UnsafeUtility.AlignOf<sbyte>());

                var nholes = 0;
                for (var i = 0; i < contourSet->Nconts; ++i)
                {
                    var cont = contourSet->Conts[i];

                    // If the contour is wound backwards, it is a hole
                    winding[i] = CalcAreaOfPolygon2D(cont.Verts, cont.NVerts) < 0 ? (sbyte)-1 : (sbyte)1;
                    if (winding[i] < 0)
                    {
                        nholes++;
                    }
                }

                if (nholes > 0)
                {
                    // Collect outline contour and holes contours per region
                    // We assume that there is one outline and multiple holes
                    var nregions = compactHeightfield->MaxRegions + 1;
                    var regions = (ContourRegion*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ContourRegion) * nregions, UnsafeUtility.AlignOf<ContourRegion>());

                    UnsafeUtility.MemClear(regions, sizeof(ContourRegion) * nregions);

                    var holes = (ContourHole*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ContourHole) * contourSet->Nconts, UnsafeUtility.AlignOf<ContourHole>());

                    UnsafeUtility.MemClear(holes, sizeof(ContourHole) * contourSet->Nconts);

                    for (var i = 0; i < contourSet->Nconts; ++i)
                    {
                        var cont = contourSet->Conts[i];

                        // Positively wound contours are outlines, negative holes
                        if (winding[i] > 0)
                        {
                            regions[cont.Reg].Outline = &contourSet->Conts[i];
                        }
                        else
                        {
                            regions[cont.Reg].Nholes++;
                        }
                    }

                    var index = 0;
                    for (var i = 0; i < nregions; i++)
                    {
                        if (regions[i].Nholes > 0)
                        {
                            regions[i].Holes = &holes[index];
                            index += regions[i].Nholes;
                            regions[i].Nholes = 0;
                        }
                    }

                    for (var i = 0; i < contourSet->Nconts; ++i)
                    {
                        var cont = contourSet->Conts[i];
                        var reg = &regions[cont.Reg];
                        if (winding[i] < 0)
                        {
                            reg->Holes[reg->Nholes++].Contour = &contourSet->Conts[i];
                        }
                    }

                    // Finally merge each regions holes into the outline
                    for (var i = 0; i < nregions; i++)
                    {
                        var reg = regions[i];
                        if (reg.Nholes == 0)
                        {
                            continue;
                        }

                        if (reg.Outline != null)
                        {
                            MergeRegionHoles(reg);
                        }
                    }
                }
            }
        }

        private static int GetCornerHeight(int x, int y, int i, int dir, RcCompactHeightfield* chf, ref bool isBorderVertex)
        {
            var s = chf->Spans[i];
            var ch = (int)s.Y;
            var dirp = (dir + 1) & 0x3;

            var regs = stackalloc uint[4];
            regs[0] = regs[1] = regs[2] = regs[3] = 0;

            // Combine region and area codes in order to prevent
            // border vertices which are in between two areas to be removed
            regs[0] = chf->Spans[i].Reg | ((uint)chf->Areas[i] << 16);

            if (GetCon(s, dir) != RCNotConnected)
            {
                var ax = x + GetDirOffsetX(dir);
                var ay = y + GetDirOffsetY(dir);
                var ai = (int)chf->Cells[ax + (ay * chf->Width)].Index + GetCon(s, dir);
                var aSpan = chf->Spans[ai];
                ch = math.max(ch, aSpan.Y);
                regs[1] = chf->Spans[ai].Reg | ((uint)chf->Areas[ai] << 16);
                if (GetCon(aSpan, dirp) != RCNotConnected)
                {
                    var ax2 = ax + GetDirOffsetX(dirp);
                    var ay2 = ay + GetDirOffsetY(dirp);
                    var ai2 = (int)chf->Cells[ax2 + (ay2 * chf->Width)].Index + GetCon(aSpan, dirp);
                    var as2 = chf->Spans[ai2];
                    ch = math.max(ch, as2.Y);
                    regs[2] = chf->Spans[ai2].Reg | ((uint)chf->Areas[ai2] << 16);
                }
            }

            if (GetCon(s, dirp) != RCNotConnected)
            {
                var ax = x + GetDirOffsetX(dirp);
                var ay = y + GetDirOffsetY(dirp);
                var ai = (int)chf->Cells[ax + (ay * chf->Width)].Index + GetCon(s, dirp);
                var aSpan = chf->Spans[ai];
                ch = math.max(ch, aSpan.Y);
                regs[3] = chf->Spans[ai].Reg | ((uint)chf->Areas[ai] << 16);
                if (GetCon(aSpan, dir) != RCNotConnected)
                {
                    var ax2 = ax + GetDirOffsetX(dir);
                    var ay2 = ay + GetDirOffsetY(dir);
                    var ai2 = (int)chf->Cells[ax2 + (ay2 * chf->Width)].Index + GetCon(aSpan, dir);
                    var as2 = chf->Spans[ai2];
                    ch = math.max(ch, as2.Y);
                    regs[2] = chf->Spans[ai2].Reg | ((uint)chf->Areas[ai2] << 16);
                }
            }

            // Check if the vertex is special edge vertex, these vertices will be removed later
            for (var j = 0; j < 4; ++j)
            {
                var a = j;
                var b = (j + 1) & 0x3;
                var c = (j + 2) & 0x3;
                var d = (j + 3) & 0x3;

                // The vertex is a border vertex there are two same exterior cells in a row,
                // followed by two interior cells and none of the regions are out of bounds
                var twoSameExts = (regs[a] & regs[b] & RCBorderReg) != 0 && regs[a] == regs[b];
                var twoInts = ((regs[c] | regs[d]) & RCBorderReg) == 0;
                var intsSameArea = regs[c] >> 16 == regs[d] >> 16;
                var noZeros = regs[a] != 0 && regs[b] != 0 && regs[c] != 0 && regs[d] != 0;
                if (twoSameExts && twoInts && intsSameArea && noZeros)
                {
                    isBorderVertex = true;
                    break;
                }
            }

            return ch;
        }

        private static void WalkContour(int x, int y, int i, RcCompactHeightfield* chf, byte* flags, NativeList<int> points)
        {
            // Choose the first non-connected edge
            byte dir = 0;
            while ((flags[i] & (1 << dir)) == 0)
            {
                dir++;
            }

            var startDir = dir;
            var starti = i;

            var area = chf->Areas[i];

            var iter = 0;
            while (++iter < 40000)
            {
                if ((flags[i] & (1 << dir)) != 0)
                {
                    // Choose the edge corner
                    var isBorderVertex = false;
                    var isAreaBorder = false;
                    var px = x;
                    var py = GetCornerHeight(x, y, i, dir, chf, ref isBorderVertex);
                    var pz = y;
                    switch (dir)
                    {
                        case 0:
                            pz++;
                            break;
                        case 1:
                            px++;
                            pz++;
                            break;
                        case 2:
                            px++;
                            break;
                    }

                    var r = 0;
                    var s = chf->Spans[i];
                    if (GetCon(s, dir) != RCNotConnected)
                    {
                        var ax = x + GetDirOffsetX(dir);
                        var ay = y + GetDirOffsetY(dir);
                        var ai = (int)chf->Cells[ax + (ay * chf->Width)].Index + GetCon(s, dir);
                        r = chf->Spans[ai].Reg;
                        if (area != chf->Areas[ai])
                        {
                            isAreaBorder = true;
                        }
                    }

                    if (isBorderVertex)
                    {
                        r |= RCBorderVertex;
                    }

                    if (isAreaBorder)
                    {
                        r |= RCAreaBorder;
                    }

                    points.Add(px);
                    points.Add(py);
                    points.Add(pz);
                    points.Add(r);

                    flags[i] &= (byte)~(1 << dir); // Remove visited edges
                    dir = (byte)((dir + 1) & 0x3); // Rotate CW
                }
                else
                {
                    var ni = -1;
                    var nx = x + GetDirOffsetX(dir);
                    var ny = y + GetDirOffsetY(dir);
                    var s = chf->Spans[i];
                    if (GetCon(s, dir) != RCNotConnected)
                    {
                        var nc = chf->Cells[nx + (ny * chf->Width)];
                        ni = (int)nc.Index + GetCon(s, dir);
                    }

                    if (ni == -1)
                    {
                        // Should not happen
                        return;
                    }

                    x = nx;
                    y = ny;
                    i = ni;
                    dir = (byte)((dir + 3) & 0x3); // Rotate CCW
                }

                if (starti == i && startDir == dir)
                {
                    break;
                }
            }
        }

        private static float DistancePtSeg(int x, int z, int px, int pz, int qx, int qz)
        {
            float pqx = qx - px;
            float pqz = qz - pz;
            float dx = x - px;
            float dz = z - pz;
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

            dx = px + (t * pqx) - x;
            dz = pz + (t * pqz) - z;

            return (dx * dx) + (dz * dz);
        }

        private static void SimplifyContour(NativeList<int> points, NativeList<int> simplified, float maxError, int maxEdgeLen, RcBuildContoursFlags buildFlags)
        {
            // Add initial points
            var hasConnections = false;
            for (var i = 0; i < points.Length; i += 4)
            {
                if ((points[i + 3] & RCContourRegMask) != 0)
                {
                    hasConnections = true;
                    break;
                }
            }

            if (hasConnections)
            {
                // The contour has some portals to other regions
                // Add a new point to every location where the region changes
                for (int i = 0, ni = points.Length / 4; i < ni; ++i)
                {
                    var ii = (i + 1) % ni;
                    var differentRegs = (points[(i * 4) + 3] & RCContourRegMask) != (points[(ii * 4) + 3] & RCContourRegMask);
                    var areaBorders = (points[(i * 4) + 3] & RCAreaBorder) != (points[(ii * 4) + 3] & RCAreaBorder);
                    if (differentRegs || areaBorders)
                    {
                        simplified.Add(points[(i * 4) + 0]);
                        simplified.Add(points[(i * 4) + 1]);
                        simplified.Add(points[(i * 4) + 2]);
                        simplified.Add(i);
                    }
                }
            }

            if (simplified.Length == 0)
            {
                // If there is no connections at all,
                // create some initial points for the simplification process
                // Find lower-left and upper-right vertices of the contour
                var llx = points[0];
                var lly = points[1];
                var llz = points[2];
                var lli = 0;
                var urx = points[0];
                var ury = points[1];
                var urz = points[2];
                var uri = 0;
                for (var i = 0; i < points.Length; i += 4)
                {
                    var x = points[i + 0];
                    var y = points[i + 1];
                    var z = points[i + 2];
                    if (x < llx || (x == llx && z < llz))
                    {
                        llx = x;
                        lly = y;
                        llz = z;
                        lli = i / 4;
                    }

                    if (x > urx || (x == urx && z > urz))
                    {
                        urx = x;
                        ury = y;
                        urz = z;
                        uri = i / 4;
                    }
                }

                simplified.Add(llx);
                simplified.Add(lly);
                simplified.Add(llz);
                simplified.Add(lli);

                simplified.Add(urx);
                simplified.Add(ury);
                simplified.Add(urz);
                simplified.Add(uri);
            }

            // Add points until all raw points are within error tolerance to the simplified shape
            var pn = points.Length / 4;
            for (var i = 0; i < simplified.Length / 4;)
            {
                var ii = (i + 1) % (simplified.Length / 4);

                var ax = simplified[(i * 4) + 0];
                var az = simplified[(i * 4) + 2];
                var ai = simplified[(i * 4) + 3];

                var bx = simplified[(ii * 4) + 0];
                var bz = simplified[(ii * 4) + 2];
                var bi = simplified[(ii * 4) + 3];

                // Find maximum deviation from the segment
                float maxd = 0;
                var maxi = -1;
                int ci, cinc, endi;

                // Traverse the segment in lexilogical order so that the
                // max deviation is calculated similarly when traversing opposite segments
                if (bx > ax || (bx == ax && bz > az))
                {
                    cinc = 1;
                    ci = (ai + cinc) % pn;
                    endi = bi;
                }
                else
                {
                    cinc = pn - 1;
                    ci = (bi + cinc) % pn;
                    endi = ai;

                    // Swap ax,bx and az,bz
                    (ax, bx) = (bx, ax);
                    (az, bz) = (bz, az);
                }

                // Tessellate only outer edges or edges between areas
                if ((points[(ci * 4) + 3] & RCContourRegMask) == 0 || (points[(ci * 4) + 3] & RCAreaBorder) != 0)
                {
                    while (ci != endi)
                    {
                        var d = DistancePtSeg(points[(ci * 4) + 0], points[(ci * 4) + 2], ax, az, bx, bz);
                        if (d > maxd)
                        {
                            maxd = d;
                            maxi = ci;
                        }

                        ci = (ci + cinc) % pn;
                    }
                }

                // If the max deviation is larger than accepted error, add new point, else continue to next segment
                if (maxi != -1 && maxd > maxError * maxError)
                {
                    // Add space for the new point
                    simplified.Resize(simplified.Length + 4, NativeArrayOptions.UninitializedMemory);
                    var n = simplified.Length / 4;
                    for (var j = n - 1; j > i; --j)
                    {
                        simplified[(j * 4) + 0] = simplified[((j - 1) * 4) + 0];
                        simplified[(j * 4) + 1] = simplified[((j - 1) * 4) + 1];
                        simplified[(j * 4) + 2] = simplified[((j - 1) * 4) + 2];
                        simplified[(j * 4) + 3] = simplified[((j - 1) * 4) + 3];
                    }

                    // Add the point
                    simplified[((i + 1) * 4) + 0] = points[(maxi * 4) + 0];
                    simplified[((i + 1) * 4) + 1] = points[(maxi * 4) + 1];
                    simplified[((i + 1) * 4) + 2] = points[(maxi * 4) + 2];
                    simplified[((i + 1) * 4) + 3] = maxi;
                }
                else
                {
                    ++i;
                }
            }

            // Split too long edges
            if (maxEdgeLen > 0 && (buildFlags & (RcBuildContoursFlags.RCContourTessWallEdges | RcBuildContoursFlags.RCContourTessAreaEdges)) != 0)
            {
                for (var i = 0; i < simplified.Length / 4;)
                {
                    var ii = (i + 1) % (simplified.Length / 4);

                    var ax = simplified[(i * 4) + 0];
                    var az = simplified[(i * 4) + 2];
                    var ai = simplified[(i * 4) + 3];

                    var bx = simplified[(ii * 4) + 0];
                    var bz = simplified[(ii * 4) + 2];
                    var bi = simplified[(ii * 4) + 3];

                    // Find maximum deviation from the segment
                    var maxi = -1;
                    var ci = (ai + 1) % pn;

                    // Tessellate only outer edges or edges between areas
                    // Wall edges or Edges between areas
                    var tess =
                        ((buildFlags & RcBuildContoursFlags.RCContourTessWallEdges) != 0 && (points[(ci * 4) + 3] & RCContourRegMask) == 0)
                        || ((buildFlags & RcBuildContoursFlags.RCContourTessAreaEdges) != 0 && (points[(ci * 4) + 3] & RCAreaBorder) != 0);

                    if (tess)
                    {
                        var dx = bx - ax;
                        var dz = bz - az;
                        if ((dx * dx) + (dz * dz) > maxEdgeLen * maxEdgeLen)
                        {
                            // Round based on the segments in lexilogical order so that the
                            // max tesselation is consistent regardless in which direction segments are traversed
                            var n = bi < ai ? bi + pn - ai : bi - ai;
                            if (n > 1)
                            {
                                if (bx > ax || (bx == ax && bz > az))
                                {
                                    maxi = (ai + (n / 2)) % pn;
                                }
                                else
                                {
                                    maxi = (ai + ((n + 1) / 2)) % pn;
                                }
                            }
                        }
                    }

                    // If the max deviation is larger than accepted error, add new point, else continue to next segment
                    if (maxi != -1)
                    {
                        // Add space for the new point
                        simplified.Resize(simplified.Length + 4, NativeArrayOptions.UninitializedMemory);
                        var n = simplified.Length / 4;
                        for (var j = n - 1; j > i; --j)
                        {
                            simplified[(j * 4) + 0] = simplified[((j - 1) * 4) + 0];
                            simplified[(j * 4) + 1] = simplified[((j - 1) * 4) + 1];
                            simplified[(j * 4) + 2] = simplified[((j - 1) * 4) + 2];
                            simplified[(j * 4) + 3] = simplified[((j - 1) * 4) + 3];
                        }

                        // Add the point
                        simplified[((i + 1) * 4) + 0] = points[(maxi * 4) + 0];
                        simplified[((i + 1) * 4) + 1] = points[(maxi * 4) + 1];
                        simplified[((i + 1) * 4) + 2] = points[(maxi * 4) + 2];
                        simplified[((i + 1) * 4) + 3] = maxi;
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            for (var i = 0; i < simplified.Length / 4; ++i)
            {
                // The edge vertex flag is taken from the current raw point,
                // and the neighbour region is taken from the next raw point
                var ai = (simplified[(i * 4) + 3] + 1) % pn;
                var bi = simplified[(i * 4) + 3];
                simplified[(i * 4) + 3] = (points[(ai * 4) + 3] & (RCContourRegMask | RCAreaBorder)) |
                    (points[(bi * 4) + 3] & RCBorderVertex);
            }
        }

        private static int CalcAreaOfPolygon2D(int4* verts, int nverts)
        {
            var area = 0;
            for (int i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                var vi = verts[i];
                var vj = verts[j];
                area += (vi.x * vj.z) - (vj.x * vi.z);
            }

            return (area + 1) / 2;
        }

        private static void RemoveDegenerateSegments(NativeList<int> simplified)
        {
            // Remove adjacent vertices which are equal on xz-plane, or else the triangulator will get confused.
            var npts = simplified.Length / 4;
            for (var i = npts - 1; i >= 0; --i)
            {
                if (npts < 2)
                {
                    break;
                }

                // Get the index of the next vertex in the polygon, wrapping around.
                var ni = (i + 1) % npts;

                // If the vertex at 'i' is the same as the next one, it's a degenerate segment.
                if (VEqual(simplified, i * 4, ni * 4))
                {
                    simplified.RemoveRange(i * 4, 4);
                    npts--;
                }
            }
        }

        private static bool VEqual(NativeList<int> verts, int a, int b)
        {
            return verts[a] == verts[b] && verts[a + 2] == verts[b + 2];
        }

        private static void MergeRegionHoles(ContourRegion region)
        {
            // Sort holes from left to right
            for (var i = 0; i < region.Nholes; i++)
            {
                FindLeftMostVertex(region.Holes[i].Contour, out region.Holes[i].Minx, out region.Holes[i].Minz, out region.Holes[i].Leftmost);
            }

            // Sort holes by position
            SortHoles(region.Holes, region.Nholes);

            var maxVerts = region.Outline->NVerts;
            for (var i = 0; i < region.Nholes; i++)
            {
                maxVerts += region.Holes[i].Contour->NVerts;
            }

            var diags = (PotentialDiagonal*)AllocatorManager.Allocate(Allocator.Temp, sizeof(PotentialDiagonal) * maxVerts, UnsafeUtility.AlignOf<PotentialDiagonal>());

            var outline = region.Outline;

            // Merge holes into the outline one by one
            for (var i = 0; i < region.Nholes; i++)
            {
                var hole = region.Holes[i].Contour;

                var index = -1;
                var bestVertex = region.Holes[i].Leftmost;
                for (var iter = 0; iter < hole->NVerts; iter++)
                {
                    // Find potential diagonals
                    var ndiags = 0;
                    var corner = hole->Verts + bestVertex;
                    for (var j = 0; j < outline->NVerts; j++)
                    {
                        if (InCone(j, outline->NVerts, outline->Verts, corner))
                        {
                            var dx = outline->Verts[j].x - corner->x;
                            var dz = outline->Verts[j].z - corner->z;
                            diags[ndiags].Vert = j;
                            diags[ndiags].Dist = (dx * dx) + (dz * dz);
                            ndiags++;
                        }
                    }

                    // Sort potential diagonals by distance
                    SortDiagonals(diags, ndiags);

                    // Find a diagonal that is not intersecting the outline or remaining holes
                    index = -1;
                    for (var j = 0; j < ndiags; j++)
                    {
                        var pt = outline->Verts + diags[j].Vert;
                        var intersect = IntersectSegContour(pt, corner, diags[j].Vert, outline->NVerts, outline->Verts);
                        for (var k = i; k < region.Nholes && !intersect; k++)
                        {
                            intersect |= IntersectSegContour(pt, corner, -1, region.Holes[k].Contour->NVerts, region.Holes[k].Contour->Verts);
                        }

                        if (!intersect)
                        {
                            index = diags[j].Vert;
                            break;
                        }
                    }

                    // If found non-intersecting diagonal, stop looking
                    if (index != -1)
                    {
                        break;
                    }

                    // All potential diagonals were intersecting, try next vertex
                    bestVertex = (bestVertex + 1) % hole->NVerts;
                }

                if (index == -1)
                {
                    continue;
                }

                if (!MergeContours(region.Outline, hole, index, bestVertex))
                {
                    continue;
                }
            }
        }

        private static void FindLeftMostVertex(RcContour* contour, out int minx, out int minz, out int leftmost)
        {
            minx = contour->Verts[0].x;
            minz = contour->Verts[0].z;
            leftmost = 0;
            for (var i = 1; i < contour->NVerts; i++)
            {
                var x = contour->Verts[i].x;
                var z = contour->Verts[i].z;
                if (x < minx || (x == minx && z < minz))
                {
                    minx = x;
                    minz = z;
                    leftmost = i;
                }
            }
        }

        private static void SortHoles(ContourHole* holes, int nholes)
        {
            // Simple insertion sort for holes
            for (var i = 1; i < nholes; i++)
            {
                var key = holes[i];
                var j = i - 1;
                while (j >= 0 && CompareHoles(holes[j], key) > 0)
                {
                    holes[j + 1] = holes[j];
                    j--;
                }

                holes[j + 1] = key;
            }
        }

        private static int CompareHoles(ContourHole a, ContourHole b)
        {
            if (a.Minx == b.Minx)
            {
                if (a.Minz < b.Minz)
                {
                    return -1;
                }

                if (a.Minz > b.Minz)
                {
                    return 1;
                }

                return 0;
            }
            else
            {
                if (a.Minx < b.Minx)
                {
                    return -1;
                }

                if (a.Minx > b.Minx)
                {
                    return 1;
                }

                return 0;
            }
        }

        private static void SortDiagonals(PotentialDiagonal* diags, int ndiags)
        {
            // Simple insertion sort for diagonals
            for (var i = 1; i < ndiags; i++)
            {
                var key = diags[i];
                var j = i - 1;
                while (j >= 0 && diags[j].Dist > key.Dist)
                {
                    diags[j + 1] = diags[j];
                    j--;
                }

                diags[j + 1] = key;
            }
        }

        private static bool MergeContours(RcContour* ca, RcContour* cb, int ia, int ib)
        {
            var maxVerts = ca->NVerts + cb->NVerts + 2;
            var verts = (int4*)AllocatorManager.Allocate(ca->Allocator, sizeof(int4) * maxVerts, UnsafeUtility.AlignOf<int4>());

            var nv = 0;

            // Copy contour A
            for (var i = 0; i <= ca->NVerts; ++i)
            {
                verts[nv] = ca->Verts[(ia + i) % ca->NVerts];
                nv++;
            }

            // Copy contour B
            for (var i = 0; i <= cb->NVerts; ++i)
            {
                verts[nv] = cb->Verts[(ib + i) % cb->NVerts];
                nv++;
            }

            AllocatorManager.Free(ca->Allocator, ca->Verts);
            ca->Verts = verts;
            ca->NVerts = nv;

            AllocatorManager.Free(cb->Allocator, cb->Verts);
            cb->Verts = null;
            cb->NVerts = 0;

            return true;
        }

        private static bool IntersectSegContour(int4* d0, int4* d1, int i, int n, int4* verts)
        {
            // For each edge (k,k+1) of P
            for (var k = 0; k < n; k++)
            {
                var k1 = Next(k, n);

                // Skip edges incident to i
                if (i == k || i == k1)
                {
                    continue;
                }

                var p0 = verts + k;
                var p1 = verts + k1;
                if (VEqual((int*)d0, (int*)p0) || VEqual((int*)d1, (int*)p0) || VEqual((int*)d0, (int*)p1) || VEqual((int*)d1, (int*)p1))
                {
                    continue;
                }

                if (Intersect((int*)d0, (int*)d1, (int*)p0, (int*)p1))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool InCone(int i, int n, int4* verts, int4* pj)
        {
            var pi = verts + i;
            var pi1 = verts + Next(i, n);
            var pin1 = verts + Prev(i, n);

            // If P[i] is a convex vertex [ i+1 left or on (i-1,i) ]
            if (LeftOn((int*)pin1, (int*)pi, (int*)pi1))
            {
                return Left((int*)pi, (int*)pj, (int*)pin1) && Left((int*)pj, (int*)pi, (int*)pi1);
            }

            // Assume (i-1,i,i+1) not collinear
            // else P[i] is reflex
            return !(LeftOn((int*)pi, (int*)pj, (int*)pi1) && LeftOn((int*)pj, (int*)pi, (int*)pin1));
        }

        private struct PotentialDiagonal
        {
            public int Vert;
            public int Dist;
        }

        private struct ContourHole
        {
            public RcContour* Contour;
            public int Minx;
            public int Minz;
            public int Leftmost;
        }

        private struct ContourRegion
        {
            public RcContour* Outline;
            public ContourHole* Holes;
            public int Nholes;
        }
    }
}
