// <copyright file="Recast.Area.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Recast area marking and processing functions.
    /// These functions allow marking specific areas of the compact heightfield
    /// with different area types and applying filtering operations.
    /// </summary>
    public static unsafe partial class Recast
    {
        /// <summary>
        /// Erodes the walkable area within the heightfield by the specified radius.
        /// </summary>
        /// <remarks>
        /// Any spans that are closer to an obstruction or boundary than <paramref name="erosionRadius"/> are
        /// marked as unwalkable. This mirrors the behaviour of <c>rcErodeWalkableArea</c> in the native Recast
        /// library and should usually be invoked immediately after building the compact heightfield.
        /// </remarks>
        /// <param name="erosionRadius">The erosion radius to apply [Limits: 0 &lt; value &lt; 255] [Units: vx].</param>
        /// <param name="compactHeightfield">The populated compact heightfield whose span areas are updated.</param>
        public static void ErodeWalkableArea(int erosionRadius, RcCompactHeightfield* compactHeightfield)
        {
            var xSize = compactHeightfield->Width;
            var zSize = compactHeightfield->Height;
            var zStride = xSize; // For readability

            var distanceToBoundary = (byte*)AllocatorManager.Allocate(Allocator.Temp, sizeof(byte) * compactHeightfield->SpanCount,
                UnsafeUtility.AlignOf<byte>());

            UnsafeUtility.MemSet(distanceToBoundary, 0xff, compactHeightfield->SpanCount);

            // Mark boundary cells
            for (var z = 0; z < zSize; z++)
            {
                for (var x = 0; x < xSize; x++)
                {
                    var cell = compactHeightfield->Cells[x + (z * zStride)];
                    for (int spanIndex = (int)cell.Index, maxSpanIndex = (int)(cell.Index + cell.Count); spanIndex < maxSpanIndex; spanIndex++)
                    {
                        if (compactHeightfield->Areas[spanIndex] == RCNullArea)
                        {
                            distanceToBoundary[spanIndex] = 0;
                            continue;
                        }

                        var span = compactHeightfield->Spans[spanIndex];

                        var neighborCount = 0;
                        for (var direction = 0; direction < 4; direction++)
                        {
                            var neighborConnection = GetCon(span, direction);
                            if (neighborConnection == RCNotConnected)
                            {
                                break;
                            }

                            var neighborX = x + GetDirOffsetX(direction);
                            var neighborZ = z + GetDirOffsetY(direction);
                            var neighborSpanIndex = (int)compactHeightfield->Cells[neighborX + (neighborZ * zStride)].Index + neighborConnection;

                            if (compactHeightfield->Areas[neighborSpanIndex] == RCNullArea)
                            {
                                break;
                            }

                            neighborCount++;
                        }

                        if (neighborCount != 4)
                        {
                            distanceToBoundary[spanIndex] = 0;
                        }
                    }
                }
            }

            byte newDistance;

            // Pass 1
            for (var z = 0; z < zSize; z++)
            {
                for (var x = 0; x < xSize; x++)
                {
                    var cell = compactHeightfield->Cells[x + (z * zStride)];
                    var maxSpanIndex = (int)(cell.Index + cell.Count);
                    for (var spanIndex = (int)cell.Index; spanIndex < maxSpanIndex; spanIndex++)
                    {
                        var span = compactHeightfield->Spans[spanIndex];

                        if (GetCon(span, 0) != RCNotConnected)
                        {
                            var aX = x + GetDirOffsetX(0);
                            var aY = z + GetDirOffsetY(0);
                            var aIndex = (int)compactHeightfield->Cells[aX + (aY * xSize)].Index + GetCon(span, 0);
                            var aSpan = compactHeightfield->Spans[aIndex];
                            newDistance = (byte)math.min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            if (GetCon(aSpan, 3) != RCNotConnected)
                            {
                                var bX = aX + GetDirOffsetX(3);
                                var bY = aY + GetDirOffsetY(3);
                                var bIndex = (int)compactHeightfield->Cells[bX + (bY * xSize)].Index + GetCon(aSpan, 3);
                                newDistance = (byte)math.min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }

                        if (GetCon(span, 3) != RCNotConnected)
                        {
                            var aX = x + GetDirOffsetX(3);
                            var aY = z + GetDirOffsetY(3);
                            var aIndex = (int)compactHeightfield->Cells[aX + (aY * xSize)].Index + GetCon(span, 3);
                            var aSpan = compactHeightfield->Spans[aIndex];
                            newDistance = (byte)math.min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            if (GetCon(aSpan, 2) != RCNotConnected)
                            {
                                var bX = aX + GetDirOffsetX(2);
                                var bY = aY + GetDirOffsetY(2);
                                var bIndex = (int)compactHeightfield->Cells[bX + (bY * xSize)].Index + GetCon(aSpan, 2);
                                newDistance = (byte)math.min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }

            // Pass 2
            for (var z = zSize - 1; z >= 0; z--)
            {
                for (var x = xSize - 1; x >= 0; x--)
                {
                    var cell = compactHeightfield->Cells[x + (z * zStride)];
                    var maxSpanIndex = (int)(cell.Index + cell.Count);
                    for (var spanIndex = (int)cell.Index; spanIndex < maxSpanIndex; spanIndex++)
                    {
                        var span = compactHeightfield->Spans[spanIndex];

                        if (GetCon(span, 2) != RCNotConnected)
                        {
                            var aX = x + GetDirOffsetX(2);
                            var aY = z + GetDirOffsetY(2);
                            var aIndex = (int)compactHeightfield->Cells[aX + (aY * xSize)].Index + GetCon(span, 2);
                            var aSpan = compactHeightfield->Spans[aIndex];
                            newDistance = (byte)math.min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            if (GetCon(aSpan, 1) != RCNotConnected)
                            {
                                var bX = aX + GetDirOffsetX(1);
                                var bY = aY + GetDirOffsetY(1);
                                var bIndex = (int)compactHeightfield->Cells[bX + (bY * xSize)].Index + GetCon(aSpan, 1);
                                newDistance = (byte)math.min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }

                        if (GetCon(span, 1) != RCNotConnected)
                        {
                            var aX = x + GetDirOffsetX(1);
                            var aY = z + GetDirOffsetY(1);
                            var aIndex = (int)compactHeightfield->Cells[aX + (aY * xSize)].Index + GetCon(span, 1);
                            var aSpan = compactHeightfield->Spans[aIndex];
                            newDistance = (byte)math.min(distanceToBoundary[aIndex] + 2, 255);
                            if (newDistance < distanceToBoundary[spanIndex])
                            {
                                distanceToBoundary[spanIndex] = newDistance;
                            }

                            if (GetCon(aSpan, 0) != RCNotConnected)
                            {
                                var bX = aX + GetDirOffsetX(0);
                                var bY = aY + GetDirOffsetY(0);
                                var bIndex = (int)compactHeightfield->Cells[bX + (bY * xSize)].Index + GetCon(aSpan, 0);
                                newDistance = (byte)math.min(distanceToBoundary[bIndex] + 3, 255);
                                if (newDistance < distanceToBoundary[spanIndex])
                                {
                                    distanceToBoundary[spanIndex] = newDistance;
                                }
                            }
                        }
                    }
                }
            }

            var minBoundaryDistance = (byte)(erosionRadius * 2);
            for (var spanIndex = 0; spanIndex < compactHeightfield->SpanCount; spanIndex++)
            {
                if (distanceToBoundary[spanIndex] < minBoundaryDistance)
                {
                    compactHeightfield->Areas[spanIndex] = RCNullArea;
                }
            }
        }

        /// <summary>
        /// Applies a median filter to walkable area types (based on area ids), removing noise.
        /// </summary>
        /// <remarks>
        /// This is typically called after assigning custom areas using helpers such as
        /// <see cref="MarkBoxArea"/>, <see cref="MarkConvexPolyArea"/>, or <see cref="MarkCylinderArea"/>.
        /// Equivalent to <c>rcMedianFilterWalkableArea</c> in the Recast C++ API.
        /// </remarks>
        /// <param name="compactHeightfield">The compact heightfield whose span areas will be filtered.</param>
        /// <returns><see langword="true"/> if the operation completes successfully; otherwise, <see langword="false"/>.</returns>
        public static bool MedianFilterWalkableArea(RcCompactHeightfield* compactHeightfield)
        {
            var xSize = compactHeightfield->Width;
            var zSize = compactHeightfield->Height;
            var zStride = xSize;

            var areas = (byte*)AllocatorManager.Allocate(Allocator.Temp, sizeof(byte) * compactHeightfield->SpanCount, UnsafeUtility.AlignOf<byte>());

            UnsafeUtility.MemSet(areas, 0xff, compactHeightfield->SpanCount);

            byte* neighborAreas = stackalloc byte[9];

            for (var z = 0; z < zSize; z++)
            {
                for (var x = 0; x < xSize; x++)
                {
                    var cell = compactHeightfield->Cells[x + (z * zStride)];
                    var maxSpanIndex = (int)(cell.Index + cell.Count);
                    for (var spanIndex = (int)cell.Index; spanIndex < maxSpanIndex; spanIndex++)
                    {
                        var span = compactHeightfield->Spans[spanIndex];
                        if (compactHeightfield->Areas[spanIndex] == RCNullArea)
                        {
                            areas[spanIndex] = compactHeightfield->Areas[spanIndex];
                            continue;
                        }

                        for (var neighborIndex = 0; neighborIndex < 9; neighborIndex++)
                        {
                            neighborAreas[neighborIndex] = compactHeightfield->Areas[spanIndex];
                        }

                        for (var dir = 0; dir < 4; dir++)
                        {
                            if (GetCon(span, dir) == RCNotConnected)
                            {
                                continue;
                            }

                            var aX = x + GetDirOffsetX(dir);
                            var aZ = z + GetDirOffsetY(dir);
                            var aIndex = (int)compactHeightfield->Cells[aX + (aZ * zStride)].Index + GetCon(span, dir);
                            if (compactHeightfield->Areas[aIndex] != RCNullArea)
                            {
                                neighborAreas[dir * 2] = compactHeightfield->Areas[aIndex];
                            }

                            var aSpan = compactHeightfield->Spans[aIndex];
                            var dir2 = (dir + 1) & 0x3;
                            var neighborConnection2 = GetCon(aSpan, dir2);
                            if (neighborConnection2 != RCNotConnected)
                            {
                                var bX = aX + GetDirOffsetX(dir2);
                                var bZ = aZ + GetDirOffsetY(dir2);
                                var bIndex = (int)compactHeightfield->Cells[bX + (bZ * zStride)].Index + neighborConnection2;
                                if (compactHeightfield->Areas[bIndex] != RCNullArea)
                                {
                                    neighborAreas[(dir * 2) + 1] = compactHeightfield->Areas[bIndex];
                                }
                            }
                        }

                        InsertionSort(neighborAreas, 9);
                        areas[spanIndex] = neighborAreas[4];
                    }
                }
            }

            UnsafeUtility.MemCpy(compactHeightfield->Areas, areas, sizeof(byte) * compactHeightfield->SpanCount);
            return true;
        }

        /// <summary>
        /// Applies an area id to all spans whose voxels fall within the specified axis-aligned bounding box.
        /// </summary>
        /// <remarks>Equivalent to <c>rcMarkBoxArea</c> in the native Recast implementation.</remarks>
        /// <param name="boxMinBounds">The minimum extents of the bounding box [(x, y, z)] [Units: wu].</param>
        /// <param name="boxMaxBounds">The maximum extents of the bounding box [(x, y, z)] [Units: wu].</param>
        /// <param name="areaId">The area id to apply [Limit: &lt;= <see cref="RCWalkableArea"/>].</param>
        /// <param name="compactHeightfield">The compact heightfield whose span areas are modified.</param>
        public static void MarkBoxArea(in float3 boxMinBounds, in float3 boxMaxBounds, byte areaId,
            RcCompactHeightfield* compactHeightfield)
        {
            var xSize = compactHeightfield->Width;
            var zSize = compactHeightfield->Height;
            var zStride = xSize; // For readability

            // Find the footprint of the box area in grid cell coordinates
            var minX = (int)((boxMinBounds.x - compactHeightfield->BMin.x) / compactHeightfield->CellSize);
            var minY = (int)((boxMinBounds.y - compactHeightfield->BMin.y) / compactHeightfield->CellHeight);
            var minZ = (int)((boxMinBounds.z - compactHeightfield->BMin.z) / compactHeightfield->CellSize);
            var maxX = (int)((boxMaxBounds.x - compactHeightfield->BMin.x) / compactHeightfield->CellSize);
            var maxY = (int)((boxMaxBounds.y - compactHeightfield->BMin.y) / compactHeightfield->CellHeight);
            var maxZ = (int)((boxMaxBounds.z - compactHeightfield->BMin.z) / compactHeightfield->CellSize);

            // Early-out if the box is outside the bounds of the grid
            if (maxX < 0)
            {
                return;
            }

            if (minX >= xSize)
            {
                return;
            }

            if (maxZ < 0)
            {
                return;
            }

            if (minZ >= zSize)
            {
                return;
            }

            // Clamp relevant bound coordinates to the grid
            if (minX < 0)
            {
                minX = 0;
            }

            if (maxX >= xSize)
            {
                maxX = xSize - 1;
            }

            if (minZ < 0)
            {
                minZ = 0;
            }

            if (maxZ >= zSize)
            {
                maxZ = zSize - 1;
            }

            // Mark relevant cells
            for (var z = minZ; z <= maxZ; z++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var cell = compactHeightfield->Cells[x + (z * zStride)];
                    var maxSpanIndex = (int)(cell.Index + cell.Count);
                    for (var spanIndex = (int)cell.Index; spanIndex < maxSpanIndex; spanIndex++)
                    {
                        var span = compactHeightfield->Spans[spanIndex];

                        // Skip if the span is outside the box extents
                        if (span.Y < minY || span.Y > maxY)
                        {
                            continue;
                        }

                        // Skip if the span has been removed
                        if (compactHeightfield->Areas[spanIndex] == RCNullArea)
                        {
                            continue;
                        }

                        // Mark the span
                        compactHeightfield->Areas[spanIndex] = areaId;
                    }
                }
            }
        }

        /// <summary>
        /// Applies the area id to all spans within the specified y-axis-aligned cylinder.
        /// </summary>
        /// <remarks>Equivalent to <c>rcMarkCylinderArea</c> in the C++ Recast library.</remarks>
        /// <param name="position">The center of the base of the cylinder [Form: (x, y, z)] [Units: wu].</param>
        /// <param name="radius">The cylinder radius [Units: wu] [Limit: &gt; 0].</param>
        /// <param name="height">The cylinder height [Units: wu] [Limit: &gt; 0].</param>
        /// <param name="areaId">The area id to apply [Limit: &lt;= <see cref="RCWalkableArea"/>].</param>
        /// <param name="compactHeightfield">The compact heightfield whose span areas are modified.</param>
        public static void MarkCylinderArea(in float3 position, float radius, float height, byte areaId,
            RcCompactHeightfield* compactHeightfield)
        {
            var xSize = compactHeightfield->Width;
            var zSize = compactHeightfield->Height;
            var zStride = xSize; // For readability

            // Compute the bounding box of the cylinder
            var cylinderBBMin = new float3(
                position.x - radius,
                position.y,
                position.z - radius);
            var cylinderBBMax = new float3(
                position.x + radius,
                position.y + height,
                position.z + radius);

            // Compute the grid footprint of the cylinder
            var minx = (int)((cylinderBBMin.x - compactHeightfield->BMin.x) / compactHeightfield->CellSize);
            var miny = (int)((cylinderBBMin.y - compactHeightfield->BMin.y) / compactHeightfield->CellHeight);
            var minz = (int)((cylinderBBMin.z - compactHeightfield->BMin.z) / compactHeightfield->CellSize);
            var maxx = (int)((cylinderBBMax.x - compactHeightfield->BMin.x) / compactHeightfield->CellSize);
            var maxy = (int)((cylinderBBMax.y - compactHeightfield->BMin.y) / compactHeightfield->CellHeight);
            var maxz = (int)((cylinderBBMax.z - compactHeightfield->BMin.z) / compactHeightfield->CellSize);

            // Early-out if the cylinder is completely outside the grid bounds
            if (maxx < 0)
            {
                return;
            }

            if (minx >= xSize)
            {
                return;
            }

            if (maxz < 0)
            {
                return;
            }

            if (minz >= zSize)
            {
                return;
            }

            // Clamp the cylinder bounds to the grid
            if (minx < 0)
            {
                minx = 0;
            }

            if (maxx >= xSize)
            {
                maxx = xSize - 1;
            }

            if (minz < 0)
            {
                minz = 0;
            }

            if (maxz >= zSize)
            {
                maxz = zSize - 1;
            }

            var radiusSq = radius * radius;

            for (var z = minz; z <= maxz; z++)
            {
                for (var x = minx; x <= maxx; x++)
                {
                    var cell = compactHeightfield->Cells[x + (z * zStride)];
                    var maxSpanIndex = (int)(cell.Index + cell.Count);

                    var cellX = compactHeightfield->BMin.x + ((x + 0.5f) * compactHeightfield->CellSize);
                    var cellZ = compactHeightfield->BMin.z + ((z + 0.5f) * compactHeightfield->CellSize);
                    var deltaX = cellX - position.x;
                    var deltaZ = cellZ - position.z;

                    // Skip this column if it's too far from the center point of the cylinder
                    if ((deltaX * deltaX) + (deltaZ * deltaZ) >= radiusSq)
                    {
                        continue;
                    }

                    // Mark all overlapping spans
                    for (var spanIndex = (int)cell.Index; spanIndex < maxSpanIndex; spanIndex++)
                    {
                        var span = compactHeightfield->Spans[spanIndex];

                        // Skip if span is removed
                        if (compactHeightfield->Areas[spanIndex] == RCNullArea)
                        {
                            continue;
                        }

                        // Mark if y extents overlap
                        if (span.Y >= miny && span.Y <= maxy)
                        {
                            compactHeightfield->Areas[spanIndex] = areaId;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies the area id to all spans within the specified convex polygon.
        /// </summary>
        /// <remarks>
        /// The polygon is projected onto the xz-plane, translated to <paramref name="minY"/>, and extruded to
        /// <paramref name="maxY"/> before spans are tested. Equivalent to <c>rcMarkConvexPolyArea</c> in C++.
        /// </remarks>
        /// <param name="verts">The polygon vertices [Form: (x, y, z) * <paramref name="numVerts"/>].</param>
        /// <param name="numVerts">The number of vertices in the polygon.</param>
        /// <param name="minY">The height of the base of the polygon [Units: wu].</param>
        /// <param name="maxY">The height of the top of the polygon [Units: wu].</param>
        /// <param name="areaId">The area id to apply [Limit: &lt;= <see cref="RCWalkableArea"/>].</param>
        /// <param name="compactHeightfield">The compact heightfield whose span areas are updated.</param>
        public static void MarkConvexPolyArea(float3* verts, int numVerts, float minY, float maxY, byte areaId,
            RcCompactHeightfield* compactHeightfield)
        {
            var xSize = compactHeightfield->Width;
            var zSize = compactHeightfield->Height;
            var zStride = xSize; // For readability

            // Compute the bounding box of the polygon
            var bmin = verts[0];
            var bmax = verts[0];
            for (var i = 1; i < numVerts; i++)
            {
                bmin = math.min(bmin, verts[i]);
                bmax = math.max(bmax, verts[i]);
            }

            bmin.y = minY;
            bmax.y = maxY;

            // Compute the grid footprint of the polygon
            var minx = (int)((bmin.x - compactHeightfield->BMin.x) / compactHeightfield->CellSize);
            var miny = (int)((bmin.y - compactHeightfield->BMin.y) / compactHeightfield->CellHeight);
            var minz = (int)((bmin.z - compactHeightfield->BMin.z) / compactHeightfield->CellSize);
            var maxx = (int)((bmax.x - compactHeightfield->BMin.x) / compactHeightfield->CellSize);
            var maxy = (int)((bmax.y - compactHeightfield->BMin.y) / compactHeightfield->CellHeight);
            var maxz = (int)((bmax.z - compactHeightfield->BMin.z) / compactHeightfield->CellSize);

            // Early-out if the polygon lies entirely outside the grid
            if (maxx < 0)
            {
                return;
            }

            if (minx >= xSize)
            {
                return;
            }

            if (maxz < 0)
            {
                return;
            }

            if (minz >= zSize)
            {
                return;
            }

            // Clamp the polygon footprint to the grid
            if (minx < 0)
            {
                minx = 0;
            }

            if (maxx >= xSize)
            {
                maxx = xSize - 1;
            }

            if (minz < 0)
            {
                minz = 0;
            }

            if (maxz >= zSize)
            {
                maxz = zSize - 1;
            }

            for (var z = minz; z <= maxz; z++)
            {
                for (var x = minx; x <= maxx; x++)
                {
                    var cell = compactHeightfield->Cells[x + (z * zStride)];
                    var maxSpanIndex = (int)(cell.Index + cell.Count);
                    for (var spanIndex = (int)cell.Index; spanIndex < maxSpanIndex; spanIndex++)
                    {
                        var span = compactHeightfield->Spans[spanIndex];

                        // Skip if span is removed
                        if (compactHeightfield->Areas[spanIndex] == RCNullArea)
                        {
                            continue;
                        }

                        // Skip if y extents don't overlap
                        if (span.Y < miny || span.Y > maxy)
                        {
                            continue;
                        }

                        var point = new float3(
                            compactHeightfield->BMin.x + ((x + 0.5f) * compactHeightfield->CellSize),
                            0,
                            compactHeightfield->BMin.z + ((z + 0.5f) * compactHeightfield->CellSize));

                        if (PointInPoly(numVerts, verts, point))
                        {
                            compactHeightfield->Areas[spanIndex] = areaId;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Expands a convex polygon along its vertex normals by the given offset amount, beveling sharp corners.
        /// </summary>
        /// <remarks>Helper function used by <see cref="MarkConvexPolyArea"/>. Mirrors <c>rcOffsetPoly</c> in Recast.</remarks>
        /// <param name="verts">The vertices of the polygon [Form: (x, y, z) * <paramref name="numVerts"/>].</param>
        /// <param name="numVerts">The number of vertices in the polygon.</param>
        /// <param name="offset">How much to offset the polygon [Units: wu].</param>
        /// <param name="outVerts">Receives the offset vertices (capacity must be &gt;= returned vertex count).</param>
        /// <param name="maxOutVerts">The maximum number of vertices that <paramref name="outVerts"/> can store.</param>
        /// <returns>The number of vertices in the offset polygon, or 0 if <paramref name="maxOutVerts"/> is insufficient.</returns>
        public static int OffsetPoly(float3* verts, int numVerts, float offset, float3* outVerts, int maxOutVerts)
        {
            const float miterLimit = 1.20f;
            var numOutVerts = 0;

            for (var vertIndex = 0; vertIndex < numVerts; vertIndex++)
            {
                var vertIndexA = (vertIndex + numVerts - 1) % numVerts;
                var vertIndexB = vertIndex;
                var vertIndexC = (vertIndex + 1) % numVerts;
                var vertA = verts[vertIndexA];
                var vertB = verts[vertIndexB];
                var vertC = verts[vertIndexC];

                var prevSegmentDir = vertB - vertA;
                prevSegmentDir.y = 0;
                prevSegmentDir = SafeNormalize(prevSegmentDir);

                var currSegmentDir = vertC - vertB;
                currSegmentDir.y = 0;
                currSegmentDir = SafeNormalize(currSegmentDir);

                var cross = (currSegmentDir.x * prevSegmentDir.z) - (prevSegmentDir.x * currSegmentDir.z);

                var prevSegmentNormX = -prevSegmentDir.z;
                var prevSegmentNormZ = prevSegmentDir.x;

                var currSegmentNormX = -currSegmentDir.z;
                var currSegmentNormZ = currSegmentDir.x;

                var cornerMiterX = (prevSegmentNormX + currSegmentNormX) * 0.5f;
                var cornerMiterZ = (prevSegmentNormZ + currSegmentNormZ) * 0.5f;
                var cornerMiterSqMag = (cornerMiterX * cornerMiterX) + (cornerMiterZ * cornerMiterZ);

                var bevel = cornerMiterSqMag * miterLimit * miterLimit < 1.0f;

                if (cornerMiterSqMag > math.EPSILON)
                {
                    var scale = 1.0f / cornerMiterSqMag;
                    cornerMiterX *= scale;
                    cornerMiterZ *= scale;
                }

                if (bevel && cross < 0.0f)
                {
                    if (numOutVerts + 2 > maxOutVerts)
                    {
                        return 0;
                    }

                    var d = (1.0f - ((prevSegmentDir.x * currSegmentDir.x) + (prevSegmentDir.z * currSegmentDir.z))) * 0.5f;

                    outVerts[numOutVerts++] = new float3(
                        vertB.x + ((-prevSegmentNormX + (prevSegmentDir.x * d)) * offset),
                        vertB.y,
                        vertB.z + ((-prevSegmentNormZ + (prevSegmentDir.z * d)) * offset));

                    outVerts[numOutVerts++] = new float3(
                        vertB.x + ((-currSegmentNormX - (currSegmentDir.x * d)) * offset),
                        vertB.y,
                        vertB.z + ((-currSegmentNormZ - (currSegmentDir.z * d)) * offset));
                }
                else
                {
                    if (numOutVerts + 1 > maxOutVerts)
                    {
                        return 0;
                    }

                    outVerts[numOutVerts++] = new float3(
                        vertB.x - (cornerMiterX * offset),
                        vertB.y,
                        vertB.z - (cornerMiterZ * offset));
                }
            }

            return numOutVerts;
        }

        /// <summary>
        /// Sorts the given data in-place using insertion sort.
        /// </summary>
        /// <param name="data">The buffer to sort.</param>
        /// <param name="dataLength">The number of elements in <paramref name="data"/> to include in the sort.</param>
        private static void InsertionSort(byte* data, int dataLength)
        {
            for (var valueIndex = 1; valueIndex < dataLength; valueIndex++)
            {
                var value = data[valueIndex];
                int insertionIndex;
                for (insertionIndex = valueIndex - 1; insertionIndex >= 0 && data[insertionIndex] > value; insertionIndex--)
                {
                    data[insertionIndex + 1] = data[insertionIndex];
                }

                data[insertionIndex + 1] = value;
            }
        }

        /// <summary>
        /// Tests whether a 2D point lies inside a convex polygon projected onto the xz-plane.
        /// </summary>
        /// <param name="numVerts">The number of vertices that define the polygon.</param>
        /// <param name="verts">The polygon vertices [Form: (x, y, z) * <paramref name="numVerts"/>].</param>
        /// <param name="point">The xz-plane point to evaluate (y is ignored).</param>
        /// <returns><see langword="true"/> if the point lies inside the polygon; otherwise, <see langword="false"/>.</returns>
        private static bool PointInPoly(int numVerts, float3* verts, float3 point)
        {
            var inPoly = false;
            for (int i = 0, j = numVerts - 1; i < numVerts; j = i++)
            {
                var vi = verts[i];
                var vj = verts[j];

                if ((vi.z > point.z) == (vj.z > point.z))
                {
                    continue;
                }

                if (point.x < ((vj.x - vi.x) * (point.z - vi.z) / (vj.z - vi.z)) + vi.x)
                {
                    inPoly = !inPoly;
                }
            }

            return inPoly;
        }

        /// <summary>
        /// Normalizes a vector only when its magnitude exceeds a small epsilon.
        /// </summary>
        /// <param name="v">The vector to normalize.</param>
        /// <returns>The normalized vector, or the original value when its magnitude is below the epsilon.</returns>
        private static float3 SafeNormalize(float3 v)
        {
            var sqMag = math.lengthsq(v);
            if (sqMag > math.EPSILON)
            {
                return math.normalize(v);
            }

            return v;
        }
    }
}
