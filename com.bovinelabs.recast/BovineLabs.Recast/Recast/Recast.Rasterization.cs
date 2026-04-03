// <copyright file="Recast.Rasterization.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Recast triangle rasterization functions for converting triangle geometry into voxel heightfields.
    /// These functions handle the core voxelization process that converts 3D triangle meshes into
    /// the 2.5D heightfield representation used by Recast.
    /// </summary>
    public static unsafe partial class Recast
    {
        private const int AxisX = 0;
        private const int AxisZ = 2;

        /// <summary>
        /// Rasterizes an indexed triangle mesh into the specified heightfield.
        /// Spans will only be added for triangles that overlap the heightfield grid.
        /// </summary>
        /// <param name="verts"> The vertices [(x, y, z) * numVerts]. </param>
        /// <param name="tris"> The triangle indices [(vertA, vertB, vertC) * numTris]. </param>
        /// <param name="triAreaIDs"> The area id's of the triangles [Limit: &lt;= RC_WALKABLE_AREA] [Size: numTris]. </param>
        /// <param name="numTris"> The number of triangles. </param>
        /// <param name="heightfield"> An initialized heightfield. </param>
        /// <param name="flagMergeThreshold"> The distance where the walkable flag is favored over the non-walkable flag [Limit: >= 0] [Units: vx]. </param>
        public static void RasterizeTriangles(float3* verts, int3* tris, byte* triAreaIDs, int numTris, RcHeightfield* heightfield, int flagMergeThreshold = 1)
        {
            var inverseCellSize = math.rcp(heightfield->Cs);
            var inverseCellHeight = math.rcp(heightfield->Ch);

            for (var triIndex = 0; triIndex < numTris; triIndex++)
            {
                var v0 = verts[tris[triIndex].x];
                var v1 = verts[tris[triIndex].y];
                var v2 = verts[tris[triIndex].z];

                RasterizeTriangle(v0, v1, v2, triAreaIDs[triIndex], heightfield, inverseCellSize, inverseCellHeight, flagMergeThreshold);
            }
        }

        /// <summary>
        /// Rasterizes an indexed triangle mesh into the specified heightfield using unsigned short indices.
        /// Spans will only be added for triangles that overlap the heightfield grid.
        /// </summary>
        /// <param name="verts"> The vertices [(x, y, z) * numVerts]. </param>
        /// <param name="tris"> The triangle indices [(vertA, vertB, vertC) * numTris] using ushort indices. </param>
        /// <param name="triAreaIDs"> The area id's of the triangles [Limit: &lt;= RC_WALKABLE_AREA] [Size: numTris]. </param>
        /// <param name="numTris"> The number of triangles. </param>
        /// <param name="heightfield"> An initialized heightfield. </param>
        /// <param name="flagMergeThreshold"> The distance where the walkable flag is favored over the non-walkable flag [Limit: >= 0] [Units: vx]. </param>
        public static void RasterizeTriangles(
            float3* verts, ushort3* tris, byte* triAreaIDs, int numTris, RcHeightfield* heightfield, int flagMergeThreshold = 1)
        {
            var inverseCellSize = 1.0f / heightfield->Cs;
            var inverseCellHeight = 1.0f / heightfield->Ch;

            for (var triIndex = 0; triIndex < numTris; triIndex++)
            {
                var v0 = verts[tris[triIndex].x];
                var v1 = verts[tris[triIndex].y];
                var v2 = verts[tris[triIndex].z];

                RasterizeTriangle(v0, v1, v2, triAreaIDs[triIndex], heightfield, inverseCellSize, inverseCellHeight, flagMergeThreshold);
            }
        }

        /// <summary>
        /// Rasterizes a triangle list into the specified heightfield.
        /// Expects each triangle to be specified as three sequential vertices.
        /// Spans will only be added for triangles that overlap the heightfield grid.
        /// </summary>
        /// <param name="verts"> The triangle vertices [(ax, ay, az, bx, by, bz, cx, cy, cz) * numTris]. </param>
        /// <param name="triAreaIDs"> The area id's of the triangles [Limit: &lt;= RC_WALKABLE_AREA] [Size: numTris]. </param>
        /// <param name="numTris"> The number of triangles. </param>
        /// <param name="heightfield"> An initialized heightfield. </param>
        /// <param name="flagMergeThreshold"> The distance where the walkable flag is favored over the non-walkable flag [Limit: >= 0] [Units: vx]. </param>
        public static void RasterizeTriangles(float3* verts, byte* triAreaIDs, int numTris, RcHeightfield* heightfield, int flagMergeThreshold = 1)
        {
            var inverseCellSize = 1.0f / heightfield->Cs;
            var inverseCellHeight = 1.0f / heightfield->Ch;

            for (var triIndex = 0; triIndex < numTris; triIndex++)
            {
                var v0 = verts[(triIndex * 3) + 0];
                var v1 = verts[(triIndex * 3) + 1];
                var v2 = verts[(triIndex * 3) + 2];

                RasterizeTriangle(v0, v1, v2, triAreaIDs[triIndex], heightfield, inverseCellSize, inverseCellHeight, flagMergeThreshold);
            }
        }

        /// <summary>
        /// Rasterizes a single triangle into the specified heightfield.
        /// No spans will be added if the triangle does not overlap the heightfield grid.
        /// </summary>
        /// <param name="v0"> Triangle vertex 0. </param>
        /// <param name="v1"> Triangle vertex 1. </param>
        /// <param name="v2"> Triangle vertex 2. </param>
        /// <param name="areaID"> The area id of the triangle [Limit: &lt;= RC_WALKABLE_AREA]. </param>
        /// <param name="heightfield"> An initialized heightfield. </param>
        /// <param name="flagMergeThreshold"> The distance where the walkable flag is favored over the non-walkable flag [Limit: >= 0] [Units: vx]. </param>
        public static void RasterizeTriangle(float3 v0, float3 v1, float3 v2, uint areaID, RcHeightfield* heightfield, int flagMergeThreshold = 1)
        {
            var inverseCellSize = 1.0f / heightfield->Cs;
            var inverseCellHeight = 1.0f / heightfield->Ch;

            RasterizeTriangle(v0, v1, v2, areaID, heightfield, inverseCellSize, inverseCellHeight, flagMergeThreshold);
        }

        /// <summary>
        /// Internal triangle rasterization implementation.
        /// Clips the triangle against grid cells and adds spans to the heightfield.
        /// </summary>
        /// <param name="v0"> Triangle vertex 0. </param>
        /// <param name="v1"> Triangle vertex 1. </param>
        /// <param name="v2"> Triangle vertex 2. </param>
        /// <param name="areaID"> The area id of the triangle [Limit: &lt;= RC_WALKABLE_AREA]. </param>
        /// <param name="heightfield"> An initialized heightfield. </param>
        /// <param name="inverseCellSize"> The reciprocal of the cell size. </param>
        /// <param name="inverseCellHeight"> The reciprocal of the cell height. </param>
        /// <param name="flagMergeThreshold"> The distance where the walkable flag is favored over the non-walkable flag [Limit: >= 0] [Units: vx]. </param>
        public static void RasterizeTriangle(
            float3 v0, float3 v1, float3 v2, uint areaID, RcHeightfield* heightfield, float inverseCellSize, float inverseCellHeight, int flagMergeThreshold)
        {
            // Calculate triangle bounding box
            var triBBMin = math.min(math.min(v0, v1), v2);
            var triBBMax = math.max(math.max(v0, v1), v2);

            // Check if triangle overlaps heightfield
            var heightfieldMin = heightfield->Bmin;
            var heightfieldMax = heightfield->Bmax;
            var overlapMask = (triBBMin <= heightfieldMax) & (triBBMax >= heightfieldMin);
            if (!math.all(overlapMask))
            {
                return;
            }

            var w = heightfield->Width;
            var h = heightfield->Height;
            var heightfieldRange = heightfieldMax.y - heightfieldMin.y;

            // Calculate Z footprint
            var zRange = new float2((triBBMin.z - heightfieldMin.z) * inverseCellSize, (triBBMax.z - heightfieldMin.z) * inverseCellSize);

            var z0 = (int)zRange.x;
            var z1 = (int)zRange.y;

            // Use -1 rather than 0 to cut the polygon properly at the start of the tile
            z0 = math.min(math.max(z0, -1), h - 1);
            z1 = math.min(math.max(z1, 0), h - 1);

            // Clip the triangle into all grid cells it touches
            var bufferSize = 7 * 4; // 7 verts * 4 arrays
            var buf = stackalloc float3[bufferSize];

            var inVerts = buf;
            var inRow = buf + 7;
            var p1 = buf + 14;
            var p2 = buf + 21;

            inVerts[0] = v0;
            inVerts[1] = v1;
            inVerts[2] = v2;
            var nvIn = 3;

            for (var z = z0; z <= z1; z++)
            {
                // Clip polygon to row. Store the remaining polygon as well
                var cellZ = heightfieldMin.z + (z * heightfield->Cs);
                DividePoly(inVerts, nvIn, inRow, out var nvRow, p1, out nvIn, cellZ + heightfield->Cs, AxisZ);

                // Swap buffers
                var tmp1 = inVerts;
                inVerts = p1;
                p1 = tmp1;

                if (nvRow < 3)
                {
                    continue;
                }

                if (z < 0)
                {
                    continue;
                }

                // Find X-axis bounds of the row
                var minX = inRow[0].x;
                var maxX = inRow[0].x;
                for (var vert = 1; vert < nvRow; vert++)
                {
                    minX = math.min(minX, inRow[vert].x);
                    maxX = math.max(maxX, inRow[vert].x);
                }

                var x0 = (int)((minX - heightfield->Bmin.x) * inverseCellSize);
                var x1 = (int)((maxX - heightfield->Bmin.x) * inverseCellSize);

                if (x1 < 0 || x0 >= w)
                {
                    continue;
                }

                x0 = math.min(math.max(x0, -1), w - 1);
                x1 = math.min(math.max(x1, 0), w - 1);

                var nv2 = nvRow;

                for (var x = x0; x <= x1; x++)
                {
                    // Clip polygon to column
                    var cellX = heightfieldMin.x + (x * heightfield->Cs);
                    DividePoly(inRow, nv2, p1, out var nv, p2, out nv2, cellX + heightfield->Cs, AxisX);

                    // Swap buffers
                    var tmp2 = inRow;
                    inRow = p2;
                    p2 = tmp2;

                    if (nv < 3)
                    {
                        continue;
                    }

                    if (x < 0)
                    {
                        continue;
                    }

                    // Calculate min and max of the span
                    var spanMin = p1[0].y;
                    var spanMax = p1[0].y;
                    for (var vert = 1; vert < nv; vert++)
                    {
                        spanMin = math.min(spanMin, p1[vert].y);
                        spanMax = math.max(spanMax, p1[vert].y);
                    }

                    spanMin -= heightfield->Bmin.y;
                    spanMax -= heightfield->Bmin.y;

                    // Skip the span if it's completely outside the heightfield bounding box
                    if (spanMax < 0.0f)
                    {
                        continue;
                    }

                    if (spanMin > heightfieldRange)
                    {
                        continue;
                    }

                    // Clamp the span to the heightfield bounding box
                    spanMin = math.max(spanMin, 0);
                    spanMax = math.min(spanMax, heightfieldRange);

                    // Snap the span to the heightfield height grid
                    var spanMinCellIndex = (uint)math.clamp((int)math.floor(spanMin * inverseCellHeight), 0, RCSpanMaxHeight);
                    var spanMaxCellIndex = (uint)math.clamp((int)math.ceil(spanMax * inverseCellHeight), (int)spanMinCellIndex + 1, RCSpanMaxHeight);

                    AddSpan(heightfield, x, z, spanMinCellIndex, spanMaxCellIndex, areaID, flagMergeThreshold);
                }
            }
        }

        /// <summary>
        /// Adds a span to the specified heightfield.
        /// If the span merges with an existing span and the new spanMax is within flagMergeThreshold
        /// units from the existing span, the span flags are merged.
        /// </summary>
        /// <param name="heightfield"> An initialized heightfield. </param>
        /// <param name="x"> The column x index where the span is to be added [Limits: 0 &lt;= value &lt; rcHeightfield::width]. </param>
        /// <param name="z"> The column z index where the span is to be added [Limits: 0 &lt;= value &lt; rcHeightfield::height]. </param>
        /// <param name="min"> The minimum height of the span [Limit: &lt; spanMax] [Units: vx]. </param>
        /// <param name="max"> The maximum height of the span [Limit: &lt;= RC_SPAN_MAX_HEIGHT] [Units: vx]. </param>
        /// <param name="areaID"> The area id of the span [Limit: &lt;= RC_WALKABLE_AREA]. </param>
        /// <param name="flagMergeThreshold"> The merge threshold [Limit: >= 0] [Units: vx]. </param>
        public static void AddSpan(RcHeightfield* heightfield, int x, int z, uint min, uint max, uint areaID, int flagMergeThreshold)
        {
            if (min >= max)
            {
                return;
            }

            // Create the new span
            var newSpan = AllocSpan(heightfield);

            newSpan->SMin = min;
            newSpan->SMax = max;
            newSpan->Area = areaID;
            newSpan->Next = null;

            var columnIndex = x + (z * heightfield->Width);
            RcSpan* previousSpan = null;
            var currentSpan = heightfield->Spans[columnIndex];

            // Insert the new span, possibly merging it with existing spans
            while (currentSpan != null)
            {
                if (currentSpan->SMin > newSpan->SMax)
                {
                    // Current span is completely after the new span, break
                    break;
                }

                if (currentSpan->SMax < newSpan->SMin)
                {
                    // Current span is completely before the new span. Keep going
                    previousSpan = currentSpan;
                    currentSpan = currentSpan->Next;
                }
                else
                {
                    // The new span overlaps with an existing span. Merge them
                    if (currentSpan->SMin < newSpan->SMin)
                    {
                        newSpan->SMin = currentSpan->SMin;
                    }

                    if (currentSpan->SMax > newSpan->SMax)
                    {
                        newSpan->SMax = currentSpan->SMax;
                    }

                    // Merge flags
                    if (math.abs((int)newSpan->SMax - (int)currentSpan->SMax) <= flagMergeThreshold)
                    {
                        // Higher area ID numbers indicate higher resolution priority
                        newSpan->Area = math.max(newSpan->Area, currentSpan->Area);
                    }

                    // Remove the current span since it's now merged with newSpan
                    // Keep going because there might be other overlapping spans that also need to be merged
                    var next = currentSpan->Next;
                    FreeSpan(heightfield, currentSpan);
                    if (previousSpan != null)
                    {
                        previousSpan->Next = next;
                    }
                    else
                    {
                        heightfield->Spans[columnIndex] = next;
                    }

                    currentSpan = next;
                }
            }

            // Insert new span after prev
            if (previousSpan != null)
            {
                newSpan->Next = previousSpan->Next;
                previousSpan->Next = newSpan;
            }
            else
            {
                // This span should go before the others in the list
                newSpan->Next = heightfield->Spans[columnIndex];
                heightfield->Spans[columnIndex] = newSpan;
            }
        }

        /// <summary> Divides a convex polygon of max 12 vertices into two convex polygons across a separating axis. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DividePoly(
            float3* inVerts, int inVertsCount, float3* outVerts1, out int outVerts1Count, float3* outVerts2, out int outVerts2Count, float axisOffset, int axis)
        {
            var inVertAxisDelta = stackalloc float[12];

            // Calculate axis deltas for all vertices
            for (var i = 0; i < inVertsCount; i++)
            {
                inVertAxisDelta[i] = axisOffset - inVerts[i][axis];
            }

            var poly1Vert = 0;
            var poly2Vert = 0;

            // Process each edge of the polygon
            for (int inVertA = 0, inVertB = inVertsCount - 1; inVertA < inVertsCount; inVertB = inVertA, inVertA++)
            {
                var deltaA = inVertAxisDelta[inVertA];
                var deltaB = inVertAxisDelta[inVertB];

                // Check if vertices are on the same side of the separating axis
                var sideA = deltaA >= 0;
                var sideB = deltaB >= 0;
                var sameSide = sideA == sideB;

                if (!sameSide)
                {
                    // Edge crosses the separating axis - add intersection point to both polygons
                    var t = deltaB / (deltaB - deltaA);
                    var intersection = math.lerp(inVerts[inVertB], inVerts[inVertA], t);

                    outVerts1[poly1Vert] = intersection;
                    outVerts2[poly2Vert] = intersection;
                    poly1Vert++;
                    poly2Vert++;

                    // Add current vertex to appropriate polygon(s)
                    if (deltaA > 0)
                    {
                        outVerts1[poly1Vert++] = inVerts[inVertA];
                    }
                    else if (deltaA < 0)
                    {
                        outVerts2[poly2Vert++] = inVerts[inVertA];
                    }
                }
                else
                {
                    if (sideA)
                    {
                        outVerts1[poly1Vert++] = inVerts[inVertA];
                        if (inVertAxisDelta[inVertA] != 0)
                        {
                            continue;
                        }
                    }

                    outVerts2[poly2Vert++] = inVerts[inVertA];
                }
            }

            outVerts1Count = poly1Vert;
            outVerts2Count = poly2Vert;
        }

        /// <summary> Allocates a new span in the heightfield. Uses a memory pool and free list to minimize allocations. </summary>
        private static RcSpan* AllocSpan(RcHeightfield* heightfield)
        {
            // If necessary, allocate new page and update the freelist
            if (heightfield->Freelist == null || heightfield->Freelist->Next == null)
            {
                // Create new page
                var spanPool = (RcSpanPool*)AllocatorManager.Allocate(heightfield->Allocator, sizeof(RcSpanPool), UnsafeUtility.AlignOf<RcSpanPool>());

                // Add the pool into the list of pools
                spanPool->Next = heightfield->Pools;
                heightfield->Pools = spanPool;

                // Add new spans to the free list
                var freeList = heightfield->Freelist;
                for (var i = RCSpansPerPool - 1; i >= 0; i--)
                {
                    var span = spanPool->GetSpan(i);
                    span->Next = freeList;
                    freeList = span;
                }

                heightfield->Freelist = freeList;
            }

            // Pop item from the front of the free list
            var newSpan = heightfield->Freelist;
            heightfield->Freelist = heightfield->Freelist->Next;
            return newSpan;
        }

        /// <summary> Releases the memory used by the span back to the heightfield, so it can be re-used for new spans. </summary>
        private static void FreeSpan(RcHeightfield* heightfield, RcSpan* span)
        {
            if (span == null)
            {
                return;
            }

            // Add the span to the front of the free list
            span->Next = heightfield->Freelist;
            heightfield->Freelist = span;
        }
    }
}
