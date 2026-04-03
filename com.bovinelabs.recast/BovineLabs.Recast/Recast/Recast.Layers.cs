// <copyright file="Recast.Layers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Recast heightfield layer building functions for generating layered heightfields.
    /// </summary>
    public static unsafe partial class Recast
    {
        // Must be 255 or smaller (not 256) because layer IDs are stored as
        // a byte where 255 is a special value
        private const int RCMaxLayers = 63;
        private const int RCMaxNeis = 16;

        /// <summary>
        /// Builds heightfield layers from the provided compact heightfield.
        /// </summary>
        /// <param name="compactHeightfield">A fully built compact heightfield.</param>
        /// <param name="borderSize">The size of the non-navigable border around the heightfield [Limit: >=0] [Units: vx].</param>
        /// <param name="walkableHeight">Minimum floor to 'ceiling' height that will still allow the floor area to be considered walkable [Limit: >= 3] [Units: vx].</param>
        /// <param name="layerSet">The resulting heightfield layer set.</param>
        /// <returns>True if the operation completed successfully.</returns>
        public static bool BuildHeightfieldLayers(RcCompactHeightfield* compactHeightfield, int borderSize, int walkableHeight,
            RcHeightfieldLayerSet* layerSet)
        {
            var w = compactHeightfield->Width;
            var h = compactHeightfield->Height;

            var srcReg = (byte*)AllocatorManager.Allocate(Allocator.Temp, sizeof(byte) * compactHeightfield->SpanCount, UnsafeUtility.AlignOf<byte>());
            UnsafeUtility.MemSet(srcReg, 0xff, compactHeightfield->SpanCount);

            var nsweeps = compactHeightfield->Width;
            var sweeps = (LayerSweepSpan*)AllocatorManager.Allocate(Allocator.Temp, sizeof(LayerSweepSpan) * nsweeps, UnsafeUtility.AlignOf<LayerSweepSpan>());

            // Partition walkable area into monotone regions
            var prevCount = stackalloc int[256];
            byte regId = 0;

            for (var y = borderSize; y < h - borderSize; ++y)
            {
                UnsafeUtility.MemClear(prevCount, sizeof(int) * regId);
                byte sweepId = 0;

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

                        byte sid = 0xff;

                        // -x
                        if (GetCon(s, 0) != RCNotConnected)
                        {
                            var ax = x + GetDirOffsetX(0);
                            var ay = y + GetDirOffsetY(0);
                            var ai = (int)compactHeightfield->Cells[ax + (ay * w)].Index + GetCon(s, 0);
                            if (compactHeightfield->Areas[ai] != RCNullArea && srcReg[ai] != 0xff)
                            {
                                sid = srcReg[ai];
                            }
                        }

                        if (sid == 0xff)
                        {
                            sid = sweepId++;
                            sweeps[sid].Nei = 0xff;
                            sweeps[sid].Ns = 0;
                        }

                        // -y
                        if (GetCon(s, 3) != RCNotConnected)
                        {
                            var ax = x + GetDirOffsetX(3);
                            var ay = y + GetDirOffsetY(3);
                            var ai = (int)compactHeightfield->Cells[ax + (ay * w)].Index + GetCon(s, 3);
                            var nr = srcReg[ai];
                            if (nr != 0xff)
                            {
                                // Set neighbour when first valid neighbour is encountered
                                if (sweeps[sid].Ns == 0)
                                {
                                    sweeps[sid].Nei = nr;
                                }

                                if (sweeps[sid].Nei == nr)
                                {
                                    // Update existing neighbour
                                    sweeps[sid].Ns++;
                                    prevCount[nr]++;
                                }
                                else
                                {
                                    // This is hit if there is more than one neighbour.
                                    // Invalidate the neighbour
                                    sweeps[sid].Nei = 0xff;
                                }
                            }
                        }

                        srcReg[i] = sid;
                    }
                }

                // Create unique ID
                for (var i = 0; i < sweepId; ++i)
                {
                    // If the neighbour is set and there is only one continuous connection to it,
                    // the sweep will be merged with the previous one, else new region is created
                    if (sweeps[i].Nei != 0xff && prevCount[sweeps[i].Nei] == sweeps[i].Ns)
                    {
                        sweeps[i].ID = sweeps[i].Nei;
                    }
                    else
                    {
                        if (regId == 255)
                        {
                            return false;
                        }

                        sweeps[i].ID = regId++;
                    }
                }

                // Remap local sweep ids to region ids
                for (var x = borderSize; x < w - borderSize; ++x)
                {
                    var c = compactHeightfield->Cells[x + (y * w)];
                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        if (srcReg[i] != 0xff)
                        {
                            srcReg[i] = sweeps[srcReg[i]].ID;
                        }
                    }
                }
            }

            // Allocate and init layer regions
            int nregs = regId;
            var regs = (LayerRegion*)AllocatorManager.Allocate(Allocator.Temp, sizeof(LayerRegion) * nregs, UnsafeUtility.AlignOf<LayerRegion>());

            UnsafeUtility.MemClear(regs, sizeof(LayerRegion) * nregs);
            for (var i = 0; i < nregs; ++i)
            {
                regs[i].LayerId = 0xff;
                regs[i].Ymin = 0xffff;
                regs[i].Ymax = 0;
            }

            var lregs = stackalloc byte[RCMaxLayers];

            // Find region neighbours and overlapping regions
            for (var y = 0; y < h; ++y)
            {
                for (var x = 0; x < w; ++x)
                {
                    var c = compactHeightfield->Cells[x + (y * w)];

                    var nlregs = 0;

                    for (int i = (int)c.Index, ni = (int)(c.Index + c.Count); i < ni; ++i)
                    {
                        var s = compactHeightfield->Spans[i];
                        var ri = srcReg[i];
                        if (ri == 0xff)
                        {
                            continue;
                        }

                        regs[ri].Ymin = (ushort)math.min((int)regs[ri].Ymin, s.Y);
                        regs[ri].Ymax = (ushort)math.max((int)regs[ri].Ymax, s.Y);

                        // Collect all region layers
                        if (nlregs < RCMaxLayers)
                        {
                            lregs[nlregs++] = ri;
                        }

                        // Update neighbours
                        for (var dir = 0; dir < 4; ++dir)
                        {
                            if (GetCon(s, dir) != RCNotConnected)
                            {
                                var ax = x + GetDirOffsetX(dir);
                                var ay = y + GetDirOffsetY(dir);
                                var ai = (int)compactHeightfield->Cells[ax + (ay * w)].Index + GetCon(s, dir);
                                var rai = srcReg[ai];
                                if (rai != 0xff && rai != ri)
                                {
                                    AddUnique(regs[ri].Neis, &regs[ri].Nneis, RCMaxNeis, rai);
                                }
                            }
                        }
                    }

                    // Update overlapping regions
                    for (var i = 0; i < nlregs - 1; ++i)
                    {
                        for (var j = i + 1; j < nlregs; ++j)
                        {
                            if (lregs[i] != lregs[j])
                            {
                                var ri = &regs[lregs[i]];
                                var rj = &regs[lregs[j]];

                                if (!AddUnique(ri->Layers, &ri->Nlayers, RCMaxLayers, lregs[j]) ||
                                    !AddUnique(rj->Layers, &rj->Nlayers, RCMaxLayers, lregs[i]))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            // Create 2D layers from regions
            byte layerId = 0;

            const int maxStack = 64;
            var stack = stackalloc byte[maxStack];

            for (var i = 0; i < nregs; ++i)
            {
                var root = &regs[i];

                // Skip already visited
                if (root->LayerId != 0xff)
                {
                    continue;
                }

                // Start search
                root->LayerId = layerId;
                root->Base = 1;

                var nStack = 0;
                stack[nStack++] = (byte)i;

                while (nStack > 0)
                {
                    // Pop front
                    var reg = &regs[stack[0]];
                    nStack--;
                    for (var j = 0; j < nStack; ++j)
                    {
                        stack[j] = stack[j + 1];
                    }

                    int nneis = reg->Nneis;
                    for (var j = 0; j < nneis; ++j)
                    {
                        var nei = reg->Neis[j];
                        var regn = &regs[nei];

                        // Skip already visited
                        if (regn->LayerId != 0xff)
                        {
                            continue;
                        }

                        // Skip if the neighbour is overlapping root region
                        if (Contains(root->Layers, root->Nlayers, nei))
                        {
                            continue;
                        }

                        // Skip if the height range would become too large
                        var ymin = math.min((int)root->Ymin, regn->Ymin);
                        var ymax = math.max((int)root->Ymax, regn->Ymax);
                        if (ymax - ymin >= 255)
                        {
                            continue;
                        }

                        if (nStack < maxStack)
                        {
                            // Deepen
                            stack[nStack++] = nei;

                            // Mark layer id
                            regn->LayerId = layerId;

                            // Merge current layers to root
                            for (var k = 0; k < regn->Nlayers; ++k)
                            {
                                if (!AddUnique(root->Layers, &root->Nlayers, RCMaxLayers, regn->Layers[k]))
                                {
                                    return false;
                                }
                            }

                            root->Ymin = (ushort)math.min((int)root->Ymin, regn->Ymin);
                            root->Ymax = (ushort)math.max((int)root->Ymax, regn->Ymax);
                        }
                    }
                }

                layerId++;
            }

            // Merge non-overlapping regions that are close in height
            var mergeHeight = (ushort)(walkableHeight * 4);

            for (var i = 0; i < nregs; ++i)
            {
                var ri = &regs[i];
                if (ri->Base == 0)
                {
                    continue;
                }

                var newId = ri->LayerId;

                while (true)
                {
                    byte oldId = 0xff;

                    for (var j = 0; j < nregs; ++j)
                    {
                        if (i == j)
                        {
                            continue;
                        }

                        var rj = &regs[j];
                        if (rj->Base == 0)
                        {
                            continue;
                        }

                        // Skip if the regions are not close to each other
                        if (!OverlapRange(ri->Ymin, ri->Ymax + mergeHeight, rj->Ymin, rj->Ymax + mergeHeight))
                        {
                            continue;
                        }

                        // Skip if the height range would become too large
                        var ymin = math.min((int)ri->Ymin, rj->Ymin);
                        var ymax = math.max((int)ri->Ymax, rj->Ymax);
                        if (ymax - ymin >= 255)
                        {
                            continue;
                        }

                        // Make sure that there is no overlap when merging 'ri' and 'rj'
                        var overlap = false;

                        // Iterate over all regions which have the same layerId as 'rj'
                        for (var k = 0; k < nregs; ++k)
                        {
                            if (regs[k].LayerId != rj->LayerId)
                            {
                                continue;
                            }

                            // Check if region 'k' is overlapping region 'ri'
                            // Index to 'regs' is the same as region id
                            if (Contains(ri->Layers, ri->Nlayers, (byte)k))
                            {
                                overlap = true;
                                break;
                            }
                        }

                        // Cannot merge if regions overlap
                        if (overlap)
                        {
                            continue;
                        }

                        // Can merge i and j
                        oldId = rj->LayerId;
                        break;
                    }

                    // Could not find anything to merge with, stop
                    if (oldId == 0xff)
                    {
                        break;
                    }

                    // Merge
                    for (var j = 0; j < nregs; ++j)
                    {
                        var rj = &regs[j];
                        if (rj->LayerId == oldId)
                        {
                            rj->Base = 0;

                            // Remap layerIds
                            rj->LayerId = newId;

                            // Add overlaid layers from 'rj' to 'ri'
                            for (var k = 0; k < rj->Nlayers; ++k)
                            {
                                if (!AddUnique(ri->Layers, &ri->Nlayers, RCMaxLayers, rj->Layers[k]))
                                {
                                    return false;
                                }
                            }

                            // Update height bounds
                            ri->Ymin = (ushort)math.min((int)ri->Ymin, rj->Ymin);
                            ri->Ymax = (ushort)math.max((int)ri->Ymax, rj->Ymax);
                        }
                    }
                }
            }

            // Compact layerIds
            var remap = stackalloc byte[256];
            UnsafeUtility.MemClear(remap, 256);

            // Find number of unique layers
            layerId = 0;
            for (var i = 0; i < nregs; ++i)
            {
                remap[regs[i].LayerId] = 1;
            }

            for (var i = 0; i < 256; ++i)
            {
                if (remap[i] != 0)
                {
                    remap[i] = layerId++;
                }
                else
                {
                    remap[i] = 0xff;
                }
            }

            // Remap ids
            for (var i = 0; i < nregs; ++i)
            {
                regs[i].LayerId = remap[regs[i].LayerId];
            }

            // No layers, return empty
            if (layerId == 0)
            {
                return true;
            }

            // Create layers
            var lw = w - (borderSize * 2);
            var lh = h - (borderSize * 2);

            // Build contracted bbox for layers
            var bmin = compactHeightfield->BMin;
            var bmax = compactHeightfield->BMax;
            bmin.x += borderSize * compactHeightfield->CellSize;
            bmin.z += borderSize * compactHeightfield->CellSize;
            bmax.x -= borderSize * compactHeightfield->CellSize;
            bmax.z -= borderSize * compactHeightfield->CellSize;

            layerSet->NLayers = layerId;

            layerSet->Layers = (RcHeightfieldLayer*)AllocatorManager.Allocate(layerSet->Allocator, sizeof(RcHeightfieldLayer) * layerSet->NLayers,
                UnsafeUtility.AlignOf<RcHeightfieldLayer>());

            UnsafeUtility.MemClear(layerSet->Layers, sizeof(RcHeightfieldLayer) * layerSet->NLayers);

            // Store layers
            for (var i = 0; i < layerSet->NLayers; ++i)
            {
                var curId = (byte)i;

                var layer = &layerSet->Layers[i];
                *layer = new RcHeightfieldLayer(layerSet->Allocator);

                var gridSize = sizeof(byte) * lw * lh;

                layer->Heights = (byte*)AllocatorManager.Allocate(layerSet->Allocator, gridSize, UnsafeUtility.AlignOf<byte>());
                UnsafeUtility.MemSet(layer->Heights, 0xff, gridSize);

                layer->Areas = (byte*)AllocatorManager.Allocate(layerSet->Allocator, gridSize, UnsafeUtility.AlignOf<byte>());
                UnsafeUtility.MemClear(layer->Areas, gridSize);

                layer->Cons = (byte*)AllocatorManager.Allocate(layerSet->Allocator, gridSize, UnsafeUtility.AlignOf<byte>());
                UnsafeUtility.MemClear(layer->Cons, gridSize);

                // Find layer height bounds
                int hmin = 0, hmax = 0;
                for (var j = 0; j < nregs; ++j)
                {
                    if (regs[j].Base != 0 && regs[j].LayerId == curId)
                    {
                        hmin = regs[j].Ymin;
                        hmax = regs[j].Ymax;
                    }
                }

                layer->Width = lw;
                layer->Height = lh;
                layer->CellSize = compactHeightfield->CellSize;
                layer->CellHeight = compactHeightfield->CellHeight;

                // Adjust the bbox to fit the heightfield
                layer->BoundMin = bmin;
                layer->BoundMax = bmax;
                layer->BoundMin.y = bmin.y + (hmin * compactHeightfield->CellHeight);
                layer->BoundMax.y = bmin.y + (hmax * compactHeightfield->CellHeight);
                layer->HeightMin = hmin;
                layer->HeightMax = hmax;

                // Update usable data region
                layer->MinX = layer->Width;
                layer->MaxX = 0;
                layer->MinY = layer->Height;
                layer->MaxY = 0;

                // Copy height and area from compact heightfield
                for (var y = 0; y < lh; ++y)
                {
                    for (var x = 0; x < lw; ++x)
                    {
                        var cx = borderSize + x;
                        var cy = borderSize + y;
                        var c = compactHeightfield->Cells[cx + (cy * w)];
                        for (int j = (int)c.Index, nj = (int)(c.Index + c.Count); j < nj; ++j)
                        {
                            var s = compactHeightfield->Spans[j];

                            // Skip unassigned regions
                            if (srcReg[j] == 0xff)
                            {
                                continue;
                            }

                            // Skip if does not belong to current layer
                            var lid = regs[srcReg[j]].LayerId;
                            if (lid != curId)
                            {
                                continue;
                            }

                            // Update data bounds
                            layer->MinX = math.min(layer->MinX, x);
                            layer->MaxX = math.max(layer->MaxX, x);
                            layer->MinY = math.min(layer->MinY, y);
                            layer->MaxY = math.max(layer->MaxY, y);

                            // Store height and area type
                            var idx = x + (y * lw);
                            layer->Heights[idx] = (byte)(s.Y - hmin);
                            layer->Areas[idx] = compactHeightfield->Areas[j];

                            // Check connection
                            byte portal = 0;
                            byte con = 0;
                            for (var dir = 0; dir < 4; ++dir)
                            {
                                if (GetCon(s, dir) != RCNotConnected)
                                {
                                    var ax = cx + GetDirOffsetX(dir);
                                    var ay = cy + GetDirOffsetY(dir);
                                    var ai = (int)compactHeightfield->Cells[ax + (ay * w)].Index + GetCon(s, dir);
                                    var alid = srcReg[ai] != 0xff ? regs[srcReg[ai]].LayerId : (byte)0xff;

                                    // Portal mask
                                    if (compactHeightfield->Areas[ai] != RCNullArea && lid != alid)
                                    {
                                        portal |= (byte)(1 << dir);

                                        // Update height so that it matches on both sides of the portal
                                        var aSpan = compactHeightfield->Spans[ai];
                                        if (aSpan.Y > hmin)
                                        {
                                            layer->Heights[idx] = (byte)math.max((int)layer->Heights[idx], (byte)(aSpan.Y - hmin));
                                        }
                                    }

                                    // Valid connection mask
                                    if (compactHeightfield->Areas[ai] != RCNullArea && lid == alid)
                                    {
                                        var nx = ax - borderSize;
                                        var ny = ay - borderSize;
                                        if (nx >= 0 && ny >= 0 && nx < lw && ny < lh)
                                        {
                                            con |= (byte)(1 << dir);
                                        }
                                    }
                                }
                            }

                            layer->Cons[idx] = (byte)((portal << 4) | con);
                        }
                    }
                }

                if (layer->MinX > layer->MaxX)
                {
                    layer->MinX = layer->MaxX = 0;
                }

                if (layer->MinY > layer->MaxY)
                {
                    layer->MinY = layer->MaxY = 0;
                }
            }

            return true;
        }

        private static bool Contains(byte* a, byte an, byte v)
        {
            int n = an;
            for (var i = 0; i < n; ++i)
            {
                if (a[i] == v)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AddUnique(byte* a, byte* an, int anMax, byte v)
        {
            if (Contains(a, *an, v))
            {
                return true;
            }

            if (*an >= anMax)
            {
                return false;
            }

            a[*an] = v;
            (*an)++;
            return true;
        }

        private static bool OverlapRange(int amin, int amax, int bmin, int bmax)
        {
            return !(amin > bmax || amax < bmin);
        }

        private struct LayerSweepSpan
        {
            public ushort Ns;   // number samples
            public byte ID;     // region id
            public byte Nei;    // neighbour id
        }

        private struct LayerRegion
        {
            public fixed byte Layers[RCMaxLayers];
            public fixed byte Neis[RCMaxNeis];
            public ushort Ymin;
            public ushort Ymax;
            public byte LayerId;        // Layer ID
            public byte Nlayers;        // Layer count
            public byte Nneis;          // Neighbour count
            public byte Base;          // Flag indicating if the region is the base of merged regions
        }
    }
}
