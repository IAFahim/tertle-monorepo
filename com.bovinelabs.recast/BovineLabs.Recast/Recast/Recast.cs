// <copyright file="Recast.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary> Recast heightfield allocation and creation methods. </summary>
    public static unsafe partial class Recast
    {
        private const int MaxLayers = RCNotConnected - 1;
        private const int MaxHeight = 0xffff;

        private static readonly int[] DirOffsetX = { -1, 0, 1, 0 };
        private static readonly int[] DirOffsetY = { 0, 1, 0, -1 };
        private static readonly int[] Dirs = { 3, 0, -1, 2, 1 };

        /// <summary> Allocates a heightfield object. </summary>
        /// <param name="allocator">The allocator to use for the allocation.</param>
        /// <returns>A heightfield that is ready for initialization, or null on failure.</returns>
        /// <remarks> Equivalent to rcAllocHeightfield() in C++. </remarks>
        public static RcHeightfield* AllocHeightfield(Allocator allocator)
        {
            var heightfield = (RcHeightfield*)AllocatorManager.Allocate(allocator, sizeof(RcHeightfield), UnsafeUtility.AlignOf<RcHeightfield>());
            *heightfield = new RcHeightfield(allocator);
            return heightfield;
        }

        /// <summary> Frees the specified heightfield object. </summary>
        /// <param name="heightfield">The heightfield to free.</param>
        /// <remarks> Equivalent to rcFreeHeightField() in C++. </remarks>
        public static void FreeHeightfield(RcHeightfield* heightfield)
        {
            heightfield->Dispose();
            AllocatorManager.Free(heightfield->Allocator, heightfield);
        }

        /// <summary> Allocates a compact heightfield object. </summary>
        /// <param name="allocator">The allocator to use for the allocation.</param>
        /// <returns>A compact heightfield that is ready for initialization, or null on failure.</returns>
        /// <remarks> Equivalent to rcAllocCompactHeightfield() in C++. </remarks>
        public static RcCompactHeightfield* AllocCompactHeightfield(Allocator allocator)
        {
            var chf = (RcCompactHeightfield*)AllocatorManager.Allocate(allocator, sizeof(RcCompactHeightfield), UnsafeUtility.AlignOf<RcCompactHeightfield>());
            *chf = new RcCompactHeightfield(allocator);
            return chf;
        }

        /// <summary> Frees the specified compact heightfield object. </summary>
        /// <param name="chf">The compact heightfield to free.</param>
        /// <remarks> Equivalent to rcFreeCompactHeightfield() in C++. </remarks>
        public static void FreeCompactHeightfield(RcCompactHeightfield* chf)
        {
            chf->Dispose();
            AllocatorManager.Free(chf->Allocator, chf);
        }

        /// <summary>Allocates a contour set object.</summary>
        /// <param name="allocator">The allocator to use for the allocation.</param>
        /// <returns>A contour set that is ready for initialization, or null on failure.</returns>
        /// <remarks>Equivalent to rcAllocContourSet() in C++.</remarks>
        public static RcContourSet* AllocContourSet(Allocator allocator)
        {
            var cset = (RcContourSet*)AllocatorManager.Allocate(allocator, sizeof(RcContourSet), UnsafeUtility.AlignOf<RcContourSet>());
            *cset = new RcContourSet(allocator);
            return cset;
        }

        /// <summary>Frees the specified contour set.</summary>
        /// <param name="cset">The contour set to free.</param>
        /// <remarks>Equivalent to rcFreeContourSet() in C++.</remarks>
        public static void FreeContourSet(RcContourSet* cset)
        {
            cset->Dispose();
            AllocatorManager.Free(cset->Allocator, cset);
        }

        /// <summary>Allocates a polygon mesh object.</summary>
        /// <param name="allocator">The allocator to use for the allocation.</param>
        /// <returns>A polygon mesh that is ready for initialization, or null on failure.</returns>
        /// <remarks>Equivalent to rcAllocPolyMesh() in C++.</remarks>
        public static RcPolyMesh* AllocPolyMesh(Allocator allocator)
        {
            var pmesh = (RcPolyMesh*)AllocatorManager.Allocate(allocator, sizeof(RcPolyMesh), UnsafeUtility.AlignOf<RcPolyMesh>());
            *pmesh = new RcPolyMesh(allocator);
            return pmesh;
        }

        /// <summary>Frees the specified polygon mesh.</summary>
        /// <param name="pmesh">The polygon mesh to free.</param>
        /// <remarks>Equivalent to rcFreePolyMesh() in C++.</remarks>
        public static void FreePolyMesh(RcPolyMesh* pmesh)
        {
            pmesh->Dispose();
            AllocatorManager.Free(pmesh->Allocator, pmesh);
        }

        /// <summary>Allocates a detail mesh object.</summary>
        /// <param name="allocator">The allocator to use for the allocation.</param>
        /// <returns>A detail mesh that is ready for initialization, or null on failure.</returns>
        /// <remarks>Equivalent to rcAllocPolyMeshDetail() in C++.</remarks>
        public static RcPolyMeshDetail* AllocPolyMeshDetail(Allocator allocator)
        {
            var dmesh = (RcPolyMeshDetail*)AllocatorManager.Allocate(allocator, sizeof(RcPolyMeshDetail), UnsafeUtility.AlignOf<RcPolyMeshDetail>());
            *dmesh = new RcPolyMeshDetail(allocator);
            return dmesh;
        }

        /// <summary>Frees the specified detail mesh.</summary>
        /// <param name="dmesh">The detail mesh to free.</param>
        /// <remarks>Equivalent to rcFreePolyMeshDetail() in C++.</remarks>
        public static void FreePolyMeshDetail(RcPolyMeshDetail* dmesh)
        {
            dmesh->Dispose();
            AllocatorManager.Free(dmesh->Allocator, dmesh);
        }

        /// <summary>Allocates a heightfield layer set.</summary>
        /// <param name="allocator">The allocator to use for the allocation.</param>
        /// <returns>A heightfield layer set that is ready for initialization, or null on failure.</returns>
        /// <remarks>Equivalent to rcAllocHeightfieldLayerSet() in C++.</remarks>
        public static RcHeightfieldLayerSet* AllocHeightfieldLayerSet(Allocator allocator)
        {
            var lset = (RcHeightfieldLayerSet*)AllocatorManager.Allocate(allocator, sizeof(RcHeightfieldLayerSet),
                UnsafeUtility.AlignOf<RcHeightfieldLayerSet>());
            *lset = new RcHeightfieldLayerSet(allocator);
            return lset;
        }

        /// <summary>Frees the specified heightfield layer set.</summary>
        /// <param name="lset">The heightfield layer set to free.</param>
        /// <remarks>Equivalent to rcFreeHeightfieldLayerSet() in C++.</remarks>
        public static void FreeHeightfieldLayerSet(RcHeightfieldLayerSet* lset)
        {
            lset->Dispose();
            AllocatorManager.Free(lset->Allocator, lset);
        }

        /// <summary> Initializes a new heightfield. </summary>
        /// <param name="heightfield">The heightfield to initialize.</param>
        /// <param name="sizeX">The width of the field along the x-axis [Limit: &gt;= 0] [Units: vx].</param>
        /// <param name="sizeZ">The height of the field along the z-axis [Limit: &gt;= 0] [Units: vx].</param>
        /// <param name="minBounds">The minimum bounds of the field's AABB [(x, y, z)] [Units: wu].</param>
        /// <param name="maxBounds">The maximum bounds of the field's AABB [(x, y, z)] [Units: wu].</param>
        /// <param name="cellSize">The xz-plane cell size to use for the field [Limit: &gt; 0] [Units: wu].</param>
        /// <param name="cellHeight">The y-axis cell size to use for the field [Limit: &gt; 0] [Units: wu].</param>
        /// <remarks> Equivalent to rcCreateHeightfield() in C++. </remarks>
        public static void CreateHeightfield(
            RcHeightfield* heightfield, int sizeX, int sizeZ, in float3 minBounds, in float3 maxBounds, float cellSize, float cellHeight)
        {
            heightfield->Width = sizeX;
            heightfield->Height = sizeZ;
            heightfield->Bmin = minBounds;
            heightfield->Bmax = maxBounds;
            heightfield->Cs = cellSize;
            heightfield->Ch = cellHeight;

            // Allocate spans array
            var spanArraySize = sizeX * sizeZ;
            heightfield->Spans = (RcSpan**)AllocatorManager.Allocate(heightfield->Allocator, sizeof(RcSpan*) * spanArraySize, UnsafeUtility.AlignOf<IntPtr>());

            // Initialize all span pointers to null
            UnsafeUtility.MemClear(heightfield->Spans, sizeof(RcSpan*) * spanArraySize);
        }

        /// <summary>
        /// Sets the area id of all triangles with a slope below the specified value to RC_WALKABLE_AREA.
        /// Only sets the area id's for the walkable triangles. Does not alter the area id's for un-walkable triangles.
        /// </summary>
        /// <param name="walkableSlopeAngle">The maximum slope that is considered walkable [Limits: 0 &lt;= value &lt; 90] [Units: Degrees].</param>
        /// <param name="verts">The vertices [(x, y, z) * numVerts].</param>
        /// <param name="tris">The triangle vertex indices [(vertA, vertB, vertC) * numTris].</param>
        /// <param name="triAreaIDs">The triangle area ids [Length: >= numTris].</param>
        /// <param name="numTris">The number of triangles.</param>
        /// <remarks>Equivalent to rcMarkWalkableTriangles() in C++.</remarks>
        public static void MarkWalkableTriangles(float walkableSlopeAngle, float3* verts, int3* tris, byte* triAreaIDs, int numTris)
        {
            var walkableThr = math.cos(walkableSlopeAngle / 180.0f * math.PI);

            for (var i = 0; i < numTris; i++)
            {
                var tri = tris[i];
                var norm = CalculateTriangleNormal(verts[tri.x], verts[tri.y], verts[tri.z]);

                // Check if the face is walkable (Y component of normal > threshold)
                if (norm.y > walkableThr)
                {
                    triAreaIDs[i] = RCWalkableArea;
                }
            }
        }

        /// <summary>
        /// Sets the area id of all triangles with a slope greater than or equal to the specified value to RC_NULL_AREA.
        /// Only sets the area id's for the un-walkable triangles. Does not alter the area id's for walkable triangles.
        /// </summary>
        /// <param name="walkableSlopeAngle">The maximum slope that is considered walkable [Limits: 0 &lt;= value &lt; 90] [Units: Degrees].</param>
        /// <param name="verts">The vertices [(x, y, z) * numVerts].</param>
        /// <param name="tris">The triangle vertex indices [(vertA, vertB, vertC) * numTris].</param>
        /// <param name="triAreaIDs">The triangle area ids [Length: >= numTris].</param>
        /// <param name="numTris">The number of triangles.</param>
        /// <remarks>Equivalent to rcClearUnwalkableTriangles() in C++.</remarks>
        public static void ClearUnwalkableTriangles(float walkableSlopeAngle, float3* verts, int3* tris, byte* triAreaIDs, int numTris)
        {
            // The minimum Y value for a face normal of a triangle with a walkable slope
            var walkableLimitY = math.cos(walkableSlopeAngle / 180.0f * math.PI);

            for (var i = 0; i < numTris; i++)
            {
                var tri = tris[i];
                var faceNormal = CalculateTriangleNormal(verts[tri.x], verts[tri.y], verts[tri.z]);

                // Check if the face is unwalkable
                if (faceNormal.y <= walkableLimitY)
                {
                    triAreaIDs[i] = RCNullArea;
                }
            }
        }

        /// <summary>
        /// Calculates the grid size based on the bounding box and grid cell size.
        /// </summary>
        /// <param name="minBounds">The minimum bounds of the AABB [(x, y, z)] [Units: wu].</param>
        /// <param name="maxBounds">The maximum bounds of the AABB [(x, y, z)] [Units: wu].</param>
        /// <param name="cellSize">The xz-plane cell size [Limit: > 0] [Units: wu].</param>
        /// <param name="sizeX">The width along the x-axis [Limit: >= 0] [Units: vx].</param>
        /// <param name="sizeZ">The height along the z-axis [Limit: >= 0] [Units: vx].</param>
        /// <remarks>Equivalent to rcCalcGridSize() in C++.</remarks>
        public static void CalcGridSize(in float3 minBounds, in float3 maxBounds, float cellSize, out int sizeX, out int sizeZ)
        {
            sizeX = (int)(((maxBounds.x - minBounds.x) / cellSize) + 0.5f);
            sizeZ = (int)(((maxBounds.z - minBounds.z) / cellSize) + 0.5f);
        }

        /// <summary>
        /// Calculates the bounding box of an array of vertices.
        /// </summary>
        /// <param name="verts">An array of vertices [(x, y, z) * numVerts].</param>
        /// <param name="numVerts">The number of vertices in the verts array.</param>
        /// <param name="minBounds">The minimum bounds of the AABB [(x, y, z)] [Units: wu].</param>
        /// <param name="maxBounds">The maximum bounds of the AABB [(x, y, z)] [Units: wu].</param>
        /// <remarks>Equivalent to rcCalcBounds() in C++.</remarks>
        public static void CalcBounds(float3* verts, int numVerts, out float3 minBounds, out float3 maxBounds)
        {
            // Calculate bounding box
            minBounds = verts[0];
            maxBounds = verts[0];

            for (var i = 1; i < numVerts; i++)
            {
                minBounds = math.min(minBounds, verts[i]);
                maxBounds = math.max(maxBounds, verts[i]);
            }
        }

        /// <summary>
        /// Returns the number of spans contained in the specified heightfield.
        /// </summary>
        /// <param name="heightfield">An initialized heightfield.</param>
        /// <returns>The number of spans in the heightfield.</returns>
        /// <remarks>Equivalent to rcGetHeightFieldSpanCount() in C++.</remarks>
        public static int GetHeightFieldSpanCount(RcHeightfield* heightfield)
        {
            var numCols = heightfield->Width * heightfield->Height;
            var spanCount = 0;

            for (var columnIndex = 0; columnIndex < numCols; columnIndex++)
            {
                for (var span = heightfield->Spans[columnIndex]; span != null; span = span->Next)
                {
                    if (span->Area != RCNullArea)
                    {
                        spanCount++;
                    }
                }
            }

            return spanCount;
        }

        /// <summary>
        /// Builds a compact heightfield representing open space, from a heightfield representing solid space.
        /// This is just the beginning of the process of fully building a compact heightfield.
        /// Various filters may be applied, then the distance field and regions built.
        /// </summary>
        /// <param name="walkableHeight">Minimum floor to 'ceiling' height that will still allow the floor area to be considered walkable [Limit: >= 3] [Units: vx].</param>
        /// <param name="walkableClimb">Maximum ledge height that is considered to still be traversable [Limit: >=0] [Units: vx].</param>
        /// <param name="heightfield">The heightfield to be compacted.</param>
        /// <param name="compactHeightfield">The resulting compact heightfield (must be pre-allocated).</param>
        /// <returns>True if the operation completed successfully.</returns>
        /// <remarks>Equivalent to rcBuildCompactHeightfield() in C++.</remarks>
        public static bool BuildCompactHeightfield(int walkableHeight, int walkableClimb, RcHeightfield* heightfield, RcCompactHeightfield* compactHeightfield)
        {
            var xSize = heightfield->Width;
            var zSize = heightfield->Height;
            var spanCount = GetHeightFieldSpanCount(heightfield);

            // Fill in header
            compactHeightfield->Width = xSize;
            compactHeightfield->Height = zSize;
            compactHeightfield->SpanCount = spanCount;
            compactHeightfield->WalkableHeight = walkableHeight;
            compactHeightfield->WalkableClimb = walkableClimb;
            compactHeightfield->MaxRegions = 0;
            compactHeightfield->BMin = heightfield->Bmin;
            compactHeightfield->BMax = heightfield->Bmax;
            compactHeightfield->BMax.y += walkableHeight * heightfield->Ch;
            compactHeightfield->CellSize = heightfield->Cs;
            compactHeightfield->CellHeight = heightfield->Ch;

            // Allocate arrays
            var cellCount = xSize * zSize;
            compactHeightfield->Cells = (RcCompactCell*)AllocatorManager.Allocate(compactHeightfield->Allocator, sizeof(RcCompactCell) * cellCount,
                UnsafeUtility.AlignOf<RcCompactCell>());

            UnsafeUtility.MemClear(compactHeightfield->Cells, sizeof(RcCompactCell) * cellCount);

            compactHeightfield->Spans = (RcCompactSpan*)AllocatorManager.Allocate(compactHeightfield->Allocator, sizeof(RcCompactSpan) * spanCount,
                UnsafeUtility.AlignOf<RcCompactSpan>());

            UnsafeUtility.MemClear(compactHeightfield->Spans, sizeof(RcCompactSpan) * spanCount);

            compactHeightfield->Areas = (byte*)AllocatorManager.Allocate(compactHeightfield->Allocator, sizeof(byte) * spanCount, UnsafeUtility.AlignOf<byte>());
            UnsafeUtility.MemSet(compactHeightfield->Areas, RCNullArea, spanCount);

            // Fill in cells and spans
            var currentCellIndex = 0;
            var numColumns = xSize * zSize;

            for (var columnIndex = 0; columnIndex < numColumns; columnIndex++)
            {
                var span = heightfield->Spans[columnIndex];

                // If there are no spans at this cell, just leave the data to index=0, count=0
                if (span == null)
                {
                    continue;
                }

                var cell = &compactHeightfield->Cells[columnIndex];
                cell->Index = (uint)currentCellIndex;
                cell->Count = 0;

                for (; span != null; span = span->Next)
                {
                    if (span->Area != RCNullArea)
                    {
                        var bot = (int)span->SMax;
                        var top = span->Next != null ? (int)span->Next->SMin : MaxHeight;

                        compactHeightfield->Spans[currentCellIndex].Y = (ushort)math.clamp(bot, 0, 0xffff);
                        compactHeightfield->Spans[currentCellIndex].H = (uint)math.clamp(top - bot, 0, 0xff);
                        compactHeightfield->Areas[currentCellIndex] = (byte)span->Area;
                        currentCellIndex++;
                        cell->Count++;
                    }
                }
            }

            // Find neighbor connections
            var maxLayerIndex = 0;
            var zStride = xSize; // for readability

            for (var z = 0; z < zSize; z++)
            {
                for (var x = 0; x < xSize; x++)
                {
                    var cell = compactHeightfield->Cells[x + (z * zStride)];
                    for (int i = (int)cell.Index, ni = (int)(cell.Index + cell.Count); i < ni; i++)
                    {
                        var span = &compactHeightfield->Spans[i];

                        for (var dir = 0; dir < 4; dir++)
                        {
                            SetCon(span, dir, RCNotConnected);
                            var neighborX = x + GetDirOffsetX(dir);
                            var neighborZ = z + GetDirOffsetY(dir);

                            if (neighborX < 0 || neighborZ < 0 || neighborX >= xSize || neighborZ >= zSize)
                            {
                                continue;
                            }

                            var neighborCell = compactHeightfield->Cells[neighborX + (neighborZ * zStride)];
                            for (int k = (int)neighborCell.Index, nk = (int)(neighborCell.Index + neighborCell.Count); k < nk; k++)
                            {
                                var neighborSpan = compactHeightfield->Spans[k];
                                var bot = math.max(span->Y, (int)neighborSpan.Y);
                                var top = math.min(span->Y + (int)span->H, neighborSpan.Y + (int)neighborSpan.H);

                                if (top - bot >= walkableHeight && math.abs(neighborSpan.Y - span->Y) <= walkableClimb)
                                {
                                    var layerIndex = k - (int)neighborCell.Index;
                                    if (layerIndex < 0 || layerIndex > MaxLayers)
                                    {
                                        maxLayerIndex = math.max(maxLayerIndex, layerIndex);
                                        continue;
                                    }

                                    SetCon(span, dir, layerIndex);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (maxLayerIndex > MaxLayers)
            {
                Debug.LogError($"rcBuildCompactHeightfield: Heightfield has too many layers {maxLayerIndex} (max: {MaxLayers})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sets the neighbor connection data for the specified direction.
        /// </summary>
        /// <param name="span">The span to update.</param>
        /// <param name="direction">The direction to set [Limits: 0 &lt;= value &lt; 4].</param>
        /// <param name="neighborIndex">The index of the neighbor span.</param>
        /// <remarks>Equivalent to rcSetCon() inline function in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCon(RcCompactSpan* span, int direction, int neighborIndex)
        {
            var shift = (uint)direction * 6;
            var con = span->Con;
            span->Con = (con & ~(0x3fu << (int)shift)) | (((uint)neighborIndex & 0x3f) << (int)shift);
        }

        /// <summary>
        /// Gets neighbor connection data for the specified direction.
        /// </summary>
        /// <param name="span">The span to check.</param>
        /// <param name="direction">The direction to check [Limits: 0 &lt;= value &lt; 4].</param>
        /// <returns>The neighbor connection data for the specified direction, or RC_NOT_CONNECTED if there is no connection.</returns>
        /// <remarks>Equivalent to rcGetCon() inline function in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCon(RcCompactSpan span, int direction)
        {
            var shift = (uint)direction * 6;
            return (int)((span.Con >> (int)shift) & 0x3f);
        }

        /// <summary>
        /// Gets the standard width (x-axis) offset for the specified direction.
        /// </summary>
        /// <param name="direction">The direction [Limits: 0 &lt;= value &lt; 4].</param>
        /// <returns>The width offset to apply to the current cell position to move in the direction.</returns>
        /// <remarks>Equivalent to rcGetDirOffsetX() inline function in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDirOffsetX(int direction)
        {
            return DirOffsetX[direction & 0x03];
        }

        /// <summary>
        /// Gets the standard height (z-axis) offset for the specified direction.
        /// </summary>
        /// <param name="direction">The direction [Limits: 0 &lt;= value &lt; 4].</param>
        /// <returns>The height offset to apply to the current cell position to move in the direction.</returns>
        /// <remarks>Equivalent to rcGetDirOffsetY() inline function in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDirOffsetY(int direction)
        {
            return DirOffsetY[direction & 0x03];
        }

        /// <summary>
        /// Gets the direction for the specified offset. One of x and y should be 0.
        /// </summary>
        /// <param name="offsetX">The x offset [Limits: -1 &lt;= value &lt;= 1].</param>
        /// <param name="offsetZ">The z offset [Limits: -1 &lt;= value &lt;= 1].</param>
        /// <returns>The direction that represents the offset.</returns>
        /// <remarks>Equivalent to rcGetDirForOffset() inline function in C++.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDirForOffset(int offsetX, int offsetZ)
        {
            return Dirs[((offsetZ + 1) << 1) + offsetX];
        }

        /// <summary>
        /// Calculates the normalized normal vector of a triangle.
        /// </summary>
        /// <param name="v0">Triangle vertex 0 [(x, y, z)].</param>
        /// <param name="v1">Triangle vertex 1 [(x, y, z)].</param>
        /// <param name="v2">Triangle vertex 2 [(x, y, z)].</param>
        /// <returns>The normalized triangle normal.</returns>
        private static float3 CalculateTriangleNormal(float3 v0, float3 v1, float3 v2)
        {
            var e0 = v1 - v0;
            var e1 = v2 - v0;
            var faceNormal = math.cross(e0, e1);
            return math.normalize(faceNormal);
        }

        // Geometric helper functions
        private static int Prev(int i, int n) => i - 1 >= 0 ? i - 1 : n - 1;

        private static int Next(int i, int n) => i + 1 < n ? i + 1 : 0;

        private static int Area2(int* a, int* b, int* c)
        {
            return ((b[0] - a[0]) * (c[2] - a[2])) - ((c[0] - a[0]) * (b[2] - a[2]));
        }

        private static bool Xorb(bool x, bool y)
        {
            return !x ^ !y;
        }

        private static bool Left(int* a, int* b, int* c)
        {
            return Area2(a, b, c) < 0;
        }

        private static bool LeftOn(int* a, int* b, int* c)
        {
            return Area2(a, b, c) <= 0;
        }

        private static bool Collinear(int* a, int* b, int* c)
        {
            return Area2(a, b, c) == 0;
        }

        private static bool IntersectProp(int* a, int* b, int* c, int* d)
        {
            // Eliminate improper cases
            if (Collinear(a, b, c) || Collinear(a, b, d) || Collinear(c, d, a) || Collinear(c, d, b))
            {
                return false;
            }

            return Xorb(Left(a, b, c), Left(a, b, d)) && Xorb(Left(c, d, a), Left(c, d, b));
        }

        private static bool Between(int* a, int* b, int* c)
        {
            if (!Collinear(a, b, c))
            {
                return false;
            }

            // If ab not vertical, check betweenness on x; else on z
            if (a[0] != b[0])
            {
                return (a[0] <= c[0] && c[0] <= b[0]) || (a[0] >= c[0] && c[0] >= b[0]);
            }

            return (a[2] <= c[2] && c[2] <= b[2]) || (a[2] >= c[2] && c[2] >= b[2]);
        }

        private static bool Intersect(int* a, int* b, int* c, int* d)
        {
            if (IntersectProp(a, b, c, d))
            {
                return true;
            }

            if (Between(a, b, c) || Between(a, b, d) || Between(c, d, a) || Between(c, d, b))
            {
                return true;
            }

            return false;
        }

        private static bool VEqual(int* a, int* b)
        {
            return a[0] == b[0] && a[2] == b[2];
        }
    }
}
