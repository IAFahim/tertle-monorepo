// <copyright file="Recast.Region.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Recast region building functions for partitioning the heightfield into non-overlapping regions.
    /// These functions handle distance field calculation, watershed partitioning, and region merging.
    /// </summary>
    public static unsafe partial class Recast
    {
        private const ushort RCNullNei = 0xffff;

        /// <summary>
        /// Builds the distance field for the compact heightfield.
        /// This is usually the second to the last step in creating a fully built compact heightfield.
        /// </summary>
        /// <param name="compactHeightfield">The compact heightfield to process.</param>
        public static void BuildDistanceField(RcCompactHeightfield* compactHeightfield)
        {
            if (compactHeightfield->Dist != null)
            {
                AllocatorManager.Free(compactHeightfield->Allocator, compactHeightfield->Dist);
                compactHeightfield->Dist = null;
            }

            // Allocate distance arrays
            var src = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * compactHeightfield->SpanCount, UnsafeUtility.AlignOf<ushort>());
            var dst = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * compactHeightfield->SpanCount, UnsafeUtility.AlignOf<ushort>());

            CalculateDistanceField(*compactHeightfield, src, out var maxDist);
            compactHeightfield->MaxDistance = maxDist;

            var dist = BoxBlur(*compactHeightfield, 1, src, dst);

            // Store distance
            compactHeightfield->Dist = (ushort*)AllocatorManager.Allocate(compactHeightfield->Allocator, sizeof(ushort) * compactHeightfield->SpanCount,
                UnsafeUtility.AlignOf<ushort>());
            UnsafeUtility.MemCpy(compactHeightfield->Dist, dist, sizeof(ushort) * compactHeightfield->SpanCount);
        }

        /// <summary>
        /// Builds regions using watershed partitioning.
        /// </summary>
        /// <param name="compactHeightfield">A fully built compact heightfield.</param>
        /// <param name="borderSize">The size of the non-navigable border around the heightfield [Units: vx].</param>
        /// <param name="minRegionArea">The minimum number of cells allowed to form isolated island areas [Units: vx].</param>
        /// <param name="mergeRegionArea">Any regions with a span count smaller than this value will be merged with larger regions [Units: vx].</param>
        /// <returns>True if the operation completed successfully.</returns>
        public static bool BuildRegions(RcCompactHeightfield* compactHeightfield, int borderSize, int minRegionArea, int mergeRegionArea)
        {
            var w = compactHeightfield->Width;
            var h = compactHeightfield->Height;

            var buf = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * compactHeightfield->SpanCount * 2, UnsafeUtility.AlignOf<ushort>());

            const int logNbStacks = 3;
            const int nbStacks = 1 << logNbStacks;
            var lvlStacks = new NativeArray<NativeList<LevelStackEntry>>(nbStacks, Allocator.Temp);
            for (var i = 0; i < nbStacks; ++i)
            {
                lvlStacks[i] = new NativeList<LevelStackEntry>(256, Allocator.Temp);
            }

            var stack = new NativeList<LevelStackEntry>(256, Allocator.Temp);

            var srcReg = buf;
            var srcDist = buf + compactHeightfield->SpanCount;

            UnsafeUtility.MemClear(srcReg, sizeof(ushort) * compactHeightfield->SpanCount);
            UnsafeUtility.MemClear(srcDist, sizeof(ushort) * compactHeightfield->SpanCount);

            ushort regionId = 1;
            var level = (ushort)((compactHeightfield->MaxDistance + 1) & ~1);

            const int expandIters = 8;

            if (borderSize > 0)
            {
                // Make sure border will not overflow
                var bw = math.min(w, borderSize);
                var bh = math.min(h, borderSize);

                // Paint regions
                PaintRectRegion(0, bw, 0, h, (ushort)(regionId | RCBorderReg), *compactHeightfield, srcReg);
                regionId++;
                PaintRectRegion(w - bw, w, 0, h, (ushort)(regionId | RCBorderReg), *compactHeightfield, srcReg);
                regionId++;
                PaintRectRegion(0, w, 0, bh, (ushort)(regionId | RCBorderReg), *compactHeightfield, srcReg);
                regionId++;
                PaintRectRegion(0, w, h - bh, h, (ushort)(regionId | RCBorderReg), *compactHeightfield, srcReg);
                regionId++;
            }

            compactHeightfield->BorderSize = borderSize;

            var sId = -1;
            while (level > 0)
            {
                level = level >= 2 ? (ushort)(level - 2) : (ushort)0;
                sId = (sId + 1) & (nbStacks - 1);

                if (sId == 0)
                {
                    SortCellsByLevel(level, *compactHeightfield, srcReg, nbStacks, lvlStacks, 1);
                }
                else
                {
                    AppendStacks(lvlStacks[sId - 1], lvlStacks[sId], srcReg);
                }

                // Expand current regions until no empty connected cells found
                ExpandRegions(expandIters, level, *compactHeightfield, srcReg, srcDist, lvlStacks[sId], false);

                // Mark new regions with IDs
                for (var j = 0; j < lvlStacks[sId].Length; j++)
                {
                    var current = lvlStacks[sId][j];
                    var x = current.X;
                    var y = current.Y;
                    var i = current.Index;
                    if (i >= 0 && srcReg[i] == 0)
                    {
                        if (FloodRegion(x, y, i, level, regionId, *compactHeightfield, srcReg, srcDist, stack))
                        {
                            if (regionId == 0xFFFF)
                            {
                                return false;
                            }

                            regionId++;
                        }
                    }
                }
            }

            // Expand current regions until no empty connected cells found
            ExpandRegions(expandIters * 8, 0, *compactHeightfield, srcReg, srcDist, stack, true);

            // Merge regions and filter out small regions
            var overlaps = new NativeList<int>(32, Allocator.Temp);
            compactHeightfield->MaxRegions = regionId;
            MergeAndFilterRegions(minRegionArea, mergeRegionArea, ref compactHeightfield->MaxRegions, *compactHeightfield, srcReg, overlaps);

            // Write the result out
            for (var i = 0; i < compactHeightfield->SpanCount; ++i)
            {
                compactHeightfield->Spans[i].Reg = srcReg[i];
            }

            return true;
        }

        /// <summary>
        /// Builds regions using monotone partitioning.
        /// </summary>
        /// <param name="compactHeightfield">A fully built compact heightfield.</param>
        /// <param name="borderSize">The size of the non-navigable border around the heightfield [Units: vx].</param>
        /// <param name="minRegionArea">The minimum number of cells allowed to form isolated island areas [Units: vx].</param>
        /// <param name="mergeRegionArea">Any regions with a span count smaller than this value will be merged with larger regions [Units: vx].</param>
        public static void BuildRegionsMonotone(RcCompactHeightfield* compactHeightfield, int borderSize, int minRegionArea, int mergeRegionArea)
        {
            var w = compactHeightfield->Width;
            var h = compactHeightfield->Height;
            ushort id = 1;

            var srcReg = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * compactHeightfield->SpanCount, UnsafeUtility.AlignOf<ushort>());
            UnsafeUtility.MemClear(srcReg, sizeof(ushort) * compactHeightfield->SpanCount);

            var nsweeps = math.max(compactHeightfield->Width, compactHeightfield->Height);
            var sweeps = (RcSweepSpan*)AllocatorManager.Allocate(Allocator.Temp, sizeof(RcSweepSpan) * nsweeps, UnsafeUtility.AlignOf<RcSweepSpan>());

            // Mark border regions
            if (borderSize > 0)
            {
                var bw = math.min(w, borderSize);
                var bh = math.min(h, borderSize);
                PaintRectRegion(0, bw, 0, h, (ushort)(id | RCBorderReg), *compactHeightfield, srcReg);
                id++;
                PaintRectRegion(w - bw, w, 0, h, (ushort)(id | RCBorderReg), *compactHeightfield, srcReg);
                id++;
                PaintRectRegion(0, w, 0, bh, (ushort)(id | RCBorderReg), *compactHeightfield, srcReg);
                id++;
                PaintRectRegion(0, w, h - bh, h, (ushort)(id | RCBorderReg), *compactHeightfield, srcReg);
                id++;
            }

            compactHeightfield->BorderSize = borderSize;

            var prev = new NativeList<int>(256, Allocator.Temp);

            // Sweep one line at a time
            for (var y = borderSize; y < h - borderSize; ++y)
            {
                prev.Resize(id + 1, NativeArrayOptions.ClearMemory);
                ushort rid = 1;

                for (var x = borderSize; x < w - borderSize; ++x)
                {
                    var c = compactHeightfield->Cells[x + (y * w)];

                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        var s = compactHeightfield->Spans[i];
                        if (compactHeightfield->Areas[i] == RCNullArea)
                        {
                            continue;
                        }

                        // -x
                        ushort previd = 0;
                        if (GetCon(s, 0) != RCNotConnected)
                        {
                            var ax = x + GetDirOffsetX(0);
                            var ay = y + GetDirOffsetY(0);
                            var ai = (int)compactHeightfield->Cells[ax + (ay * w)].Index + GetCon(s, 0);
                            if ((srcReg[ai] & RCBorderReg) == 0 && compactHeightfield->Areas[i] == compactHeightfield->Areas[ai])
                            {
                                previd = srcReg[ai];
                            }
                        }

                        if (previd == 0)
                        {
                            previd = rid++;
                            sweeps[previd].Rid = previd;
                            sweeps[previd].Ns = 0;
                            sweeps[previd].Nei = 0;
                        }

                        // -y
                        if (GetCon(s, 3) != RCNotConnected)
                        {
                            var ax = x + GetDirOffsetX(3);
                            var ay = y + GetDirOffsetY(3);
                            var ai = (int)compactHeightfield->Cells[ax + (ay * w)].Index + GetCon(s, 3);
                            if (srcReg[ai] != 0 && (srcReg[ai] & RCBorderReg) == 0 && compactHeightfield->Areas[i] == compactHeightfield->Areas[ai])
                            {
                                var nr = srcReg[ai];
                                if (sweeps[previd].Nei == 0 || sweeps[previd].Nei == nr)
                                {
                                    sweeps[previd].Nei = nr;
                                    sweeps[previd].Ns++;
                                    prev[nr]++;
                                }
                                else
                                {
                                    sweeps[previd].Nei = RCNullNei;
                                }
                            }
                        }

                        srcReg[i] = previd;
                    }
                }

                // Create unique ID
                for (var i = 1; i < rid; ++i)
                {
                    if (sweeps[i].Nei != RCNullNei && sweeps[i].Nei != 0 &&
                        prev[sweeps[i].Nei] == sweeps[i].Ns)
                    {
                        sweeps[i].ID = sweeps[i].Nei;
                    }
                    else
                    {
                        sweeps[i].ID = id++;
                    }
                }

                // Remap IDs
                for (var x = borderSize; x < w - borderSize; ++x)
                {
                    var c = compactHeightfield->Cells[x + (y * w)];

                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        if (srcReg[i] > 0 && srcReg[i] < rid)
                        {
                            srcReg[i] = sweeps[srcReg[i]].ID;
                        }
                    }
                }
            }

            // Merge regions and filter out small regions
            var overlaps = new NativeList<int>(32, Allocator.Temp);
            compactHeightfield->MaxRegions = id;
            MergeAndFilterRegions(minRegionArea, mergeRegionArea, ref compactHeightfield->MaxRegions, *compactHeightfield, srcReg, overlaps);

            // Store the result out
            for (var i = 0; i < compactHeightfield->SpanCount; ++i)
            {
                compactHeightfield->Spans[i].Reg = srcReg[i];
            }
        }

        /// <summary>
        /// Builds layer regions for creating heightfield layers.
        /// </summary>
        /// <param name="compactHeightfield">A fully built compact heightfield.</param>
        /// <param name="borderSize">The size of the non-navigable border around the heightfield [Units: vx].</param>
        /// <param name="minRegionArea">The minimum number of cells allowed to form isolated island areas [Units: vx].</param>
        /// <returns>True if the operation completed successfully.</returns>
        public static bool BuildLayerRegions(RcCompactHeightfield* compactHeightfield, int borderSize, int minRegionArea)
        {
            var w = compactHeightfield->Width;
            var h = compactHeightfield->Height;
            ushort id = 1;

            var srcReg = (ushort*)AllocatorManager.Allocate(Allocator.Temp, sizeof(ushort) * compactHeightfield->SpanCount, UnsafeUtility.AlignOf<ushort>());

            UnsafeUtility.MemClear(srcReg, sizeof(ushort) * compactHeightfield->SpanCount);

            var nsweeps = math.max(compactHeightfield->Width, compactHeightfield->Height);
            var sweeps = (RcSweepSpan*)AllocatorManager.Allocate(Allocator.Temp, sizeof(RcSweepSpan) * nsweeps, UnsafeUtility.AlignOf<RcSweepSpan>());

            // Mark border regions
            if (borderSize > 0)
            {
                var bw = math.min(w, borderSize);
                var bh = math.min(h, borderSize);
                PaintRectRegion(0, bw, 0, h, (ushort)(id | RCBorderReg), *compactHeightfield, srcReg);
                id++;
                PaintRectRegion(w - bw, w, 0, h, (ushort)(id | RCBorderReg), *compactHeightfield, srcReg);
                id++;
                PaintRectRegion(0, w, 0, bh, (ushort)(id | RCBorderReg), *compactHeightfield, srcReg);
                id++;
                PaintRectRegion(0, w, h - bh, h, (ushort)(id | RCBorderReg), *compactHeightfield, srcReg);
                id++;
            }

            compactHeightfield->BorderSize = borderSize;

            var prev = new NativeList<int>(256, Allocator.Temp);

            // Sweep one line at a time
            for (var y = borderSize; y < h - borderSize; ++y)
            {
                prev.Resize(id + 1, NativeArrayOptions.ClearMemory);
                ushort rid = 1;

                for (var x = borderSize; x < w - borderSize; ++x)
                {
                    var c = compactHeightfield->Cells[x + (y * w)];

                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        var s = compactHeightfield->Spans[i];
                        if (compactHeightfield->Areas[i] == RCNullArea)
                        {
                            continue;
                        }

                        // -x
                        ushort previd = 0;
                        if (GetCon(s, 0) != RCNotConnected)
                        {
                            var ax = x + GetDirOffsetX(0);
                            var ay = y + GetDirOffsetY(0);
                            var ai = (int)compactHeightfield->Cells[ax + (ay * w)].Index + GetCon(s, 0);
                            if ((srcReg[ai] & RCBorderReg) == 0 && compactHeightfield->Areas[i] == compactHeightfield->Areas[ai])
                            {
                                previd = srcReg[ai];
                            }
                        }

                        if (previd == 0)
                        {
                            previd = rid++;
                            sweeps[previd].Rid = previd;
                            sweeps[previd].Ns = 0;
                            sweeps[previd].Nei = 0;
                        }

                        // -y
                        if (GetCon(s, 3) != RCNotConnected)
                        {
                            var ax = x + GetDirOffsetX(3);
                            var ay = y + GetDirOffsetY(3);
                            var ai = (int)compactHeightfield->Cells[ax + (ay * w)].Index + GetCon(s, 3);
                            if (srcReg[ai] != 0 && (srcReg[ai] & RCBorderReg) == 0 && compactHeightfield->Areas[i] == compactHeightfield->Areas[ai])
                            {
                                var nr = srcReg[ai];
                                if (sweeps[previd].Nei == 0 || sweeps[previd].Nei == nr)
                                {
                                    sweeps[previd].Nei = nr;
                                    sweeps[previd].Ns++;
                                    prev[nr]++;
                                }
                                else
                                {
                                    sweeps[previd].Nei = RCNullNei;
                                }
                            }
                        }

                        srcReg[i] = previd;
                    }
                }

                // Create unique ID
                for (var i = 1; i < rid; ++i)
                {
                    if (sweeps[i].Nei != RCNullNei && sweeps[i].Nei != 0 &&
                        prev[sweeps[i].Nei] == sweeps[i].Ns)
                    {
                        sweeps[i].ID = sweeps[i].Nei;
                    }
                    else
                    {
                        sweeps[i].ID = id++;
                    }
                }

                // Remap IDs
                for (var x = borderSize; x < w - borderSize; ++x)
                {
                    var c = compactHeightfield->Cells[x + (y * w)];

                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        if (srcReg[i] > 0 && srcReg[i] < rid)
                        {
                            srcReg[i] = sweeps[srcReg[i]].ID;
                        }
                    }
                }
            }

            // Merge monotone regions to layers and remove small regions
            compactHeightfield->MaxRegions = id;
            if (!MergeAndFilterLayerRegions(minRegionArea, ref compactHeightfield->MaxRegions, *compactHeightfield, srcReg))
            {
                return false;
            }

            // Store the result out
            for (var i = 0; i < compactHeightfield->SpanCount; ++i)
            {
                compactHeightfield->Spans[i].Reg = srcReg[i];
            }

            return true;
        }

        private struct LevelStackEntry
        {
            public int X;
            public int Y;
            public int Index;

            public LevelStackEntry(int x, int y, int index)
            {
                this.X = x;
                this.Y = y;
                this.Index = index;
            }
        }

        private struct DirtyEntry
        {
            public int Index;
            public ushort Region;
            public ushort Distance2;

            public DirtyEntry(int index, ushort region, ushort distance2)
            {
                this.Index = index;
                this.Region = region;
                this.Distance2 = distance2;
            }
        }

        private struct RcRegion
        {
            public int SpanCount;                    // Number of spans belonging to this region
            public ushort ID;                        // ID of the region
            public byte AreaType;                    // Area type
            public bool Remap;
            public bool Visited;
            public bool Overlap;
            public bool ConnectsToBorder;
            public ushort Ymin;
            public ushort Ymax;
            public NativeList<int> Connections;
            public NativeList<int> Floors;

            public RcRegion(ushort i)
            {
                this.SpanCount = 0;
                this.ID = i;
                this.AreaType = 0;
                this.Remap = false;
                this.Visited = false;
                this.Overlap = false;
                this.ConnectsToBorder = false;
                this.Ymin = 0xffff;
                this.Ymax = 0;
                this.Connections = new NativeList<int>(Allocator.Temp);
                this.Floors = new NativeList<int>(Allocator.Temp);
            }
        }

        private struct RcSweepSpan
        {
            public ushort Rid;   // row id
            public ushort ID;    // region id
            public ushort Ns;    // number samples
            public ushort Nei;   // neighbour id
        }

        private static void CalculateDistanceField(in RcCompactHeightfield chf, ushort* src, out ushort maxDist)
        {
            var w = chf.Width;
            var h = chf.Height;

            // Init distance and points
            for (var i = 0; i < chf.SpanCount; ++i)
            {
                src[i] = 0xffff;
            }

            // Mark boundary cells
            for (var y = 0; y < h; ++y)
            {
                for (var x = 0; x < w; ++x)
                {
                    var c = chf.Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        var s = chf.Spans[i];
                        var area = chf.Areas[i];

                        var nc = 0;
                        for (var dir = 0; dir < 4; ++dir)
                        {
                            if (GetCon(s, dir) != RCNotConnected)
                            {
                                var ax = x + GetDirOffsetX(dir);
                                var ay = y + GetDirOffsetY(dir);
                                var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(s, dir);
                                if (area == chf.Areas[ai])
                                {
                                    nc++;
                                }
                            }
                        }

                        if (nc != 4)
                        {
                            src[i] = 0;
                        }
                    }
                }
            }

            // Pass 1
            for (var y = 0; y < h; ++y)
            {
                for (var x = 0; x < w; ++x)
                {
                    var c = chf.Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        var s = chf.Spans[i];

                        if (GetCon(s, 0) != RCNotConnected)
                        {
                            // (-1,0)
                            var ax = x + GetDirOffsetX(0);
                            var ay = y + GetDirOffsetY(0);
                            var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(s, 0);
                            var @as = chf.Spans[ai];
                            if (src[ai] + 2 < src[i])
                            {
                                src[i] = (ushort)(src[ai] + 2);
                            }

                            // (-1,-1)
                            if (GetCon(@as, 3) != RCNotConnected)
                            {
                                var aax = ax + GetDirOffsetX(3);
                                var aay = ay + GetDirOffsetY(3);
                                var aai = (int)chf.Cells[aax + (aay * w)].Index + GetCon(@as, 3);
                                if (src[aai] + 3 < src[i])
                                {
                                    src[i] = (ushort)(src[aai] + 3);
                                }
                            }
                        }

                        if (GetCon(s, 3) != RCNotConnected)
                        {
                            // (0,-1)
                            var ax = x + GetDirOffsetX(3);
                            var ay = y + GetDirOffsetY(3);
                            var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(s, 3);
                            var @as = chf.Spans[ai];
                            if (src[ai] + 2 < src[i])
                            {
                                src[i] = (ushort)(src[ai] + 2);
                            }

                            // (1,-1)
                            if (GetCon(@as, 2) != RCNotConnected)
                            {
                                var aax = ax + GetDirOffsetX(2);
                                var aay = ay + GetDirOffsetY(2);
                                var aai = (int)chf.Cells[aax + (aay * w)].Index + GetCon(@as, 2);
                                if (src[aai] + 3 < src[i])
                                {
                                    src[i] = (ushort)(src[aai] + 3);
                                }
                            }
                        }
                    }
                }
            }

            // Pass 2
            for (var y = h - 1; y >= 0; --y)
            {
                for (var x = w - 1; x >= 0; --x)
                {
                    var c = chf.Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        var s = chf.Spans[i];

                        if (GetCon(s, 2) != RCNotConnected)
                        {
                            // (1,0)
                            var ax = x + GetDirOffsetX(2);
                            var ay = y + GetDirOffsetY(2);
                            var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(s, 2);
                            var @as = chf.Spans[ai];
                            if (src[ai] + 2 < src[i])
                            {
                                src[i] = (ushort)(src[ai] + 2);
                            }

                            // (1,1)
                            if (GetCon(@as, 1) != RCNotConnected)
                            {
                                var aax = ax + GetDirOffsetX(1);
                                var aay = ay + GetDirOffsetY(1);
                                var aai = (int)chf.Cells[aax + (aay * w)].Index + GetCon(@as, 1);
                                if (src[aai] + 3 < src[i])
                                {
                                    src[i] = (ushort)(src[aai] + 3);
                                }
                            }
                        }

                        if (GetCon(s, 1) != RCNotConnected)
                        {
                            // (0,1)
                            var ax = x + GetDirOffsetX(1);
                            var ay = y + GetDirOffsetY(1);
                            var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(s, 1);
                            var @as = chf.Spans[ai];
                            if (src[ai] + 2 < src[i])
                            {
                                src[i] = (ushort)(src[ai] + 2);
                            }

                            // (-1,1)
                            if (GetCon(@as, 0) != RCNotConnected)
                            {
                                var aax = ax + GetDirOffsetX(0);
                                var aay = ay + GetDirOffsetY(0);
                                var aai = (int)chf.Cells[aax + (aay * w)].Index + GetCon(@as, 0);
                                if (src[aai] + 3 < src[i])
                                {
                                    src[i] = (ushort)(src[aai] + 3);
                                }
                            }
                        }
                    }
                }
            }

            maxDist = 0;
            for (var i = 0; i < chf.SpanCount; ++i)
            {
                maxDist = (ushort)math.max((int)src[i], maxDist);
            }
        }

        private static ushort* BoxBlur(in RcCompactHeightfield chf, int thr, ushort* src, ushort* dst)
        {
            var w = chf.Width;
            var h = chf.Height;

            thr *= 2;

            for (var y = 0; y < h; ++y)
            {
                for (var x = 0; x < w; ++x)
                {
                    var c = chf.Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        var s = chf.Spans[i];
                        var cd = src[i];
                        if (cd <= thr)
                        {
                            dst[i] = cd;
                            continue;
                        }

                        int d = cd;
                        for (var dir = 0; dir < 4; ++dir)
                        {
                            if (GetCon(s, dir) != RCNotConnected)
                            {
                                var ax = x + GetDirOffsetX(dir);
                                var ay = y + GetDirOffsetY(dir);
                                var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(s, dir);
                                d += src[ai];

                                var @as = chf.Spans[ai];
                                var dir2 = (dir + 1) & 0x3;
                                if (GetCon(@as, dir2) != RCNotConnected)
                                {
                                    var ax2 = ax + GetDirOffsetX(dir2);
                                    var ay2 = ay + GetDirOffsetY(dir2);
                                    var ai2 = (int)chf.Cells[ax2 + (ay2 * w)].Index + GetCon(@as, dir2);
                                    d += src[ai2];
                                }
                                else
                                {
                                    d += cd;
                                }
                            }
                            else
                            {
                                d += cd * 2;
                            }
                        }

                        dst[i] = (ushort)((d + 5) / 9);
                    }
                }
            }

            return dst;
        }

        private static bool FloodRegion(int x, int y, int i, ushort level, ushort r, in RcCompactHeightfield chf,
            ushort* srcReg, ushort* srcDist, NativeList<LevelStackEntry> stack)
        {
            var w = chf.Width;

            var area = chf.Areas[i];

            // Flood fill mark region
            stack.Clear();
            stack.Add(new LevelStackEntry(x, y, i));
            srcReg[i] = r;
            srcDist[i] = 0;

            var lev = level >= 2 ? (ushort)(level - 2) : (ushort)0;
            var count = 0;

            while (stack.Length > 0)
            {
                var back = stack[stack.Length - 1];
                stack.RemoveAt(stack.Length - 1);
                var cx = back.X;
                var cy = back.Y;
                var ci = back.Index;

                var cs = chf.Spans[ci];

                // Check if any of the neighbours already have a valid region set
                ushort ar = 0;
                for (var dir = 0; dir < 4; ++dir)
                {
                    // 8 connected
                    if (GetCon(cs, dir) != RCNotConnected)
                    {
                        var ax = cx + GetDirOffsetX(dir);
                        var ay = cy + GetDirOffsetY(dir);
                        var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(cs, dir);
                        if (chf.Areas[ai] != area)
                        {
                            continue;
                        }

                        var nr = srcReg[ai];

                        // Do not take borders into account
                        if ((nr & RCBorderReg) != 0)
                        {
                            continue;
                        }

                        if (nr != 0 && nr != r)
                        {
                            ar = nr;
                            break;
                        }

                        var @as = chf.Spans[ai];

                        var dir2 = (dir + 1) & 0x3;
                        if (GetCon(@as, dir2) != RCNotConnected)
                        {
                            var ax2 = ax + GetDirOffsetX(dir2);
                            var ay2 = ay + GetDirOffsetY(dir2);
                            var ai2 = (int)chf.Cells[ax2 + (ay2 * w)].Index + GetCon(@as, dir2);
                            if (chf.Areas[ai2] != area)
                            {
                                continue;
                            }

                            var nr2 = srcReg[ai2];
                            if (nr2 != 0 && nr2 != r)
                            {
                                ar = nr2;
                                break;
                            }
                        }
                    }
                }

                if (ar != 0)
                {
                    srcReg[ci] = 0;
                    continue;
                }

                count++;

                // Expand neighbours
                for (var dir = 0; dir < 4; ++dir)
                {
                    if (GetCon(cs, dir) != RCNotConnected)
                    {
                        var ax = cx + GetDirOffsetX(dir);
                        var ay = cy + GetDirOffsetY(dir);
                        var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(cs, dir);
                        if (chf.Areas[ai] != area)
                        {
                            continue;
                        }

                        if (chf.Dist[ai] >= lev && srcReg[ai] == 0)
                        {
                            srcReg[ai] = r;
                            srcDist[ai] = 0;
                            stack.Add(new LevelStackEntry(ax, ay, ai));
                        }
                    }
                }
            }

            return count > 0;
        }

        private static void ExpandRegions(int maxIter, ushort level, in RcCompactHeightfield chf,
            ushort* srcReg, ushort* srcDist, NativeList<LevelStackEntry> stack, bool fillStack)
        {
            var w = chf.Width;
            var h = chf.Height;

            if (fillStack)
            {
                // Find cells revealed by the raised level
                stack.Clear();
                for (var y = 0; y < h; ++y)
                {
                    for (var x = 0; x < w; ++x)
                    {
                        var c = chf.Cells[x + (y * w)];
                        for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                        {
                            if (chf.Dist[i] >= level && srcReg[i] == 0 && chf.Areas[i] != RCNullArea)
                            {
                                stack.Add(new LevelStackEntry(x, y, i));
                            }
                        }
                    }
                }
            }
            else
            {
                // use cells in the input stack
                // mark all cells which already have a region
                for (var j = 0; j < stack.Length; j++)
                {
                    var entry = stack[j];
                    var i = entry.Index;
                    if (srcReg[i] != 0)
                    {
                        entry.Index = -1;
                        stack[j] = entry;
                    }
                }
            }

            var dirtyEntries = new NativeList<DirtyEntry>(256, Allocator.Temp);
            var iter = 0;
            while (stack.Length > 0)
            {
                var failed = 0;
                dirtyEntries.Clear();

                for (var j = 0; j < stack.Length; j++)
                {
                    var entry = stack[j];
                    var x = entry.X;
                    var y = entry.Y;
                    var i = entry.Index;
                    if (i < 0)
                    {
                        failed++;
                        continue;
                    }

                    var r = srcReg[i];
                    ushort d2 = 0xffff;
                    var area = chf.Areas[i];
                    var s = chf.Spans[i];
                    for (var dir = 0; dir < 4; ++dir)
                    {
                        if (GetCon(s, dir) == RCNotConnected)
                        {
                            continue;
                        }

                        var ax = x + GetDirOffsetX(dir);
                        var ay = y + GetDirOffsetY(dir);
                        var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(s, dir);
                        if (chf.Areas[ai] != area)
                        {
                            continue;
                        }

                        if (srcReg[ai] > 0 && (srcReg[ai] & RCBorderReg) == 0)
                        {
                            if (srcDist[ai] + 2 < d2)
                            {
                                r = srcReg[ai];
                                d2 = (ushort)(srcDist[ai] + 2);
                            }
                        }
                    }

                    if (r != 0)
                    {
                        entry.Index = -1; // mark as used
                        stack[j] = entry;
                        dirtyEntries.Add(new DirtyEntry(i, r, d2));
                    }
                    else
                    {
                        failed++;
                    }
                }

                // Copy entries that differ between src and dst to keep them in sync
                for (var i = 0; i < dirtyEntries.Length; i++)
                {
                    var entry = dirtyEntries[i];
                    var idx = entry.Index;
                    srcReg[idx] = entry.Region;
                    srcDist[idx] = entry.Distance2;
                }

                if (failed == stack.Length)
                {
                    break;
                }

                if (level > 0)
                {
                    ++iter;
                    if (iter >= maxIter)
                    {
                        break;
                    }
                }
            }
        }

        private static void SortCellsByLevel(ushort startLevel, in RcCompactHeightfield chf, ushort* srcReg,
            int nbStacks, NativeArray<NativeList<LevelStackEntry>> stacks, ushort loglevelsPerStack)
        {
            var w = chf.Width;
            var h = chf.Height;
            startLevel = (ushort)(startLevel >> loglevelsPerStack);

            for (var j = 0; j < nbStacks; ++j)
            {
                stacks[j].Clear();
            }

            // put all cells in the level range into the appropriate stacks
            for (var y = 0; y < h; ++y)
            {
                for (var x = 0; x < w; ++x)
                {
                    var c = chf.Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        if (chf.Areas[i] == RCNullArea || srcReg[i] != 0)
                        {
                            continue;
                        }

                        var level = chf.Dist[i] >> loglevelsPerStack;
                        var sId = startLevel - level;
                        if (sId >= nbStacks)
                        {
                            continue;
                        }

                        if (sId < 0)
                        {
                            sId = 0;
                        }

                        stacks[sId].Add(new LevelStackEntry(x, y, i));
                    }
                }
            }
        }

        private static void AppendStacks(NativeList<LevelStackEntry> srcStack, NativeList<LevelStackEntry> dstStack, ushort* srcReg)
        {
            for (var j = 0; j < srcStack.Length; j++)
            {
                var entry = srcStack[j];
                var i = entry.Index;
                if (i < 0 || srcReg[i] != 0)
                {
                    continue;
                }

                dstStack.Add(entry);
            }
        }

        private static void RemoveAdjacentNeighbours(ref RcRegion reg)
        {
            // Remove adjacent duplicates
            for (var i = 0; i < reg.Connections.Length && reg.Connections.Length > 1;)
            {
                var ni = (i + 1) % reg.Connections.Length;
                if (reg.Connections[i] == reg.Connections[ni])
                {
                    // Remove duplicate
                    for (var j = i; j < reg.Connections.Length - 1; ++j)
                    {
                        reg.Connections[j] = reg.Connections[j + 1];
                    }

                    reg.Connections.RemoveAt(reg.Connections.Length - 1);
                }
                else
                {
                    ++i;
                }
            }
        }

        private static void ReplaceNeighbour(ref RcRegion reg, ushort oldId, ushort newId)
        {
            var neiChanged = false;
            for (var i = 0; i < reg.Connections.Length; ++i)
            {
                if (reg.Connections[i] == oldId)
                {
                    reg.Connections[i] = newId;
                    neiChanged = true;
                }
            }

            for (var i = 0; i < reg.Floors.Length; ++i)
            {
                if (reg.Floors[i] == oldId)
                {
                    reg.Floors[i] = newId;
                }
            }

            if (neiChanged)
            {
                RemoveAdjacentNeighbours(ref reg);
            }
        }

        private static bool CanMergeWithRegion(in RcRegion rega, in RcRegion regb)
        {
            if (rega.AreaType != regb.AreaType)
            {
                return false;
            }

            var n = 0;
            for (var i = 0; i < rega.Connections.Length; ++i)
            {
                if (rega.Connections[i] == regb.ID)
                {
                    n++;
                }
            }

            if (n > 1)
            {
                return false;
            }

            for (var i = 0; i < rega.Floors.Length; ++i)
            {
                if (rega.Floors[i] == regb.ID)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddUniqueFloorRegion(ref RcRegion reg, int n)
        {
            for (var i = 0; i < reg.Floors.Length; ++i)
            {
                if (reg.Floors[i] == n)
                {
                    return;
                }
            }

            reg.Floors.Add(n);
        }

        private static bool MergeRegions(ref RcRegion rega, ref RcRegion regb)
        {
            var aid = rega.ID;
            var bid = regb.ID;

            // Duplicate current neighbourhood
            var acon = new NativeList<int>(rega.Connections.Length, Allocator.Temp);
            for (var i = 0; i < rega.Connections.Length; ++i)
            {
                acon.Add(rega.Connections[i]);
            }

            var bcon = regb.Connections;

            // Find insertion point on A
            var insa = -1;
            for (var i = 0; i < acon.Length; ++i)
            {
                if (acon[i] == bid)
                {
                    insa = i;
                    break;
                }
            }

            if (insa == -1)
            {
                return false;
            }

            // Find insertion point on B
            var insb = -1;
            for (var i = 0; i < bcon.Length; ++i)
            {
                if (bcon[i] == aid)
                {
                    insb = i;
                    break;
                }
            }

            if (insb == -1)
            {
                return false;
            }

            // Merge neighbours
            rega.Connections.Clear();
            for (int i = 0, ni = acon.Length; i < ni - 1; ++i)
            {
                rega.Connections.Add(acon[(insa + 1 + i) % ni]);
            }

            for (int i = 0, ni = bcon.Length; i < ni - 1; ++i)
            {
                rega.Connections.Add(bcon[(insb + 1 + i) % ni]);
            }

            RemoveAdjacentNeighbours(ref rega);

            for (var j = 0; j < regb.Floors.Length; ++j)
            {
                AddUniqueFloorRegion(ref rega, regb.Floors[j]);
            }

            rega.SpanCount += regb.SpanCount;
            regb.SpanCount = 0;
            regb.Connections.Clear();

            return true;
        }

        private static bool IsRegionConnectedToBorder(in RcRegion reg)
        {
            // Region is connected to border if one of the neighbours is null id
            for (var i = 0; i < reg.Connections.Length; ++i)
            {
                if (reg.Connections[i] == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSolidEdge(in RcCompactHeightfield chf, ushort* srcReg, int x, int y, int i, int dir)
        {
            var s = chf.Spans[i];
            ushort r = 0;
            if (GetCon(s, dir) != RCNotConnected)
            {
                var ax = x + GetDirOffsetX(dir);
                var ay = y + GetDirOffsetY(dir);
                var ai = (int)chf.Cells[ax + (ay * chf.Width)].Index + GetCon(s, dir);
                r = srcReg[ai];
            }

            if (r == srcReg[i])
            {
                return false;
            }

            return true;
        }

        private static void WalkContour(int x, int y, int i, int dir, in RcCompactHeightfield chf, ushort* srcReg, NativeList<int> cont)
        {
            var startDir = dir;
            var starti = i;

            var ss = chf.Spans[i];
            ushort curReg = 0;
            if (GetCon(ss, dir) != RCNotConnected)
            {
                var ax = x + GetDirOffsetX(dir);
                var ay = y + GetDirOffsetY(dir);
                var ai = (int)chf.Cells[ax + (ay * chf.Width)].Index + GetCon(ss, dir);
                curReg = srcReg[ai];
            }

            cont.Add(curReg);

            var iter = 0;
            while (++iter < 40000)
            {
                var s = chf.Spans[i];

                if (IsSolidEdge(chf, srcReg, x, y, i, dir))
                {
                    // Choose the edge corner
                    ushort r = 0;
                    if (GetCon(s, dir) != RCNotConnected)
                    {
                        var ax = x + GetDirOffsetX(dir);
                        var ay = y + GetDirOffsetY(dir);
                        var ai = (int)chf.Cells[ax + (ay * chf.Width)].Index + GetCon(s, dir);
                        r = srcReg[ai];
                    }

                    if (r != curReg)
                    {
                        curReg = r;
                        cont.Add(curReg);
                    }

                    dir = (dir + 1) & 0x3; // Rotate CW
                }
                else
                {
                    var ni = -1;
                    var nx = x + GetDirOffsetX(dir);
                    var ny = y + GetDirOffsetY(dir);
                    if (GetCon(s, dir) != RCNotConnected)
                    {
                        var nc = chf.Cells[nx + (ny * chf.Width)];
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
                    dir = (dir + 3) & 0x3; // Rotate CCW
                }

                if (starti == i && startDir == dir)
                {
                    break;
                }
            }

            // Remove adjacent duplicates
            if (cont.Length > 1)
            {
                for (var j = 0; j < cont.Length;)
                {
                    var nj = (j + 1) % cont.Length;
                    if (cont[j] == cont[nj])
                    {
                        for (var k = j; k < cont.Length - 1; ++k)
                        {
                            cont[k] = cont[k + 1];
                        }

                        cont.RemoveAt(cont.Length - 1);
                    }
                    else
                    {
                        ++j;
                    }
                }
            }
        }

        private static void MergeAndFilterRegions(int minRegionArea, int mergeRegionSize, ref ushort maxRegionId,
            in RcCompactHeightfield chf, ushort* srcReg, NativeList<int> overlaps)
        {
            var w = chf.Width;
            var h = chf.Height;

            var nreg = maxRegionId + 1;
            var regions = new NativeArray<RcRegion>(nreg, Allocator.Temp);

            // Construct regions
            for (var i = 0; i < nreg; ++i)
            {
                regions[i] = new RcRegion((ushort)i);
            }

            // Find edge of a region and find connections around the contour
            for (var y = 0; y < h; ++y)
            {
                for (var x = 0; x < w; ++x)
                {
                    var c = chf.Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        var r = srcReg[i];
                        if (r == 0 || r >= nreg)
                        {
                            continue;
                        }

                        var reg = regions[r];
                        reg.SpanCount++;

                        // Update floors
                        for (var j = (int)c.Index; j < ni; ++j)
                        {
                            if (i == j)
                            {
                                continue;
                            }

                            var floorId = srcReg[j];
                            if (floorId == 0 || floorId >= nreg)
                            {
                                continue;
                            }

                            if (floorId == r)
                            {
                                reg.Overlap = true;
                            }

                            AddUniqueFloorRegion(ref reg, floorId);
                        }

                        // Have found contour
                        if (reg.Connections.Length > 0)
                        {
                            regions[r] = reg;
                            continue;
                        }

                        reg.AreaType = chf.Areas[i];

                        // Check if this cell is next to a border
                        var ndir = -1;
                        for (var dir = 0; dir < 4; ++dir)
                        {
                            if (IsSolidEdge(chf, srcReg, x, y, i, dir))
                            {
                                ndir = dir;
                                break;
                            }
                        }

                        if (ndir != -1)
                        {
                            // The cell is at border. Walk around the contour to find all the neighbours
                            WalkContour(x, y, i, ndir, chf, srcReg, reg.Connections);
                        }

                        regions[r] = reg;
                    }
                }
            }

            // Remove too small regions
            var stack = new NativeList<int>(32, Allocator.Temp);
            var trace = new NativeList<int>(32, Allocator.Temp);
            for (var i = 0; i < nreg; ++i)
            {
                var reg = regions[i];
                if (reg.ID == 0 || (reg.ID & RCBorderReg) != 0)
                {
                    continue;
                }

                if (reg.SpanCount == 0)
                {
                    continue;
                }

                if (reg.Visited)
                {
                    continue;
                }

                // Count the total size of all the connected regions
                // Also keep track of the regions connects to a tile border
                var connectsToBorder = false;
                var spanCount = 0;
                stack.Clear();
                trace.Clear();

                reg.Visited = true;
                stack.Add(i);

                while (stack.Length > 0)
                {
                    // Pop
                    var ri = stack[stack.Length - 1];
                    stack.RemoveAt(stack.Length - 1);

                    var creg = regions[ri];

                    spanCount += creg.SpanCount;
                    trace.Add(ri);

                    for (var j = 0; j < creg.Connections.Length; ++j)
                    {
                        if ((creg.Connections[j] & RCBorderReg) != 0)
                        {
                            connectsToBorder = true;
                            continue;
                        }

                        var neireg = regions[creg.Connections[j]];
                        if (neireg.Visited)
                        {
                            continue;
                        }

                        if (neireg.ID == 0 || (neireg.ID & RCBorderReg) != 0)
                        {
                            continue;
                        }

                        // Visit
                        stack.Add(neireg.ID);
                        neireg.Visited = true;
                        regions[creg.Connections[j]] = neireg;
                    }
                }

                // If the accumulated regions size is too small, remove it
                if (spanCount < minRegionArea && !connectsToBorder)
                {
                    // Kill all visited regions
                    for (var j = 0; j < trace.Length; ++j)
                    {
                        var killReg = regions[trace[j]];
                        killReg.SpanCount = 0;
                        killReg.ID = 0;
                        regions[trace[j]] = killReg;
                    }
                }
            }

            // Merge too small regions to neighbour regions
            int mergeCount;
            do
            {
                mergeCount = 0;
                for (var i = 0; i < nreg; ++i)
                {
                    var reg = regions[i];
                    if (reg.ID == 0 || (reg.ID & RCBorderReg) != 0)
                    {
                        continue;
                    }

                    if (reg.Overlap)
                    {
                        continue;
                    }

                    if (reg.SpanCount == 0)
                    {
                        continue;
                    }

                    // Check to see if the region should be merged
                    if (reg.SpanCount > mergeRegionSize && IsRegionConnectedToBorder(reg))
                    {
                        continue;
                    }

                    // Small region with more than 1 connection or region which is not connected to a border at all
                    // Find smallest neighbour region that connects to this one
                    var smallest = 0x0fffffff;
                    var mergeId = reg.ID;
                    for (var j = 0; j < reg.Connections.Length; ++j)
                    {
                        if ((reg.Connections[j] & RCBorderReg) != 0)
                        {
                            continue;
                        }

                        var mreg = regions[reg.Connections[j]];
                        if (mreg.ID == 0 || (mreg.ID & RCBorderReg) != 0 || mreg.Overlap)
                        {
                            continue;
                        }

                        if (mreg.SpanCount < smallest &&
                            CanMergeWithRegion(reg, mreg) &&
                            CanMergeWithRegion(mreg, reg))
                        {
                            smallest = mreg.SpanCount;
                            mergeId = mreg.ID;
                        }
                    }

                    // Found new id
                    if (mergeId != reg.ID)
                    {
                        var oldId = reg.ID;
                        var target = regions[mergeId];

                        // Merge neighbours
                        if (MergeRegions(ref target, ref reg))
                        {
                            regions[mergeId] = target;
                            regions[i] = reg;

                            // Fixup regions pointing to current region
                            for (var j = 0; j < nreg; ++j)
                            {
                                var fixReg = regions[j];
                                if (fixReg.ID == 0 || (fixReg.ID & RCBorderReg) != 0)
                                {
                                    continue;
                                }

                                // If another region was already merged into current region change the nid of the previous region too
                                if (fixReg.ID == oldId)
                                {
                                    fixReg.ID = mergeId;
                                    regions[j] = fixReg;
                                }

                                // Replace the current region with the new one if the current regions is neighbour
                                ReplaceNeighbour(ref fixReg, oldId, mergeId);
                                regions[j] = fixReg;
                            }

                            mergeCount++;
                        }
                    }
                }
            }
            while (mergeCount > 0);

            // Compress region Ids
            for (var i = 0; i < nreg; ++i)
            {
                var reg = regions[i];
                reg.Remap = false;
                if (reg.ID == 0)
                {
                    continue; // Skip nil regions
                }

                if ((reg.ID & RCBorderReg) != 0)
                {
                    continue; // Skip external regions
                }

                reg.Remap = true;
                regions[i] = reg;
            }

            ushort regIdGen = 0;
            for (var i = 0; i < nreg; ++i)
            {
                var reg = regions[i];
                if (!reg.Remap)
                {
                    continue;
                }

                var oldId = reg.ID;
                var newId = ++regIdGen;
                for (var j = i; j < nreg; ++j)
                {
                    var updateReg = regions[j];
                    if (updateReg.ID == oldId)
                    {
                        updateReg.ID = newId;
                        updateReg.Remap = false;
                        regions[j] = updateReg;
                    }
                }
            }

            maxRegionId = regIdGen;

            // Remap regions
            for (var i = 0; i < chf.SpanCount; ++i)
            {
                if ((srcReg[i] & RCBorderReg) == 0)
                {
                    srcReg[i] = regions[srcReg[i]].ID;
                }
            }

            // Return regions that we found to be overlapping
            for (var i = 0; i < nreg; ++i)
            {
                if (regions[i].Overlap)
                {
                    overlaps.Add(regions[i].ID);
                }
            }
        }

        private static void AddUniqueConnection(ref RcRegion reg, int n)
        {
            foreach (var connection in reg.Connections)
            {
                if (connection == n)
                {
                    return;
                }
            }

            reg.Connections.Add(n);
        }

        private static bool MergeAndFilterLayerRegions(int minRegionArea, ref ushort maxRegionId, in RcCompactHeightfield chf, ushort* srcReg)
        {
            var w = chf.Width;
            var h = chf.Height;

            var nreg = maxRegionId + 1;
            var regions = new NativeArray<RcRegion>(nreg, Allocator.Temp);

            // Construct regions
            for (var i = 0; i < nreg; ++i)
            {
                regions[i] = new RcRegion((ushort)i);
            }

            // Find region neighbours and overlapping regions
            var lregs = new NativeList<int>(32, Allocator.Temp);
            for (var y = 0; y < h; ++y)
            {
                for (var x = 0; x < w; ++x)
                {
                    var c = chf.Cells[x + (y * w)];

                    lregs.Clear();

                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        var s = chf.Spans[i];
                        var area = chf.Areas[i];
                        var ri = srcReg[i];
                        if (ri == 0 || ri >= nreg)
                        {
                            continue;
                        }

                        var reg = regions[ri];

                        reg.SpanCount++;
                        reg.AreaType = area;

                        reg.Ymin = (ushort)math.min((int)reg.Ymin, s.Y);
                        reg.Ymax = (ushort)math.max((int)reg.Ymax, s.Y);

                        // Collect all region layers
                        lregs.Add(ri);

                        // Update neighbours
                        for (var dir = 0; dir < 4; ++dir)
                        {
                            if (GetCon(s, dir) != RCNotConnected)
                            {
                                var ax = x + GetDirOffsetX(dir);
                                var ay = y + GetDirOffsetY(dir);
                                var ai = (int)chf.Cells[ax + (ay * w)].Index + GetCon(s, dir);
                                var rai = srcReg[ai];
                                if (rai > 0 && rai < nreg && rai != ri)
                                {
                                    AddUniqueConnection(ref reg, rai);
                                }

                                if ((rai & RCBorderReg) != 0)
                                {
                                    reg.ConnectsToBorder = true;
                                }
                            }
                        }

                        regions[ri] = reg;
                    }

                    // Update overlapping regions
                    for (var i = 0; i < lregs.Length - 1; ++i)
                    {
                        for (var j = i + 1; j < lregs.Length; ++j)
                        {
                            if (lregs[i] != lregs[j])
                            {
                                var ri = regions[lregs[i]];
                                var rj = regions[lregs[j]];
                                AddUniqueFloorRegion(ref ri, lregs[j]);
                                AddUniqueFloorRegion(ref rj, lregs[i]);
                                regions[lregs[i]] = ri;
                                regions[lregs[j]] = rj;
                            }
                        }
                    }
                }
            }

            // Create 2D layers from regions
            ushort layerId = 1;

            for (var i = 0; i < nreg; ++i)
            {
                var reg = regions[i];
                reg.ID = 0;
                regions[i] = reg;
            }

            // Merge monotone regions to create non-overlapping areas
            var stack = new NativeList<int>(32, Allocator.Temp);
            for (var i = 1; i < nreg; ++i)
            {
                var root = regions[i];

                // Skip already visited
                if (root.ID != 0)
                {
                    continue;
                }

                // Start search
                root.ID = layerId;
                regions[i] = root;

                stack.Clear();
                stack.Add(i);

                while (stack.Length > 0)
                {
                    var regIdx = stack[stack.Length - 1];
                    stack.RemoveAt(stack.Length - 1);

                    var reg = regions[regIdx];

                    var ncons = reg.Connections.Length;
                    for (var j = 0; j < ncons; ++j)
                    {
                        var nei = reg.Connections[j];
                        var regn = regions[nei];

                        // Skip already visited
                        if (regn.ID != 0)
                        {
                            continue;
                        }

                        // Skip if different area type, do not connect regions with different area type
                        if (reg.AreaType != regn.AreaType)
                        {
                            continue;
                        }

                        // Skip if the neighbour is overlapping root region
                        var overlap = false;
                        for (var k = 0; k < root.Floors.Length; k++)
                        {
                            if (root.Floors[k] == nei)
                            {
                                overlap = true;
                                break;
                            }
                        }

                        if (overlap)
                        {
                            continue;
                        }

                        // Deepen
                        stack.Add(nei);

                        // Mark layer id
                        regn.ID = layerId;
                        regions[nei] = regn;

                        // Merge current layers to root
                        for (var k = 0; k < regn.Floors.Length; ++k)
                        {
                            AddUniqueFloorRegion(ref root, regn.Floors[k]);
                        }

                        root.Ymin = (ushort)math.min((int)root.Ymin, regn.Ymin);
                        root.Ymax = (ushort)math.max((int)root.Ymax, regn.Ymax);
                        root.SpanCount += regn.SpanCount;
                        regn.SpanCount = 0;
                        root.ConnectsToBorder = root.ConnectsToBorder || regn.ConnectsToBorder;
                        regions[i] = root;
                        regions[nei] = regn;
                    }
                }

                layerId++;
            }

            // Remove small regions
            for (var i = 0; i < nreg; ++i)
            {
                var reg = regions[i];
                if (reg.SpanCount > 0 && reg.SpanCount < minRegionArea && !reg.ConnectsToBorder)
                {
                    var regId = reg.ID;
                    for (var j = 0; j < nreg; ++j)
                    {
                        var killReg = regions[j];
                        if (killReg.ID == regId)
                        {
                            killReg.ID = 0;
                            regions[j] = killReg;
                        }
                    }
                }
            }

            // Compress region Ids
            for (var i = 0; i < nreg; ++i)
            {
                var reg = regions[i];
                reg.Remap = false;
                if (reg.ID == 0)
                {
                    continue; // Skip nil regions
                }

                if ((reg.ID & RCBorderReg) != 0)
                {
                    continue; // Skip external regions
                }

                reg.Remap = true;
                regions[i] = reg;
            }

            ushort regIdGen = 0;
            for (var i = 0; i < nreg; ++i)
            {
                var reg = regions[i];
                if (!reg.Remap)
                {
                    continue;
                }

                var oldId = reg.ID;
                var newId = ++regIdGen;
                for (var j = i; j < nreg; ++j)
                {
                    var updateReg = regions[j];
                    if (updateReg.ID == oldId)
                    {
                        updateReg.ID = newId;
                        updateReg.Remap = false;
                        regions[j] = updateReg;
                    }
                }
            }

            maxRegionId = regIdGen;

            // Remap regions
            for (var i = 0; i < chf.SpanCount; ++i)
            {
                if ((srcReg[i] & RCBorderReg) == 0)
                {
                    srcReg[i] = regions[srcReg[i]].ID;
                }
            }

            return true;
        }

        private static void PaintRectRegion(int minx, int maxx, int miny, int maxy, ushort regId, in RcCompactHeightfield chf, ushort* srcReg)
        {
            var w = chf.Width;
            for (var y = miny; y < maxy; ++y)
            {
                for (var x = minx; x < maxx; ++x)
                {
                    var c = chf.Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        if (chf.Areas[i] != RCNullArea)
                        {
                            srcReg[i] = regId;
                        }
                    }
                }
            }
        }
    }
}
