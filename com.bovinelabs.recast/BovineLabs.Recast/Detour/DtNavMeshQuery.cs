// <copyright file="DtNavMeshQuery.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using Random = Unity.Mathematics.Random;

    /// <summary>
    /// Provides the ability to perform pathfinding related queries against a navigation mesh.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For methods that support undersized buffers, if the buffer is too small to hold the entire result set the
    /// returned status includes <see cref="DtStatus.BufferTooSmall"/>.
    /// </para>
    /// <para>
    /// Constant member functions can be used by multiple clients without side effects. For example, they will not
    /// alter the closed list or interfere with an in-progress sliced path query.
    /// </para>
    /// <para>
    /// Walls are polygon segments that are considered impassable, while portals are passable segments between
    /// polygons. Depending on the <see cref="DtQueryFilter"/> used for a query, a portal can be treated as a wall.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DtNavMeshQuery : IDisposable
    {
        private const float HScale = 0.999f; // Search heuristic scale
        private const int MaxNeighbourTiles = 32;

        private readonly AllocatorManager.AllocatorHandle allocator;

        private DtNavMesh* navMesh; // Pointer to navmesh data
        private DtQueryData queryData; // Sliced query state
        private DtNodePool* tinyNodePool; // Pointer to small node pool for local searches
        private DtNodePool* nodePool; // Pointer to node pool for pathfinding
        private DtNodeQueue* openList; // Pointer to open list queue for pathfinding

        /// <summary>
        /// Initializes a new instance of the <see cref="DtNavMeshQuery"/> struct.
        /// </summary>
        /// <param name="nav">
        /// Pointer to the <see cref="DtNavMesh"/> object to use for all queries. Optional; if not set, call
        /// <see cref="ReplaceNavMeshTarget"/> before executing queries.
        /// </param>
        /// <param name="maxNodes">Maximum number of search nodes. [Limits: 0 &lt; value &lt;= 65,535].</param>
        /// <param name="allocator"> The allocator to use. </param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Must be the first function called after construction, before any other function is used.</para>
        /// <para>The query object can be re-initialized multiple times.</para>
        /// </remarks>
        public DtNavMeshQuery(DtNavMesh* nav, int maxNodes, AllocatorManager.AllocatorHandle allocator)
        {
            this.allocator = allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (maxNodes >= DtNodePool.DTNullIDX.Value || maxNodes > (1 << DtNodePool.DTNodeParentBits) - 1)
            {
                throw new ArgumentException("InvalidParam");
            }
#endif

            this.navMesh = nav;

            this.nodePool = AllocatorManager.Allocate<DtNodePool>(this.allocator);
            *this.nodePool = new DtNodePool(maxNodes, math.ceilpow2(maxNodes / 4));

            this.tinyNodePool = AllocatorManager.Allocate<DtNodePool>(this.allocator);
            *this.tinyNodePool = new DtNodePool(64, 32);

            this.openList = AllocatorManager.Allocate<DtNodeQueue>(this.allocator);
            *this.openList = new DtNodeQueue(maxNodes, allocator);

            this.queryData = default;
        }

        /// <summary>
        /// Allocates a navigation mesh query object using the Detour allocator.
        /// </summary>
        /// <returns>An allocated query object, or <see langword="null"/> on failure.</returns>
        public static DtNavMeshQuery* Create(DtNavMesh* nav, int maxNodes, AllocatorManager.AllocatorHandle allocator)
        {
            var query = AllocatorManager.Allocate<DtNavMeshQuery>(allocator);
            *query = new DtNavMeshQuery(nav, maxNodes, allocator);
            return query;
        }

        /// <summary>
        /// Frees the specified navigation mesh query object using the Detour allocator.
        /// </summary>
        /// <param name="query">A query object allocated via <see cref="Create"/>.</param>
        public static void Free(DtNavMeshQuery* query)
        {
            if (query == null)
            {
                return;
            }

            var alloc = query->allocator;
            query->Dispose();
            AllocatorManager.Free(alloc, query);
        }

        /// <summary>
        /// Disposes the query object and frees associated memory.
        /// </summary>
        public void Dispose()
        {
            if (this.tinyNodePool != null)
            {
                this.tinyNodePool->Dispose();
                AllocatorManager.Free(this.allocator, this.tinyNodePool);
                this.tinyNodePool = null;
            }

            if (this.nodePool != null)
            {
                this.nodePool->Dispose();
                AllocatorManager.Free(this.allocator, this.nodePool);
                this.nodePool = null;
            }

            if (this.openList != null)
            {
                this.openList->Dispose();
                AllocatorManager.Free(this.allocator, this.openList);
                this.openList = null;
            }
        }

        /// <summary>
        /// Replaces the navigation mesh target used by this query.
        /// </summary>
        /// <param name="mesh">Pointer to the <see cref="DtNavMesh"/> to query.</param>
        public void ReplaceNavMeshTarget(DtNavMesh* mesh)
        {
            this.navMesh = mesh;
        }

        /// <summary>
        /// Finds a path from the start polygon to the end polygon.
        /// </summary>
        /// <param name="startRef">The reference id of the start polygon.</param>
        /// <param name="endRef">The reference id of the end polygon.</param>
        /// <param name="startPos">A position within the start polygon. [(x, y, z)].</param>
        /// <param name="endPos">A position within the end polygon. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="path">An ordered list of polygon references representing the path (start to end).</param>
        /// <param name="pathCount">The number of polygons stored in <paramref name="path"/>.</param>
        /// <param name="maxPath">The maximum number of polygons the <paramref name="path"/> array can hold. [Limit: &gt;= 1].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>If the end polygon cannot be reached through the navigation graph, the last polygon in the path is the
        /// closest polygon to the end reference.</para>
        /// <para>If <paramref name="path"/> is too small to hold the full result, it is filled from the start polygon toward
        /// the end polygon.</para>
        /// <para>The start and end positions are used when calculating traversal costs; their y-values affect the result.</para>
        /// </remarks>
        public DtStatus FindPath(
            DtPolyRef startRef, DtPolyRef endRef, in float3 startPos, in float3 endPos, ref DtQueryFilter filter, DtPolyRef* path, out int pathCount,
            int maxPath)
        {
            pathCount = 0;

            if (!this.navMesh->IsValidPolyRef(startRef) || !this.navMesh->IsValidPolyRef(endRef) || !Detour.IsFinite(startPos) || !Detour.IsFinite(endPos) ||
                path == null || maxPath <= 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (startRef == endRef)
            {
                path[0] = startRef;
                pathCount = 1;
                return DtStatus.Success;
            }

            this.nodePool->Clear();
            this.openList->Clear();

            var startNode = this.nodePool->GetNode(startRef);
            startNode->pos = startPos;
            startNode->ParentIndex = 0;
            startNode->cost = 0;
            startNode->total = math.distance(startPos, endPos) * HScale;
            startNode->id = startRef;
            startNode->Flags = DtNodeFlags.DT_NODE_OPEN;
            this.openList->Push(startNode);

            var lastBestNode = startNode;
            var lastBestNodeCost = startNode->total;
            var outOfNodes = false;

            while (!this.openList->Empty)
            {
                var bestNode = this.openList->Pop();
                bestNode->Flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode->Flags |= DtNodeFlags.DT_NODE_CLOSED;

                if (bestNode->id == endRef)
                {
                    lastBestNode = bestNode;
                    break;
                }

                var bestRef = bestNode->id;
                this.navMesh->GetTileAndPolyByRefUnsafe(bestRef, out var bestTile, out var bestPoly);

                DtPolyRef parentRef = 0;
                DtMeshTile* parentTile = null;
                DtPoly* parentPoly = null;
                if (bestNode->ParentIndex != 0)
                {
                    parentRef = this.nodePool->GetNodeAtIdx(bestNode->ParentIndex)->id;
                }

                if (parentRef != 0)
                {
                    this.navMesh->GetTileAndPolyByRefUnsafe(parentRef, out parentTile, out parentPoly);
                }

                for (var i = bestPoly->firstLink; i != Detour.DTNullLink; i = bestTile->links[i].next)
                {
                    var neighbourRef = bestTile->links[i].polyRef;
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    this.navMesh->GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    var neighbourNode = this.nodePool->GetNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        outOfNodes = true;
                        continue;
                    }

                    if (neighbourNode->Flags == 0)
                    {
                        this.GetEdgeMidPoint(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, out neighbourNode->pos);
                    }

                    float cost;
                    if (neighbourRef == endRef)
                    {
                        var curCost = filter.GetCost(bestNode->pos, neighbourNode->pos, parentRef, parentTile, parentPoly, bestRef, bestTile, bestPoly,
                            neighbourRef, neighbourTile, neighbourPoly);

                        var endCost = filter.GetCost(neighbourNode->pos, endPos, bestRef, bestTile, bestPoly, neighbourRef, neighbourTile, neighbourPoly, 0,
                            null, null);

                        cost = bestNode->cost + curCost + endCost;
                    }
                    else
                    {
                        var curCost = filter.GetCost(bestNode->pos, neighbourNode->pos, parentRef, parentTile, parentPoly, bestRef, bestTile, bestPoly,
                            neighbourRef, neighbourTile, neighbourPoly);

                        cost = bestNode->cost + curCost;
                    }

                    var heuristic = math.distance(neighbourNode->pos, endPos) * HScale;
                    var total = cost + heuristic;

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode->total)
                    {
                        continue;
                    }

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_CLOSED) != 0 && total >= neighbourNode->total)
                    {
                        continue;
                    }

                    neighbourNode->ParentIndex = this.nodePool->GetNodeIdx(bestNode);
                    neighbourNode->id = neighbourRef;
                    neighbourNode->Flags &= ~DtNodeFlags.DT_NODE_CLOSED;
                    neighbourNode->cost = cost;
                    neighbourNode->total = total;

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        this.openList->Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode->Flags |= DtNodeFlags.DT_NODE_OPEN;
                        this.openList->Push(neighbourNode);
                    }

                    if (heuristic < lastBestNodeCost)
                    {
                        lastBestNodeCost = heuristic;
                        lastBestNode = neighbourNode;
                    }
                }
            }

            var status = this.GetPathToNode(lastBestNode, path, out pathCount, maxPath);

            if (lastBestNode->id != endRef)
            {
                status |= DtStatus.PartialResult;
            }

            if (outOfNodes)
            {
                status |= DtStatus.OutOfNodes;
            }

            return status;
        }

        /// <summary>
        /// Returns a random location on the navigation mesh.
        /// </summary>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="frand">Function returning a random number in [0..1).</param>
        /// <param name="randomRef">The reference id of the chosen location.</param>
        /// <param name="randomPt">The random location. [(x, y, z)].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Polygons are chosen with probability proportional to their area. The search runs in time linear to the
        /// number of polygons.</para>
        /// </remarks>
        public DtStatus FindRandomPoint(ref DtQueryFilter filter, ref Random frand, out DtPolyRef randomRef, out float3 randomPt)
        {
            randomRef = default;
            randomPt = default;

            // Randomly pick one tile
            DtMeshTile* tile = null;
            var tsum = 0.0f;
            for (var i = 0; i < this.navMesh->GetMaxTiles(); i++)
            {
                var t1 = this.navMesh->GetTile(i);
                if (t1 == null || t1->header == null)
                {
                    continue;
                }

                var area = 1.0f; // Could be tile area too
                tsum += area;
                var u = frand.NextFloat();
                if (u * tsum <= area)
                {
                    tile = t1;
                }
            }

            if (tile == null)
            {
                return DtStatus.Failure;
            }

            // Randomly pick one polygon weighted by polygon area
            DtPoly* poly = null;
            DtPolyRef polyRef = 0;
            var polyRefBase = this.navMesh->GetPolyRefBase(tile);

            var areaSum = 0.0f;
            for (var i = 0; i < tile->header->polyCount; ++i)
            {
                var p = &tile->polys[i];

                // Do not return off-mesh connection polygons
                if (p->GetPolyType() != (byte)DtPolyTypes.PolytypeGround)
                {
                    continue;
                }

                var pRef = polyRefBase | (DtPolyRef)i;
                if (!filter.PassFilter(pRef, tile, p))
                {
                    continue;
                }

                // Calc area of the polygon
                var polyArea = 0.0f;
                for (var j = 2; j < p->vertCount; ++j)
                {
                    var va = tile->verts[p->verts[0]];
                    var vb = tile->verts[p->verts[j - 1]];
                    var vc = tile->verts[p->verts[j]];
                    polyArea += Detour.TriArea2D(va, vb, vc);
                }

                // Choose random polygon weighted by area, using reservoir sampling
                areaSum += polyArea;
                var u = frand.NextFloat();
                if (u * areaSum <= polyArea)
                {
                    poly = p;
                    polyRef = pRef;
                }
            }

            if (poly == null)
            {
                return DtStatus.Failure;
            }

            // Randomly pick point on polygon
            var verts = stackalloc float3[Detour.DTVertsPerPolygon];
            var areas = stackalloc float[Detour.DTVertsPerPolygon];
            verts[0] = tile->verts[poly->verts[0]];
            for (var j = 1; j < poly->vertCount; ++j)
            {
                verts[j] = tile->verts[poly->verts[j]];
            }

            var s = frand.NextFloat();
            var t = frand.NextFloat();

            var pt = Detour.RandomPointInConvexPoly(verts, poly->vertCount, areas, s, t);

            this.navMesh->ClosestPointOnPoly(polyRef, pt, out var closestPt, out _);

            randomPt = closestPt;
            randomRef = polyRef;

            return DtStatus.Success;
        }

        /// <summary>
        /// Returns a random location on the navigation mesh within reach of the specified position.
        /// </summary>
        /// <param name="startRef">The reference id of the polygon where the search starts.</param>
        /// <param name="centerPos">The center of the search circle. [(x, y, z)].</param>
        /// <param name="maxRadius">The radius of the search circle.</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="frand">Function returning a random number in [0..1).</param>
        /// <param name="randomRef">The reference id of the chosen location.</param>
        /// <param name="randomPt">The random location. [(x, y, z)].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Polygons are chosen with probability proportional to their area. The search runs in time linear to the
        /// number of polygons. The location is not constrained strictly to the circle, but the circle limits the polygons
        /// visited during the search.</para>
        /// </remarks>
        public DtStatus FindRandomPointAroundCircle(
            DtPolyRef startRef, in float3 centerPos, float maxRadius, ref DtQueryFilter filter, ref Random frand, out DtPolyRef randomRef, out float3 randomPt)
        {
            randomRef = default;
            randomPt = default;

            // Validate input
            if (!this.navMesh->IsValidPolyRef(startRef) || !Detour.IsFinite(centerPos) || maxRadius < 0 || !math.isfinite(maxRadius))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            this.navMesh->GetTileAndPolyByRefUnsafe(startRef, out var startTile, out var startPoly);
            if (!filter.PassFilter(startRef, startTile, startPoly))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            this.nodePool->Clear();
            this.openList->Clear();

            var startNode = this.nodePool->GetNode(startRef);
            startNode->pos = centerPos;
            startNode->ParentIndex = 0;
            startNode->cost = 0;
            startNode->total = 0;
            startNode->id = startRef;
            startNode->Flags = DtNodeFlags.DT_NODE_OPEN;
            this.openList->Push(startNode);

            var status = DtStatus.Success;
            var radiusSqr = maxRadius * maxRadius;
            var areaSum = 0.0f;

            DtMeshTile* randomTile = null;
            DtPoly* randomPoly = null;
            DtPolyRef randomPolyRef = 0;

            while (!this.openList->Empty)
            {
                var bestNode = this.openList->Pop();
                bestNode->Flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode->Flags |= DtNodeFlags.DT_NODE_CLOSED;

                this.navMesh->GetTileAndPolyByRefUnsafe(bestNode->id, out var bestTile, out var bestPoly);

                // Place random locations on ground
                if (bestPoly->GetPolyType() == (byte)DtPolyTypes.PolytypeGround)
                {
                    // Calc area of the polygon
                    var polyArea = 0.0f;
                    for (var j = 2; j < bestPoly->vertCount; ++j)
                    {
                        var va = bestTile->verts[bestPoly->verts[0]];
                        var vb = bestTile->verts[bestPoly->verts[j - 1]];
                        var vc = bestTile->verts[bestPoly->verts[j]];
                        polyArea += Detour.TriArea2D(va, vb, vc);
                    }

                    // Choose random polygon weighted by area, using reservoir sampling
                    areaSum += polyArea;
                    var u = frand.NextFloat();
                    if (u * areaSum <= polyArea)
                    {
                        randomTile = bestTile;
                        randomPoly = bestPoly;
                        randomPolyRef = bestNode->id;
                    }
                }

                // Get parent poly and tile
                DtPolyRef parentRef = 0;
                DtMeshTile* parentTile = null;
                DtPoly* parentPoly = null;
                if (bestNode->ParentIndex != 0)
                {
                    parentRef = this.nodePool->GetNodeAtIdx(bestNode->ParentIndex)->id;
                    if (parentRef != 0)
                    {
                        this.navMesh->GetTileAndPolyByRefUnsafe(parentRef, out parentTile, out parentPoly);
                    }
                }

                for (var i = bestPoly->firstLink; i != Detour.DTNullLink; i = bestTile->links[i].next)
                {
                    var link = &bestTile->links[i];
                    var neighbourRef = link->polyRef;

                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    this.navMesh->GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    // Find edge and calc distance to the edge
                    if (!Detour.StatusSucceed(this.GetPortalPoints(bestNode->id, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, out var va,
                        out var vb)))
                    {
                        continue;
                    }

                    // If the circle is not touching the next polygon, skip it
                    var distSqr = Detour.DistancePtSegSqr2D(centerPos, va, vb, out _);
                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    var neighbourNode = this.nodePool->GetNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        status |= DtStatus.OutOfNodes;
                        continue;
                    }

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    // Cost
                    if (neighbourNode->Flags == 0)
                    {
                        neighbourNode->pos = math.lerp(va, vb, 0.5f);
                    }

                    var total = bestNode->total + math.distance(bestNode->pos, neighbourNode->pos);

                    // The node is already in open list and the new result is worse, skip
                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode->total)
                    {
                        continue;
                    }

                    neighbourNode->id = neighbourRef;
                    neighbourNode->Flags = neighbourNode->Flags & ~DtNodeFlags.DT_NODE_CLOSED;
                    neighbourNode->ParentIndex = this.nodePool->GetNodeIdx(bestNode);
                    neighbourNode->total = total;

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        this.openList->Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode->Flags = DtNodeFlags.DT_NODE_OPEN;
                        this.openList->Push(neighbourNode);
                    }
                }
            }

            if (randomPoly == null)
            {
                return DtStatus.Failure;
            }

            // Randomly pick point on polygon
            var verts = stackalloc float3[Detour.DTVertsPerPolygon];
            var areas = stackalloc float[Detour.DTVertsPerPolygon];
            verts[0] = randomTile->verts[randomPoly->verts[0]];
            for (var j = 1; j < randomPoly->vertCount; ++j)
            {
                verts[j] = randomTile->verts[randomPoly->verts[j]];
            }

            var s = frand.NextFloat();
            var t = frand.NextFloat();

            var pt = Detour.RandomPointInConvexPoly(verts, randomPoly->vertCount, areas, s, t);
            this.navMesh->ClosestPointOnPoly(randomPolyRef, pt, out var closestPt, out _);

            randomPt = closestPt;
            randomRef = randomPolyRef;

            return status;
        }

        /// <summary>
        /// Finds the closest point on the specified polygon.
        /// </summary>
        /// <param name="polyRef">The reference id of the polygon.</param>
        /// <param name="pos">The position to check. [(x, y, z)].</param>
        /// <param name="closest">The closest point on the polygon. [(x, y, z)].</param>
        /// <param name="posOverPoly">Set to <see langword="true"/> if the position lies over the polygon.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Uses the detail polygons to find the surface height (most accurate). The input position does not have
        /// to lie within the bounds of the polygon or the navigation mesh.</para>
        /// <para>See <see cref="ClosestPointOnPolyBoundary"/> for a limited but faster option.</para>
        /// </remarks>
        public readonly DtStatus ClosestPointOnPoly(DtPolyRef polyRef, in float3 pos, out float3 closest, out bool posOverPoly)
        {
            if (!this.navMesh->IsValidPolyRef(polyRef) || !Detour.IsFinite(pos))
            {
                closest = default;
                posOverPoly = false;
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            this.navMesh->ClosestPointOnPoly(polyRef, pos, out closest, out posOverPoly);
            return DtStatus.Success;
        }

        /// <summary>
        /// Returns a point on the polygon boundary that is closest to the given position.
        /// </summary>
        /// <param name="polyRef">The reference id of the polygon.</param>
        /// <param name="pos">The position to check. [(x, y, z)].</param>
        /// <param name="closest">The closest point on the polygon boundary. [(x, y, z)].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Much faster than <see cref="ClosestPointOnPoly"/>.</para>
        /// <para>If the provided position lies within the polygon's xz-bounds (above or below),
        /// <paramref name="pos"/> and <paramref name="closest"/> are equal. The height of the closest point is taken
        /// from the polygon boundary and height detail is not used.</para>
        /// <para>The input position does not need to be within the bounds of the polygon or navigation mesh.</para>
        /// </remarks>
        public DtStatus ClosestPointOnPolyBoundary(DtPolyRef polyRef, in float3 pos, out float3 closest)
        {
            closest = default;

            var status = this.navMesh->GetTileAndPolyByRef(polyRef, out var tile, out var poly);
            if (Detour.StatusFailed(status))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (!Detour.IsFinite(pos))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Collect vertices
            var verts = stackalloc float3[Detour.DTVertsPerPolygon];
            var edged = stackalloc float[Detour.DTVertsPerPolygon];
            var edget = stackalloc float[Detour.DTVertsPerPolygon];
            var nv = 0;
            for (var i = 0; i < poly->vertCount; ++i)
            {
                verts[nv] = tile->verts[poly->verts[i]];
                nv++;
            }

            var inside = Detour.DistancePtPolyEdgesSqr(pos, verts, nv, edged, edget);
            if (inside)
            {
                // Point is inside the polygon, return the point
                closest = pos;
            }
            else
            {
                // Point is outside the polygon, clamp to nearest edge
                var dmin = edged[0];
                var imin = 0;
                for (var i = 1; i < nv; ++i)
                {
                    if (edged[i] < dmin)
                    {
                        dmin = edged[i];
                        imin = i;
                    }
                }

                var va = verts[imin];
                var vb = verts[(imin + 1) % nv];
                closest = math.lerp(va, vb, edget[imin]);
            }

            return DtStatus.Success;
        }

        /// <summary>
        /// Gets the height of the specified polygon at the provided position.
        /// </summary>
        /// <param name="polyRef">The reference id of the polygon.</param>
        /// <param name="pos">A position within the polygon's xz-bounds. [(x, y, z)].</param>
        /// <param name="height">The height at the polygon surface.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Uses height detail and provides the most accurate result. Returns
        /// <see cref="DtStatus.Failure"/> | <see cref="DtStatus.InvalidParam"/> if the provided position lies
        /// outside the polygon's xz-bounds.</para>
        /// </remarks>
        public DtStatus GetPolyHeight(DtPolyRef polyRef, in float3 pos, out float height)
        {
            height = 0;

            var status = this.navMesh->GetTileAndPolyByRef(polyRef, out var tile, out var poly);
            if (Detour.StatusFailed(status))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (!Detour.IsFinite2D(pos))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // We used to return success for offmesh connections, but the
            // getPolyHeight in DetourNavMesh does not do this, so special case it here
            if (poly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
            {
                var v0 = tile->verts[poly->verts[0]];
                var v1 = tile->verts[poly->verts[1]];
                Detour.DistancePtSegSqr2D(pos, v0, v1, out var t);
                height = v0.y + ((v1.y - v0.y) * t);
                return DtStatus.Success;
            }

            return this.navMesh->GetPolyHeight(tile, poly, pos, out height) ? DtStatus.Success : DtStatus.Failure | DtStatus.InvalidParam;
        }

        /// <summary>
        /// Finds the polygon nearest to the specified center point.
        /// </summary>
        /// <param name="center">The center of the search box. [(x, y, z)].</param>
        /// <param name="halfExtents">The search distance along each axis. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="nearestRef">The reference id of the nearest polygon, or zero if no polygon is found.</param>
        /// <param name="nearestPt">The nearest point on the polygon. [(x, y, z)].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>If the search box does not intersect any polygons, the method returns success with
        /// <paramref name="nearestRef"/> set to zero. In that case <paramref name="nearestPt"/> is left unchanged.</para>
        /// </remarks>
        public DtStatus FindNearestPoly(in float3 center, in float3 halfExtents, ref DtQueryFilter filter, ref DtPolyRef nearestRef, ref float3 nearestPt)
        {
            return this.FindNearestPoly(center, halfExtents, ref filter, ref nearestRef, ref nearestPt, out _);
        }

        /// <summary>
        /// Finds the polygon nearest to the specified center point.
        /// </summary>
        /// <param name="center">The center of the search box. [(x, y, z)].</param>
        /// <param name="halfExtents">The search distance along each axis. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="nearestRef">The reference id of the nearest polygon, or zero if no polygon is found.</param>
        /// <param name="nearestPt">The nearest point on the polygon. [(x, y, z)].</param>
        /// <param name="isOverPoly">Set to <see langword="true"/> if the x/z of the point lies inside the polygon when found.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>If the search box does not intersect any polygons, the method returns success with
        /// <paramref name="nearestRef"/> set to zero. In that case the remaining outputs are unchanged.</para>
        /// </remarks>
        public DtStatus FindNearestPoly(
            in float3 center, in float3 halfExtents, ref DtQueryFilter filter, ref DtPolyRef nearestRef, ref float3 nearestPt, out bool isOverPoly)
        {
            isOverPoly = false;

            var query = new DtFindNearestPolyQuery(this, center);
            var status = this.QueryPolygons(center, halfExtents, ref filter, ref query);
            if (Detour.StatusFailed(status))
            {
                return status;
            }

            nearestRef = query.NearestRef;
            if (nearestRef != 0)
            {
                nearestPt = query.NearestPoint;
                isOverPoly = query.IsOverPoly;
            }

            return status;
        }

        /// <summary>
        /// Finds polygons that overlap the specified axis-aligned search box.
        /// </summary>
        /// <param name="center">The center of the search box. [(x, y, z)].</param>
        /// <param name="halfExtents">The search distance along each axis. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="polys">Receives the reference ids of the polygons that overlap the search box.</param>
        /// <param name="polyCount">Receives the number of polygons written to <paramref name="polys"/>.</param>
        /// <param name="maxPolys">The maximum number of polygons that <paramref name="polys"/> can hold.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>If no polygons overlap the search box, the method returns success with
        /// <paramref name="polyCount"/> set to zero.</para>
        /// <para>If <paramref name="polys"/> is too small, it is filled to capacity. The subset of polygons chosen when
        /// truncating the results is unspecified.</para>
        /// </remarks>
        public DtStatus QueryPolygons(in float3 center, in float3 halfExtents, ref DtQueryFilter filter, DtPolyRef* polys, int* polyCount, int maxPolys)
        {
            if (polys == null || polyCount == null || maxPolys < 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var collector = new DtCollectPolysQuery(polys, maxPolys);
            var status = this.QueryPolygons(center, halfExtents, ref filter, ref collector);
            if (Detour.StatusFailed(status))
            {
                return status;
            }

            *polyCount = collector.NumCollected;
            return collector.Overflowed ? DtStatus.Success | DtStatus.BufferTooSmall : DtStatus.Success;
        }

        /// <summary>
        /// Finds polygons that overlap the search box and processes them with a custom query handler.
        /// </summary>
        /// <typeparam name="T">The query processor type.</typeparam>
        /// <param name="center">The center of the search box. [(x, y, z)].</param>
        /// <param name="halfExtents">The search distance along each axis. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="query">Receives batches of polygons touched by the search box.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>The query object's <see cref="IDtPolyQuery.Process"/> method is invoked one or more times with batches of
        /// unique polygons that overlap the search extents.</para>
        /// </remarks>
        public DtStatus QueryPolygons<T>(in float3 center, in float3 halfExtents, ref DtQueryFilter filter, ref T query)
            where T : unmanaged, IDtPolyQuery
        {
            if (!Detour.IsFinite(center) || !Detour.IsFinite(halfExtents))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var bmin = center - halfExtents;
            var bmax = center + halfExtents;

            // Find tiles the query touches
            this.navMesh->CalculateTileLocation(bmin, out var minx, out var miny);
            this.navMesh->CalculateTileLocation(bmax, out var maxx, out var maxy);

            var neis = stackalloc DtMeshTile*[MaxNeighbourTiles];

            for (var y = miny; y <= maxy; ++y)
            {
                for (var x = minx; x <= maxx; ++x)
                {
                    var nneis = this.navMesh->GetTilesAt(x, y, neis, MaxNeighbourTiles);
                    for (var j = 0; j < nneis; ++j)
                    {
                        this.QueryPolygonsInTile(neis[j], bmin, bmax, ref filter, ref query);
                    }
                }
            }

            return DtStatus.Success;
        }

        /// <summary>
        /// Queries polygons within a single navigation mesh tile that overlap the provided bounds.
        /// </summary>
        /// <typeparam name="T">The query processor type.</typeparam>
        /// <param name="tile">The tile to inspect.</param>
        /// <param name="qmin">The minimum corner of the query bounds. [(x, y, z)].</param>
        /// <param name="qmax">The maximum corner of the query bounds. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="query">Receives batches of polygons touched by the search box.</param>
        private void QueryPolygonsInTile<T>(DtMeshTile* tile, in float3 qmin, in float3 qmax, ref DtQueryFilter filter, ref T query)
            where T : unmanaged, IDtPolyQuery
        {
            const int batchSize = 32;
            var polyRefs = stackalloc DtPolyRef[batchSize];
            var polys = stackalloc DtPoly*[batchSize];
            var n = 0;

            if (tile->bvTree != null)
            {
                var node = &tile->bvTree[0];
                var end = &tile->bvTree[tile->header->bvNodeCount];
                var tbmin = tile->header->bmin;
                var tbmax = tile->header->bmax;
                var qfac = tile->header->bvQuantFactor;

                // Calculate quantized box
                var bmin = stackalloc ushort[3];
                var bmax = stackalloc ushort[3];

                // Clamp query box to world box
                var minx = math.clamp(qmin.x, tbmin.x, tbmax.x) - tbmin.x;
                var miny = math.clamp(qmin.y, tbmin.y, tbmax.y) - tbmin.y;
                var minz = math.clamp(qmin.z, tbmin.z, tbmax.z) - tbmin.z;
                var maxx = math.clamp(qmax.x, tbmin.x, tbmax.x) - tbmin.x;
                var maxy = math.clamp(qmax.y, tbmin.y, tbmax.y) - tbmin.y;
                var maxz = math.clamp(qmax.z, tbmin.z, tbmax.z) - tbmin.z;

                // Quantize
                bmin[0] = (ushort)((ushort)(qfac * minx) & 0xfffe);
                bmin[1] = (ushort)((ushort)(qfac * miny) & 0xfffe);
                bmin[2] = (ushort)((ushort)(qfac * minz) & 0xfffe);
                bmax[0] = (ushort)((ushort)((qfac * maxx) + 1) | 1);
                bmax[1] = (ushort)((ushort)((qfac * maxy) + 1) | 1);
                bmax[2] = (ushort)((ushort)((qfac * maxz) + 1) | 1);

                // Traverse tree
                var polyRefBase = this.navMesh->GetPolyRefBase(tile);
                while (node < end)
                {
                    var overlap = Detour.OverlapQuantBounds(new ushort3(bmin[0], bmin[1], bmin[2]), new ushort3(bmax[0], bmax[1], bmax[2]), node->bmin,
                        node->bmax);

                    var isLeafNode = node->i >= 0;

                    if (isLeafNode && overlap)
                    {
                        var pRef = polyRefBase | (DtPolyRef)node->i;
                        if (filter.PassFilter(pRef, tile, &tile->polys[node->i]))
                        {
                            polyRefs[n] = pRef;
                            polys[n] = &tile->polys[node->i];

                            if (n == batchSize - 1)
                            {
                                query.Process(tile, polyRefs, batchSize);
                                n = 0;
                            }
                            else
                            {
                                n++;
                            }
                        }
                    }

                    if (overlap || isLeafNode)
                    {
                        node++;
                    }
                    else
                    {
                        var escapeIndex = -node->i;
                        node += escapeIndex;
                    }
                }
            }
            else
            {
                var polyRefBase = this.navMesh->GetPolyRefBase(tile);
                for (var i = 0; i < tile->header->polyCount; ++i)
                {
                    var p = &tile->polys[i];

                    // Do not return off-mesh connection polygons
                    if (p->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
                    {
                        continue;
                    }

                    var pRef = polyRefBase | (DtPolyRef)i;
                    if (!filter.PassFilter(pRef, tile, p))
                    {
                        continue;
                    }

                    // Calc polygon bounds
                    var v = tile->verts[p->verts[0]];
                    var bmin = v;
                    var bmax = v;
                    for (var j = 1; j < p->vertCount; ++j)
                    {
                        v = tile->verts[p->verts[j]];
                        bmin = math.min(bmin, v);
                        bmax = math.max(bmax, v);
                    }

                    if (Detour.OverlapBounds(qmin, qmax, bmin, bmax))
                    {
                        polyRefs[n] = pRef;
                        polys[n] = p;

                        if (n == batchSize - 1)
                        {
                            query.Process(tile, polyRefs, batchSize);
                            n = 0;
                        }
                        else
                        {
                            n++;
                        }
                    }
                }
            }

            // Process the last polygons that didn't make a full batch
            if (n > 0)
            {
                query.Process(tile, polyRefs, n);
            }
        }

        /// <summary>
        /// Builds a polygon path by walking the parent chain that leads to the specified node.
        /// </summary>
        /// <param name="endNode">The node that marks the end of the reconstructed path.</param>
        /// <param name="path">Receives polygon references from start to <paramref name="endNode"/>.</param>
        /// <param name="pathCount">Receives the number of polygons stored in <paramref name="path"/>.</param>
        /// <param name="maxPath">The capacity of the <paramref name="path"/> array.</param>
        /// <returns>The status flags for the reconstruction.</returns>
        private DtStatus GetPathToNode(DtNode* endNode, DtPolyRef* path, out int pathCount, int maxPath)
        {
            pathCount = 0;

            // Find the length of the entire path
            var curNode = endNode;
            var length = 0;
            do
            {
                length++;
                curNode = this.nodePool->GetNodeAtIdx(curNode->ParentIndex);
            }
            while (curNode != null);

            // If the path cannot be fully stored then advance to the last node we will be able to store
            curNode = endNode;
            int writeCount;
            for (writeCount = length; writeCount > maxPath; writeCount--)
            {
                curNode = this.nodePool->GetNodeAtIdx(curNode->ParentIndex);
            }

            // Write path
            for (var i = writeCount - 1; i >= 0; i--)
            {
                path[i] = curNode->id;
                curNode = this.nodePool->GetNodeAtIdx(curNode->ParentIndex);
            }

            pathCount = math.min(length, maxPath);

            return length > maxPath ? DtStatus.Success | DtStatus.BufferTooSmall : DtStatus.Success;
        }

        /// <summary>
        /// Returns the midpoint of the shared edge between two polygons.
        /// </summary>
        /// <param name="from">The reference id of the first polygon.</param>
        /// <param name="fromPoly">The first polygon.</param>
        /// <param name="fromTile">The tile that contains the first polygon.</param>
        /// <param name="to">The reference id of the second polygon.</param>
        /// <param name="toPoly">The second polygon.</param>
        /// <param name="toTile">The tile that contains the second polygon.</param>
        /// <param name="mid">Receives the midpoint. [(x, y, z)].</param>
        /// <returns>The status flags for the query.</returns>
        private DtStatus GetEdgeMidPoint(
            DtPolyRef from, DtPoly* fromPoly, DtMeshTile* fromTile, DtPolyRef to, DtPoly* toPoly, DtMeshTile* toTile, out float3 mid)
        {
            mid = float3.zero;
            var status = this.GetPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, out var left, out var right);
            if (Detour.StatusFailed(status))
            {
                return status;
            }

            mid = (left + right) * 0.5f;
            return DtStatus.Success;
        }

        /// <summary>
        /// Returns the left and right portal vertices between two polygons.
        /// </summary>
        /// <param name="from">The reference id of the first polygon.</param>
        /// <param name="fromPoly">The first polygon.</param>
        /// <param name="fromTile">The tile that contains the first polygon.</param>
        /// <param name="to">The reference id of the second polygon.</param>
        /// <param name="toPoly">The second polygon.</param>
        /// <param name="toTile">The tile that contains the second polygon.</param>
        /// <param name="left">Receives the left portal vertex. [(x, y, z)].</param>
        /// <param name="right">Receives the right portal vertex. [(x, y, z)].</param>
        /// <returns>The status flags for the query.</returns>
        private DtStatus GetPortalPoints(
            DtPolyRef from, DtPoly* fromPoly, DtMeshTile* fromTile, DtPolyRef to, DtPoly* toPoly, DtMeshTile* toTile, out float3 left, out float3 right)
        {
            left = right = float3.zero;

            // Find the link that points to the 'to' polygon.
            DtLink* link = null;
            for (var i = fromPoly->firstLink; i != Detour.DTNullLink; i = fromTile->links[i].next)
            {
                if (fromTile->links[i].polyRef == to)
                {
                    link = &fromTile->links[i];
                    break;
                }
            }

            if (link == null)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Handle off-mesh connections.
            if (fromPoly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
            {
                // Find link that points to first vertex.
                for (var i = fromPoly->firstLink; i != Detour.DTNullLink; i = fromTile->links[i].next)
                {
                    if (fromTile->links[i].polyRef == to)
                    {
                        var v = fromTile->links[i].edge;
                        left = right = fromTile->verts[fromPoly->verts[v]];
                        return DtStatus.Success;
                    }
                }

                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (toPoly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
            {
                for (var i = toPoly->firstLink; i != Detour.DTNullLink; i = toTile->links[i].next)
                {
                    if (toTile->links[i].polyRef == from)
                    {
                        var v = toTile->links[i].edge;
                        left = right = toTile->verts[toPoly->verts[v]];
                        return DtStatus.Success;
                    }
                }

                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Find portal vertices.
            var v0 = fromPoly->verts[link->edge];
            var v1 = fromPoly->verts[(link->edge + 1) % fromPoly->vertCount];
            left = fromTile->verts[v0];
            right = fromTile->verts[v1];

            // If the link is at tile boundary, clamp the vertices to the link width.
            if (link->side != 0xff)
            {
                // Unpack portal limits.
                if (link->bmin != 0 || link->bmax != 255)
                {
                    const float s = 1.0f / 255.0f;
                    var tmin = link->bmin * s;
                    var tmax = link->bmax * s;
                    left = math.lerp(fromTile->verts[v0], fromTile->verts[v1], tmin);
                    right = math.lerp(fromTile->verts[v0], fromTile->verts[v1], tmax);
                }
            }

            return DtStatus.Success;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the polygon reference is valid and passes the filter restrictions.
        /// </summary>
        /// <param name="polyRef">The polygon reference to test.</param>
        /// <param name="filter">The filter whose rules must be satisfied.</param>
        /// <returns><see langword="true"/> if the polygon reference is valid and passes the filter.</returns>
        public bool IsValidPolyRef(DtPolyRef polyRef, ref DtQueryFilter filter)
        {
            var status = this.navMesh->GetTileAndPolyByRef(polyRef, out var tile, out var poly);

            // If cannot get polygon, assume it does not exists and boundary is invalid.
            if (Detour.StatusFailed(status))
            {
                return false;
            }

            // If cannot pass filter, assume flags has changed and boundary is invalid.
            if (!filter.PassFilter(polyRef, tile, poly))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the polygon reference is in the closed list.
        /// </summary>
        /// <param name="polyRef">The polygon reference to check.</param>
        /// <returns><see langword="true"/> if the polygon reference is in the closed list.</returns>
        public bool IsInClosedList(DtPolyRef polyRef)
        {
            if (this.nodePool == null)
            {
                return false;
            }

            var nodes = stackalloc DtNode*[DtNodePool.DTMaxStatesPerNode];
            var n = this.nodePool->FindNodes(polyRef, nodes, DtNodePool.DTMaxStatesPerNode);

            for (var i = 0; i < n; i++)
            {
                if ((nodes[i]->Flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the navigation mesh used by this query object.
        /// </summary>
        /// <returns>The attached navigation mesh.</returns>
        public DtNavMesh* GetAttachedNavMesh() => this.navMesh;

        /// <summary>
        /// Gets the node pool associated with this query.
        /// </summary>
        /// <returns>The node pool.</returns>
        public DtNodePool* GetNodePool() => this.nodePool;

        /// <summary>
        /// Finds the straight path from the start to the end position within the polygon corridor.
        /// </summary>
        /// <param name="startPos">Path start position. [(x, y, z)].</param>
        /// <param name="endPos">Path end position. [(x, y, z)].</param>
        /// <param name="path">An array of polygon references that represent the path corridor.</param>
        /// <param name="pathSize">The number of polygons in <paramref name="path"/>.</param>
        /// <param name="straightPath">Receives points describing the straight path. [(x, y, z) * <paramref name="straightPathCount"/>].</param>
        /// <param name="straightPathFlags">Receives flags describing each point (see <see cref="DtStraightPathFlags"/>). [opt].</param>
        /// <param name="straightPathRefs">Receives the reference id of the polygon entered at each point. [opt].</param>
        /// <param name="straightPathCount">Receives the number of points written to <paramref name="straightPath"/>.</param>
        /// <param name="maxStraightPath">The maximum number of points that can be stored in the straight path arrays. [Limit: &gt; 0].</param>
        /// <param name="options">Query options. (See <see cref="DtStraightPathOptions"/>.)</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Performs so-called "string pulling". The start position is clamped to the first polygon in the path and
        /// the end position to the last, so both should normally lie inside (or very near) those polygons.</para>
        /// <para>The polygon reference returned for the end point is always zero, which allows matching off-mesh link
        /// points to their representative polygons.</para>
        /// <para>If the provided result buffers are too small they are filled from the start toward the end position.</para>
        /// </remarks>
        public DtStatus FindStraightPath(
            in float3 startPos, in float3 endPos, DtPolyRef* path, int pathSize, float3* straightPath, DtStraightPathFlags* straightPathFlags,
            DtPolyRef* straightPathRefs, out int straightPathCount, int maxStraightPath, DtStraightPathOptions options = 0)
        {
            straightPathCount = 0;

            if (!Detour.IsFinite(startPos) || !Detour.IsFinite(endPos) || path == null || pathSize <= 0 || path[0] == 0 || maxStraightPath <= 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // TODO: Should this be caller's responsibility?
            if (Detour.StatusFailed(this.ClosestPointOnPolyBoundary(path[0], startPos, out var closestStartPos)))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (Detour.StatusFailed(this.ClosestPointOnPolyBoundary(path[pathSize - 1], endPos, out var closestEndPos)))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Add start point
            var status = this.AppendVertex(closestStartPos, DtStraightPathFlags.StraightpathStart, path[0], straightPath, straightPathFlags, straightPathRefs,
                ref straightPathCount, maxStraightPath);

            if (status != DtStatus.InProgress)
            {
                return status;
            }

            if (pathSize > 1)
            {
                var portalApex = closestStartPos;
                var portalLeft = portalApex;
                var portalRight = portalApex;
                var apexIndex = 0;
                var leftIndex = 0;
                var rightIndex = 0;

                DtPolyTypes leftPolyType = 0;
                DtPolyTypes rightPolyType = 0;

                var leftPolyRef = path[0];
                var rightPolyRef = path[0];

                for (var i = 0; i < pathSize; ++i)
                {
                    DtPolyTypes toType;

                    float3 right;
                    float3 left;
                    if (i + 1 < pathSize)
                    {
                        // Next portal
                        if (Detour.StatusFailed(this.GetPortalPoints(path[i], path[i + 1], out left, out right, out _, out toType)))
                        {
                            // Failed to get portal points, clamp the end point to path[i], and return the path so far
                            if (Detour.StatusFailed(this.ClosestPointOnPolyBoundary(path[i], endPos, out closestEndPos)))
                            {
                                return DtStatus.Failure | DtStatus.InvalidParam;
                            }

                            // Append portals along the current straight path segment
                            if ((options & (DtStraightPathOptions.StraightPathAreaCrossings | DtStraightPathOptions.StraightPathAllCrossings)) != 0)
                            {
                                this.AppendPortals(apexIndex, i, closestEndPos, path, straightPath, straightPathFlags, straightPathRefs, ref straightPathCount,
                                    maxStraightPath, options);
                            }

                            this.AppendVertex(closestEndPos, 0, path[i], straightPath, straightPathFlags, straightPathRefs, ref straightPathCount,
                                maxStraightPath);

                            return DtStatus.Success | DtStatus.PartialResult | (straightPathCount >= maxStraightPath ? DtStatus.BufferTooSmall : 0);
                        }

                        // If starting really close the portal, advance
                        if (i == 0)
                        {
                            if (Detour.DistancePtSegSqr2D(portalApex, left, right, out _) < 0.001f * 0.001f)
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // End of the path
                        left = closestEndPos;
                        right = closestEndPos;
                        toType = (byte)DtPolyTypes.PolytypeGround;
                    }

                    var areaEps = GetStraightPathAreaEpsilon(left, right);

                    var areaRight = Detour.TriArea2D(portalApex, portalRight, right);
                    var areaRightTest = Detour.TriArea2D(portalApex, portalLeft, right);
                    var areaLeft = Detour.TriArea2D(portalApex, portalLeft, left);
                    var areaLeftTest = Detour.TriArea2D(portalApex, portalRight, left);

                    // Right vertex
                    if (areaRight <= areaEps)
                    {
                        if (Detour.Equal(portalApex, portalRight) || areaRightTest > areaEps)
                        {
                            portalRight = right;
                            rightPolyRef = i + 1 < pathSize ? path[i + 1] : 0;
                            rightPolyType = toType;
                            rightIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment
                            if ((options & (DtStraightPathOptions.StraightPathAreaCrossings | DtStraightPathOptions.StraightPathAllCrossings)) != 0)
                            {
                                status = this.AppendPortals(apexIndex, leftIndex, portalLeft, path, straightPath, straightPathFlags, straightPathRefs,
                                    ref straightPathCount, maxStraightPath, options);

                                if (status != DtStatus.InProgress)
                                {
                                    return status;
                                }
                            }

                            portalApex = portalLeft;
                            apexIndex = leftIndex;

                            var flags = (DtStraightPathFlags)0;
                            if (leftPolyRef == 0)
                            {
                                flags = DtStraightPathFlags.StraightpathEnd;
                            }
                            else if (leftPolyType == DtPolyTypes.PolytypeOffMeshConnection)
                            {
                                flags = DtStraightPathFlags.StraightpathOffmeshConnection;
                            }

                            var polyRef = leftPolyRef;

                            // Append or update vertex
                            status = this.AppendVertex(portalApex, flags, polyRef, straightPath, straightPathFlags, straightPathRefs, ref straightPathCount,
                                maxStraightPath);

                            if (status != DtStatus.InProgress)
                            {
                                return status;
                            }

                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;
                            continue;
                        }
                    }

                    // Left vertex
                    if (areaLeft >= -areaEps)
                    {
                        if (Detour.Equal(portalApex, portalLeft) || areaLeftTest < -areaEps)
                        {
                            portalLeft = left;
                            leftPolyRef = i + 1 < pathSize ? path[i + 1] : 0;
                            leftPolyType = toType;
                            leftIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment
                            if ((options & (DtStraightPathOptions.StraightPathAreaCrossings | DtStraightPathOptions.StraightPathAllCrossings)) != 0)
                            {
                                status = this.AppendPortals(apexIndex, rightIndex, portalRight, path, straightPath, straightPathFlags, straightPathRefs,
                                    ref straightPathCount, maxStraightPath, options);

                                if (status != DtStatus.InProgress)
                                {
                                    return status;
                                }
                            }

                            portalApex = portalRight;
                            apexIndex = rightIndex;

                            var flags = (DtStraightPathFlags)0;
                            if (rightPolyRef == 0)
                            {
                                flags = DtStraightPathFlags.StraightpathEnd;
                            }
                            else if (rightPolyType == DtPolyTypes.PolytypeOffMeshConnection)
                            {
                                flags = DtStraightPathFlags.StraightpathOffmeshConnection;
                            }

                            var polyRef = rightPolyRef;

                            // Append or update vertex
                            status = this.AppendVertex(portalApex, flags, polyRef, straightPath, straightPathFlags, straightPathRefs, ref straightPathCount,
                                maxStraightPath);

                            if (status != DtStatus.InProgress)
                            {
                                return status;
                            }

                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;
                            continue;
                        }
                    }
                }

                // Append portals along the current straight path segment
                if ((options & (DtStraightPathOptions.StraightPathAreaCrossings | DtStraightPathOptions.StraightPathAllCrossings)) != 0)
                {
                    status = this.AppendPortals(apexIndex, pathSize - 1, closestEndPos, path, straightPath, straightPathFlags, straightPathRefs,
                        ref straightPathCount, maxStraightPath, options);

                    if (status != DtStatus.InProgress)
                    {
                        return status;
                    }
                }
            }

            this.AppendVertex(closestEndPos, DtStraightPathFlags.StraightpathEnd, 0, straightPath, straightPathFlags, straightPathRefs, ref straightPathCount,
                maxStraightPath);

            return DtStatus.Success | (straightPathCount >= maxStraightPath ? DtStatus.BufferTooSmall : 0);
        }

        /// <summary>
        /// Finds the straight path from the start to the end position within the polygon corridor.
        /// </summary>
        /// <param name="startPos">Path start position. [(x, y, z)].</param>
        /// <param name="endPos">Path end position. [(x, y, z)].</param>
        /// <param name="path">An array of polygon references that represent the path corridor.</param>
        /// <param name="pathSize">The number of polygons in <paramref name="path"/>.</param>
        /// <param name="straightPath">Receives points describing the straight path. [(x, y, z) * <paramref name="straightPathCount"/>].</param>
        /// <param name="straightPathFlags">Receives flags describing each point (see <see cref="DtStraightPathFlags"/>). [opt].</param>
        /// <param name="straightPathRefs">Receives the reference id of the polygon entered at each point. [opt].</param>
        /// <param name="straightPathCount">Receives the number of points written to <paramref name="straightPath"/>.</param>
        /// <param name="maxStraightPath">The maximum number of points that can be stored in the straight path arrays. [Limit: &gt; 0].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="options">Query options. (See <see cref="DtStraightPathOptions"/>.)</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Performs so-called "string pulling". The start position is clamped to the first polygon in the path and
        /// the end position to the last, so both should normally lie inside (or very near) those polygons.</para>
        /// <para>The polygon reference returned for the end point is always zero, which allows matching off-mesh link
        /// points to their representative polygons.</para>
        /// <para>If the provided result buffers are too small they are filled from the start toward the end position.</para>
        /// </remarks>
        public DtStatus FindStraightPath(
            in float3 startPos, in float3 endPos, DtPolyRef* path, int pathSize, float3* straightPath, DtStraightPathFlags* straightPathFlags,
            DtPolyRef* straightPathRefs, out int straightPathCount, int maxStraightPath, ref DtQueryFilter filter, DtStraightPathOptions options = 0)
        {
            straightPathCount = 0;

            if (!Detour.IsFinite(startPos) || !Detour.IsFinite(endPos) || path == null || pathSize <= 0 || path[0] == 0 || maxStraightPath <= 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // TODO: Should this be caller's responsibility?
            if (Detour.StatusFailed(this.ClosestPointOnPolyBoundary(path[0], startPos, out var closestStartPos)))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (Detour.StatusFailed(this.ClosestPointOnPolyBoundary(path[pathSize - 1], endPos, out var closestEndPos)))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // If there is direct line of sight, return a single segment.
            if (options == 0)
            {
                var rayStatus = this.Raycast(path[0], closestStartPos, closestEndPos, ref filter, out var t, out _, null, out _, 0);
                if (Detour.StatusSucceed(rayStatus) && t == float.MaxValue)
                {
                    var status = this.AppendVertex(closestStartPos, DtStraightPathFlags.StraightpathStart, path[0], straightPath, straightPathFlags,
                        straightPathRefs, ref straightPathCount, maxStraightPath);

                    if (status != DtStatus.InProgress)
                    {
                        return status;
                    }

                    return this.AppendVertex(closestEndPos, DtStraightPathFlags.StraightpathEnd, 0, straightPath, straightPathFlags, straightPathRefs,
                        ref straightPathCount, maxStraightPath);
                }
            }

            var result = this.FindStraightPath(startPos, endPos, path, pathSize, straightPath, straightPathFlags, straightPathRefs, out straightPathCount,
                maxStraightPath, options);

            if (options == 0 && straightPathCount >= 2 && straightPathRefs != null && (result & DtStatus.BufferTooSmall) == 0)
            {
                var lastIndex = straightPathCount - 1;
                var hasEnd = straightPathRefs[lastIndex] == 0;

                if (!hasEnd && straightPathFlags != null)
                {
                    hasEnd = (straightPathFlags[lastIndex] & DtStraightPathFlags.StraightpathEnd) != 0;
                }

                if (hasEnd)
                {
                    for (var i = lastIndex - 1; i >= 0; --i)
                    {
                        if (straightPathFlags != null && (straightPathFlags[i] & DtStraightPathFlags.StraightpathOffmeshConnection) != 0)
                        {
                            break;
                        }

                        var startRef = straightPathRefs[i];
                        if (startRef == 0)
                        {
                            continue;
                        }

                        var rayStatus = this.Raycast(startRef, straightPath[i], closestEndPos, ref filter, out var t, out _, null, out _, 0);
                        if (Detour.StatusSucceed(rayStatus) && t == float.MaxValue)
                        {
                            straightPath[i + 1] = closestEndPos;
                            if (straightPathFlags != null)
                            {
                                straightPathFlags[i + 1] = DtStraightPathFlags.StraightpathEnd;
                            }

                            straightPathRefs[i + 1] = 0;
                            straightPathCount = i + 2;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Finds the straight path from the start to the end position within the polygon corridor.
        /// </summary>
        /// <param name="startPos">Path start position. [(x, y, z)].</param>
        /// <param name="endPos">Path end position. [(x, y, z)].</param>
        /// <param name="path">An array of polygon references that represent the path corridor.</param>
        /// <param name="pathSize">The number of polygons in <paramref name="path"/>.</param>
        /// <param name="straightPath">Receives points describing the straight path. [(x, y, z) * count].</param>
        /// <param name="straightPathFlags">Receives flags describing each point (see <see cref="DtStraightPathFlags"/>). [opt].</param>
        /// <param name="straightPathRefs">Receives the reference id of the polygon entered at each point. [opt].</param>
        /// <param name="options">Query options. (See <see cref="DtStraightPathOptions"/>.)</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Performs so-called "string pulling". The start position is clamped to the first polygon in the path and
        /// the end position to the last, so both should normally lie inside (or very near) those polygons.</para>
        /// <para>The polygon reference returned for the end point is always zero, which allows matching off-mesh link
        /// points to their representative polygons.</para>
        /// <para>If the provided result buffers are too small they are filled from the start toward the end position.</para>
        /// </remarks>
        public DtStatus FindStraightPath(
            in float3 startPos, in float3 endPos, DtPolyRef* path, int pathSize, NativeList<float3> straightPath,
            NativeList<DtStraightPathFlags> straightPathFlags, NativeList<DtPolyRef> straightPathRefs, DtStraightPathOptions options = 0)
        {
            if (!Detour.IsFinite(startPos) || !Detour.IsFinite(endPos) || path == null || pathSize <= 0 || path[0] == 0 || !straightPath.IsCreated)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            straightPath.Clear();
            if (straightPathFlags.IsCreated)
            {
                straightPathFlags.Clear();
            }

            if (straightPathRefs.IsCreated)
            {
                straightPathRefs.Clear();
            }

            // TODO: Should this be caller's responsibility?
            if (Detour.StatusFailed(this.ClosestPointOnPolyBoundary(path[0], startPos, out var closestStartPos)))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (Detour.StatusFailed(this.ClosestPointOnPolyBoundary(path[pathSize - 1], endPos, out var closestEndPos)))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // Add start point
            var status = this.AppendVertex(closestStartPos, DtStraightPathFlags.StraightpathStart, path[0], straightPath, straightPathFlags, straightPathRefs);

            if (status != DtStatus.InProgress)
            {
                return status;
            }

            if (pathSize > 1)
            {
                var portalApex = closestStartPos;
                var portalLeft = portalApex;
                var portalRight = portalApex;
                var apexIndex = 0;
                var leftIndex = 0;
                var rightIndex = 0;

                DtPolyTypes leftPolyType = 0;
                DtPolyTypes rightPolyType = 0;

                var leftPolyRef = path[0];
                var rightPolyRef = path[0];

                for (var i = 0; i < pathSize; ++i)
                {
                    DtPolyTypes toType;

                    float3 right;
                    float3 left;
                    if (i + 1 < pathSize)
                    {
                        // Next portal
                        if (Detour.StatusFailed(this.GetPortalPoints(path[i], path[i + 1], out left, out right, out _, out toType)))
                        {
                            // Failed to get portal points, clamp the end point to path[i], and return the path so far
                            if (Detour.StatusFailed(this.ClosestPointOnPolyBoundary(path[i], endPos, out closestEndPos)))
                            {
                                return DtStatus.Failure | DtStatus.InvalidParam;
                            }

                            // Append portals along the current straight path segment
                            if ((options & (DtStraightPathOptions.StraightPathAreaCrossings | DtStraightPathOptions.StraightPathAllCrossings)) != 0)
                            {
                                var portalStatus = this.AppendPortals(apexIndex, i, closestEndPos, path, straightPath, straightPathFlags, straightPathRefs,
                                    options);

                                if (portalStatus != DtStatus.InProgress)
                                {
                                    return portalStatus;
                                }
                            }

                            var vertexStatus = this.AppendVertex(closestEndPos, 0, path[i], straightPath, straightPathFlags, straightPathRefs);

                            if (Detour.StatusFailed(vertexStatus))
                            {
                                return vertexStatus;
                            }

                            return DtStatus.Success | DtStatus.PartialResult;
                        }

                        // If starting really close the portal, advance
                        if (i == 0)
                        {
                            if (Detour.DistancePtSegSqr2D(portalApex, left, right, out _) < 0.001f * 0.001f)
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // End of the path
                        left = closestEndPos;
                        right = closestEndPos;
                        toType = (byte)DtPolyTypes.PolytypeGround;
                    }

                    var areaEps = GetStraightPathAreaEpsilon(left, right);

                    var areaRight = Detour.TriArea2D(portalApex, portalRight, right);
                    var areaRightTest = Detour.TriArea2D(portalApex, portalLeft, right);
                    var areaLeft = Detour.TriArea2D(portalApex, portalLeft, left);
                    var areaLeftTest = Detour.TriArea2D(portalApex, portalRight, left);

                    // Right vertex
                    if (areaRight <= areaEps)
                    {
                        if (Detour.Equal(portalApex, portalRight) || areaRightTest > areaEps)
                        {
                            portalRight = right;
                            rightPolyRef = i + 1 < pathSize ? path[i + 1] : 0;
                            rightPolyType = toType;
                            rightIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment
                            if ((options & (DtStraightPathOptions.StraightPathAreaCrossings | DtStraightPathOptions.StraightPathAllCrossings)) != 0)
                            {
                                status = this.AppendPortals(apexIndex, leftIndex, portalLeft, path, straightPath, straightPathFlags, straightPathRefs, options);

                                if (status != DtStatus.InProgress)
                                {
                                    return status;
                                }
                            }

                            portalApex = portalLeft;
                            apexIndex = leftIndex;

                            var flags = (DtStraightPathFlags)0;
                            if (leftPolyRef == 0)
                            {
                                flags = DtStraightPathFlags.StraightpathEnd;
                            }
                            else if (leftPolyType == DtPolyTypes.PolytypeOffMeshConnection)
                            {
                                flags = DtStraightPathFlags.StraightpathOffmeshConnection;
                            }

                            var polyRef = leftPolyRef;

                            // Append or update vertex
                            status = this.AppendVertex(portalApex, flags, polyRef, straightPath, straightPathFlags, straightPathRefs);

                            if (status != DtStatus.InProgress)
                            {
                                return status;
                            }

                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;
                            continue;
                        }
                    }

                    // Left vertex
                    if (areaLeft >= -areaEps)
                    {
                        if (Detour.Equal(portalApex, portalLeft) || areaLeftTest < -areaEps)
                        {
                            portalLeft = left;
                            leftPolyRef = i + 1 < pathSize ? path[i + 1] : 0;
                            leftPolyType = toType;
                            leftIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment
                            if ((options & (DtStraightPathOptions.StraightPathAreaCrossings | DtStraightPathOptions.StraightPathAllCrossings)) != 0)
                            {
                                status = this.AppendPortals(apexIndex, rightIndex, portalRight, path, straightPath, straightPathFlags, straightPathRefs,
                                    options);

                                if (status != DtStatus.InProgress)
                                {
                                    return status;
                                }
                            }

                            portalApex = portalRight;
                            apexIndex = rightIndex;

                            var flags = (DtStraightPathFlags)0;
                            if (rightPolyRef == 0)
                            {
                                flags = DtStraightPathFlags.StraightpathEnd;
                            }
                            else if (rightPolyType == DtPolyTypes.PolytypeOffMeshConnection)
                            {
                                flags = DtStraightPathFlags.StraightpathOffmeshConnection;
                            }

                            var polyRef = rightPolyRef;

                            // Append or update vertex
                            status = this.AppendVertex(portalApex, flags, polyRef, straightPath, straightPathFlags, straightPathRefs);

                            if (status != DtStatus.InProgress)
                            {
                                return status;
                            }

                            portalLeft = portalApex;
                            portalRight = portalApex;
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;
                            continue;
                        }
                    }
                }

                // Append portals along the current straight path segment
                if ((options & (DtStraightPathOptions.StraightPathAreaCrossings | DtStraightPathOptions.StraightPathAllCrossings)) != 0)
                {
                    status = this.AppendPortals(apexIndex, pathSize - 1, closestEndPos, path, straightPath, straightPathFlags, straightPathRefs, options);

                    if (status != DtStatus.InProgress)
                    {
                        return status;
                    }
                }
            }

            this.AppendVertex(closestEndPos, DtStraightPathFlags.StraightpathEnd, 0, straightPath, straightPathFlags, straightPathRefs);

            return DtStatus.Success;
        }

        /// <summary>
        /// Returns the left and right portal vertices between two polygons.
        /// </summary>
        /// <param name="from">The reference id of the first polygon.</param>
        /// <param name="to">The reference id of the second polygon.</param>
        /// <param name="left">Receives the left portal vertex. [(x, y, z)].</param>
        /// <param name="right">Receives the right portal vertex. [(x, y, z)].</param>
        /// <param name="fromType">Receives the type of the first polygon.</param>
        /// <param name="toType">Receives the type of the second polygon.</param>
        /// <returns>The status flags for the query.</returns>
        public DtStatus GetPortalPoints(DtPolyRef from, DtPolyRef to, out float3 left, out float3 right, out DtPolyTypes fromType, out DtPolyTypes toType)
        {
            left = right = default;
            fromType = toType = DtPolyTypes.PolytypeGround;

            var status = this.navMesh->GetTileAndPolyByRef(from, out var fromTile, out var fromPoly);
            if (Detour.StatusFailed(status))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            fromType = fromPoly->GetPolyType();

            status = this.navMesh->GetTileAndPolyByRef(to, out var toTile, out var toPoly);
            if (Detour.StatusFailed(status))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            toType = toPoly->GetPolyType();

            return this.GetPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, out left, out right);
        }

        /// <summary>
        /// Appends a point to the straight path result.
        /// </summary>
        /// <param name="pos">The point to append. [(x, y, z)].</param>
        /// <param name="flags">The straight path flags for the point.</param>
        /// <param name="polyRef">The polygon reference entered at the point.</param>
        /// <param name="straightPath">The straight path positions to update.</param>
        /// <param name="straightPathFlags">The straight path flags array to update. [opt].</param>
        /// <param name="straightPathRefs">The polygon reference array to update. [opt].</param>
        /// <param name="straightPathCount">On entry, the current count; on return, the updated count.</param>
        /// <param name="maxStraightPath">The capacity of the straight path arrays.</param>
        /// <returns>The status flags for the append operation.</returns>
        private DtStatus AppendVertex(
            in float3 pos, DtStraightPathFlags flags, DtPolyRef polyRef, float3* straightPath, DtStraightPathFlags* straightPathFlags,
            DtPolyRef* straightPathRefs, ref int straightPathCount, int maxStraightPath)
        {
            if (straightPathCount > 0 && Detour.Equal(straightPath[straightPathCount - 1], pos))
            {
                // The vertices are equal, update flags and poly
                if (straightPathFlags != null)
                {
                    straightPathFlags[straightPathCount - 1] = flags;
                }

                if (straightPathRefs != null)
                {
                    straightPathRefs[straightPathCount - 1] = polyRef;
                }
            }
            else
            {
                // Append new vertex
                straightPath[straightPathCount] = pos;
                if (straightPathFlags != null)
                {
                    straightPathFlags[straightPathCount] = flags;
                }

                if (straightPathRefs != null)
                {
                    straightPathRefs[straightPathCount] = polyRef;
                }

                straightPathCount++;

                // If there is no space to append more vertices, return
                if (straightPathCount >= maxStraightPath)
                {
                    return DtStatus.Success | DtStatus.BufferTooSmall;
                }

                // If reached end of path, return
                if (flags == DtStraightPathFlags.StraightpathEnd)
                {
                    return DtStatus.Success;
                }
            }

            return DtStatus.InProgress;
        }

        /// <summary>
        /// Appends intermediate portal points to the straight path.
        /// </summary>
        /// <param name="startIdx">The index of the first portal in the path corridor.</param>
        /// <param name="endIdx">The index of the last portal in the corridor.</param>
        /// <param name="endPos">The path end position. [(x, y, z)].</param>
        /// <param name="path">The path corridor used to derive the portals.</param>
        /// <param name="straightPath">The straight path positions to update.</param>
        /// <param name="straightPathFlags">The straight path flags array to update. [opt].</param>
        /// <param name="straightPathRefs">The straight path polygon references to update. [opt].</param>
        /// <param name="straightPathCount">On entry, the current count; on return, the updated count.</param>
        /// <param name="maxStraightPath">The capacity of the straight path arrays.</param>
        /// <param name="options">The straight path options controlling which crossings are recorded.</param>
        /// <returns>The status flags for the append operation.</returns>
        private DtStatus AppendPortals(
            int startIdx, int endIdx, in float3 endPos, DtPolyRef* path, float3* straightPath, DtStraightPathFlags* straightPathFlags,
            DtPolyRef* straightPathRefs, ref int straightPathCount, int maxStraightPath, DtStraightPathOptions options)
        {
            var startPos = straightPath[straightPathCount - 1];

            // Append or update last vertex
            for (var i = startIdx; i < endIdx; i++)
            {
                // Calculate portal
                var from = path[i];
                var status = this.navMesh->GetTileAndPolyByRef(from, out var fromTile, out var fromPoly);
                if (Detour.StatusFailed(status))
                {
                    return DtStatus.Failure | DtStatus.InvalidParam;
                }

                var to = path[i + 1];
                status = this.navMesh->GetTileAndPolyByRef(to, out var toTile, out var toPoly);
                if (Detour.StatusFailed(status))
                {
                    return DtStatus.Failure | DtStatus.InvalidParam;
                }

                status = this.GetPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, out var left, out var right);
                if (Detour.StatusFailed(status))
                {
                    break;
                }

                if ((options & DtStraightPathOptions.StraightPathAreaCrossings) != 0)
                {
                    // Skip intersection if only area crossings are requested
                    if (fromPoly->GetArea() == toPoly->GetArea())
                    {
                        continue;
                    }
                }

                // Append intersection
                if (Detour.IntersectSegSeg2D(startPos, endPos, left, right, out _, out var t))
                {
                    var pt = math.lerp(left, right, t);

                    var stat = this.AppendVertex(pt, 0, path[i + 1], straightPath, straightPathFlags, straightPathRefs, ref straightPathCount, maxStraightPath);
                    if (stat != DtStatus.InProgress)
                    {
                        return stat;
                    }
                }
            }

            return DtStatus.InProgress;
        }

        /// <summary>
        /// Appends a point to the straight path result.
        /// </summary>
        /// <param name="pos">The point to append. [(x, y, z)].</param>
        /// <param name="flags">The straight path flags for the point.</param>
        /// <param name="polyRef">The polygon reference entered at the point.</param>
        /// <param name="straightPath">The straight path positions to update.</param>
        /// <param name="straightPathFlags">The straight path flags array to update. [opt].</param>
        /// <param name="straightPathRefs">The polygon reference array to update. [opt].</param>
        /// <returns>The status flags for the append operation.</returns>
        private DtStatus AppendVertex(
            in float3 pos, DtStraightPathFlags flags, DtPolyRef polyRef, NativeList<float3> straightPath, NativeList<DtStraightPathFlags> straightPathFlags,
            NativeList<DtPolyRef> straightPathRefs)
        {
            var vertexCount = straightPath.Length;

            if (vertexCount > 0)
            {
                var previous = straightPath[vertexCount - 1];

                if (Detour.Equal(previous, pos))
                {
                    if (straightPathFlags.IsCreated)
                    {
                        if (straightPathFlags.Length <= vertexCount - 1)
                        {
                            straightPathFlags.ResizeUninitialized(vertexCount);
                        }

                        straightPathFlags[vertexCount - 1] = flags;
                    }

                    if (straightPathRefs.IsCreated)
                    {
                        if (straightPathRefs.Length <= vertexCount - 1)
                        {
                            straightPathRefs.ResizeUninitialized(vertexCount);
                        }

                        straightPathRefs[vertexCount - 1] = polyRef;
                    }

                    return flags == DtStraightPathFlags.StraightpathEnd ? DtStatus.Success : DtStatus.InProgress;
                }
            }

            straightPath.Add(pos);

            if (straightPathFlags.IsCreated)
            {
                if (straightPathFlags.Length <= vertexCount)
                {
                    straightPathFlags.Add(flags);
                }
                else
                {
                    straightPathFlags[vertexCount] = flags;
                }
            }

            if (straightPathRefs.IsCreated)
            {
                if (straightPathRefs.Length <= vertexCount)
                {
                    straightPathRefs.Add(polyRef);
                }
                else
                {
                    straightPathRefs[vertexCount] = polyRef;
                }
            }

            return flags == DtStraightPathFlags.StraightpathEnd ? DtStatus.Success : DtStatus.InProgress;
        }

        /// <summary>
        /// Appends intermediate portal points to the straight path.
        /// </summary>
        /// <param name="startIdx">The index of the first portal in the path corridor.</param>
        /// <param name="endIdx">The index of the last portal in the corridor.</param>
        /// <param name="endPos">The path end position. [(x, y, z)].</param>
        /// <param name="path">The path corridor used to derive the portals.</param>
        /// <param name="straightPath">The straight path positions to update.</param>
        /// <param name="straightPathFlags">The straight path flags array to update. [opt].</param>
        /// <param name="straightPathRefs">The straight path polygon references to update. [opt].</param>
        /// <param name="options">The straight path options controlling which crossings are recorded.</param>
        /// <returns>The status flags for the append operation.</returns>
        private DtStatus AppendPortals(
            int startIdx, int endIdx, in float3 endPos, DtPolyRef* path, NativeList<float3> straightPath, NativeList<DtStraightPathFlags> straightPathFlags,
            NativeList<DtPolyRef> straightPathRefs, DtStraightPathOptions options)
        {
            var vertexCount = straightPath.Length;

            if (vertexCount <= 0)
            {
                return DtStatus.InProgress;
            }

            var startPos = straightPath[vertexCount - 1];

            for (var i = startIdx; i < endIdx; i++)
            {
                var from = path[i];
                var status = this.navMesh->GetTileAndPolyByRef(from, out var fromTile, out var fromPoly);
                if (Detour.StatusFailed(status))
                {
                    return DtStatus.Failure | DtStatus.InvalidParam;
                }

                var to = path[i + 1];
                status = this.navMesh->GetTileAndPolyByRef(to, out var toTile, out var toPoly);
                if (Detour.StatusFailed(status))
                {
                    return DtStatus.Failure | DtStatus.InvalidParam;
                }

                status = this.GetPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, out var left, out var right);
                if (Detour.StatusFailed(status))
                {
                    break;
                }

                if ((options & DtStraightPathOptions.StraightPathAreaCrossings) != 0 && fromPoly->GetArea() == toPoly->GetArea())
                {
                    continue;
                }

                if (Detour.IntersectSegSeg2D(startPos, endPos, left, right, out _, out var t))
                {
                    var pt = math.lerp(left, right, t);

                    var appendStatus = this.AppendVertex(pt, 0, path[i + 1], straightPath, straightPathFlags, straightPathRefs);
                    if (appendStatus != DtStatus.InProgress)
                    {
                        return appendStatus;
                    }
                }
            }

            return DtStatus.InProgress;
        }

        /// <summary>
        /// Casts a walkability ray along the navigation mesh surface from the start position toward the end position.
        /// </summary>
        /// <param name="startRef">The reference id of the start polygon.</param>
        /// <param name="startPos">A position within the start polygon that represents the start of the ray. [(x, y, z)].</param>
        /// <param name="endPos">The position to cast the ray toward. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="t">Receives the hit parameter (float.MaxValue if no wall is hit).</param>
        /// <param name="hitNormal">Receives the normal of the nearest wall hit. [(x, y, z)].</param>
        /// <param name="path">Receives the references of the visited polygons. [opt].</param>
        /// <param name="pathCount">Receives the number of visited polygons. [opt].</param>
        /// <param name="maxPath">The maximum number of polygons that <paramref name="path"/> can hold.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>This is a convenience wrapper around the overload that fills a <see cref="DtRaycastHit"/> structure and
        /// is retained for backward compatibility.</para>
        /// </remarks>
        public DtStatus Raycast(
            DtPolyRef startRef, in float3 startPos, in float3 endPos, ref DtQueryFilter filter, out float t, out float3 hitNormal, DtPolyRef* path,
            out int pathCount, int maxPath)
        {
            var hit = new DtRaycastHit
            {
                path = path,
                maxPath = maxPath,
            };

            var status = this.Raycast(startRef, startPos, endPos, ref filter, 0, &hit);

            t = hit.t;
            hitNormal = hit.hitNormal;
            pathCount = hit.pathCount;

            return status;
        }

        /// <summary>
        /// Casts a walkability ray along the navigation mesh surface from the start position toward the end position.
        /// </summary>
        /// <param name="startRef">The reference id of the start polygon.</param>
        /// <param name="startPos">A position within the start polygon that represents the start of the ray. [(x, y, z)].</param>
        /// <param name="endPos">The position to cast the ray toward. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="options">Controls how the raycast behaves (see <see cref="DtRaycastOptions"/>).</param>
        /// <param name="hit">Receives the raycast results.</param>
        /// <param name="prevRef">The parent of <paramref name="startRef"/> used for cost calculation. [opt].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>This method is optimized for short checks. If the results arrays are too small they are filled from the
        /// start toward the end position.</para>
        /// <para>If <c>hit.t</c> is float.MaxValue, the ray reached the end position and the path represents a valid
        /// corridor to that point; the hit normal is undefined. If <c>hit.t</c> is zero, the start position itself is on
        /// the wall. For values where 0 &lt; t &lt; 1, the intersection occurs at <c>startPos + (endPos - startPos) * t</c>.</para>
        /// <para>The raycast ignores the y-value of the end position (a 2D check). For example, casting toward a balcony
        /// that overhangs the start polygon would report success when reaching the balcony's x/z footprint.</para>
        /// </remarks>
        public DtStatus Raycast(
            DtPolyRef startRef, in float3 startPos, in float3 endPos, ref DtQueryFilter filter, DtRaycastOptions options, DtRaycastHit* hit,
            DtPolyRef prevRef = default)
        {
            if (hit == null)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            hit->t = 0;
            hit->pathCount = 0;
            hit->pathCost = 0;

            // Validate input
            if (!this.navMesh->IsValidPolyRef(startRef) || !Detour.IsFinite(startPos) || !Detour.IsFinite(endPos) ||
                (prevRef != 0 && !this.navMesh->IsValidPolyRef(prevRef)))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var verts = stackalloc float3[Detour.DTVertsPerPolygon + 1];
            var n = 0;

            var curPos = startPos;
            var dir = endPos - startPos;
            hit->hitNormal = float3.zero;

            var status = DtStatus.Success;

            var curRef = startRef;
            this.navMesh->GetTileAndPolyByRefUnsafe(curRef, out var tile, out var poly);
            var nextTile = tile;
            var nextPoly = poly;
            var prevTile = tile;
            var prevPoly = poly;
            if (prevRef != 0)
            {
                this.navMesh->GetTileAndPolyByRefUnsafe(prevRef, out prevTile, out prevPoly);
            }

            while (curRef != 0)
            {
                // Cast ray against current polygon

                // Collect vertices
                var nv = 0;
                for (var i = 0; i < poly->vertCount; ++i)
                {
                    verts[nv] = tile->verts[poly->verts[i]];
                    nv++;
                }

                if (!Detour.IntersectSegmentPoly2D(startPos, endPos, verts, nv, out _, out var tmax, out _, out var segMax))
                {
                    // Could not hit the polygon, keep the old t and report hit
                    hit->pathCount = n;
                    return status;
                }

                hit->hitEdgeIndex = segMax;

                // Keep track of furthest t so far
                if (tmax > hit->t)
                {
                    hit->t = tmax;
                }

                // Store visited polygons
                if (n < hit->maxPath)
                {
                    hit->path[n++] = curRef;
                }
                else
                {
                    status |= DtStatus.BufferTooSmall;
                }

                // Ray end is completely inside the polygon
                if (segMax == -1)
                {
                    hit->t = float.MaxValue;
                    hit->pathCount = n;

                    // add the cost
                    if ((options & DtRaycastOptions.RaycastUseCosts) != 0)
                    {
                        hit->pathCost += filter.GetCost(curPos, endPos, prevRef, prevTile, prevPoly, curRef, tile, poly, curRef, tile, poly);
                    }

                    return status;
                }

                // Follow neighbours
                DtPolyRef nextRef = 0;

                for (var i = poly->firstLink; i != Detour.DTNullLink; i = tile->links[i].next)
                {
                    var link = &tile->links[i];

                    // Find link which contains this edge
                    if (link->edge != segMax)
                    {
                        continue;
                    }

                    // Get pointer to the next polygon
                    this.navMesh->GetTileAndPolyByRefUnsafe(link->polyRef, out nextTile, out nextPoly);

                    // Skip off-mesh connections
                    if (nextPoly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
                    {
                        continue;
                    }

                    // Skip links based on filter
                    if (!filter.PassFilter(link->polyRef, nextTile, nextPoly))
                    {
                        continue;
                    }

                    // If the link is internal, just return the ref
                    if (link->side == 0xff)
                    {
                        nextRef = link->polyRef;
                        break;
                    }

                    // Check if the link spans the whole edge, and accept
                    if (link->bmin == 0 && link->bmax == 255)
                    {
                        nextRef = link->polyRef;
                        break;
                    }

                    // Check for partial edge links
                    var v0 = poly->verts[link->edge];
                    var v1 = poly->verts[(link->edge + 1) % poly->vertCount];
                    var left = tile->verts[v0];
                    var right = tile->verts[v1];

                    // Check that the intersection lies inside the link portal
                    if (link->side == 0 || link->side == 4)
                    {
                        // Calculate link size
                        const float s = 1.0f / 255.0f;
                        var lmin = left.z + ((right.z - left.z) * (link->bmin * s));
                        var lmax = left.z + ((right.z - left.z) * (link->bmax * s));
                        if (lmin > lmax)
                        {
                            Detour.Swap(ref lmin, ref lmax);
                        }

                        // Find Z intersection
                        var z = startPos.z + ((endPos.z - startPos.z) * tmax);
                        if (z >= lmin && z <= lmax)
                        {
                            nextRef = link->polyRef;
                            break;
                        }
                    }
                    else if (link->side == 2 || link->side == 6)
                    {
                        // Calculate link size
                        const float s = 1.0f / 255.0f;
                        var lmin = left.x + ((right.x - left.x) * (link->bmin * s));
                        var lmax = left.x + ((right.x - left.x) * (link->bmax * s));
                        if (lmin > lmax)
                        {
                            Detour.Swap(ref lmin, ref lmax);
                        }

                        // Find X intersection
                        var x = startPos.x + ((endPos.x - startPos.x) * tmax);
                        if (x >= lmin && x <= lmax)
                        {
                            nextRef = link->polyRef;
                            break;
                        }
                    }
                }

                // add the cost
                if ((options & DtRaycastOptions.RaycastUseCosts) != 0)
                {
                    // compute the intersection point at the furthest end of the polygon
                    // and correct the height (since the raycast moves in 2d)
                    var lastPos = curPos;
                    curPos = startPos + (dir * hit->t);
                    var e1 = verts[segMax];
                    var e2 = verts[(segMax + 1) % nv];
                    var eDir = e2 - e1;
                    var diff = curPos - e1;
                    var s = math.abs(eDir.x) > math.abs(eDir.z) ? diff.x / eDir.x : diff.z / eDir.z;
                    curPos.y = e1.y + (eDir.y * s);

                    hit->pathCost += filter.GetCost(lastPos, curPos, prevRef, prevTile, prevPoly, curRef, tile, poly, nextRef, nextTile, nextPoly);
                }

                if (nextRef == 0)
                {
                    // No neighbour, we hit a wall

                    // Calculate hit normal
                    var a = segMax;
                    var b = segMax + 1 < nv ? segMax + 1 : 0;
                    var va = verts[a];
                    var vb = verts[b];
                    var dx = vb.x - va.x;
                    var dz = vb.z - va.z;
                    hit->hitNormal = math.normalize(new float3(dz, 0, -dx));

                    hit->pathCount = n;
                    return status;
                }

                // No hit, advance to neighbour polygon
                prevRef = curRef;
                prevTile = tile;
                prevPoly = poly;
                curRef = nextRef;
                tile = nextTile;
                poly = nextPoly;
            }

            hit->pathCount = n;
            return status;
        }

        /// <summary>
        /// Moves from the start to the end position while remaining constrained to the navigation mesh.
        /// </summary>
        /// <param name="startRef">The reference id of the start polygon.</param>
        /// <param name="startPos">A position within the start polygon. [(x, y, z)].</param>
        /// <param name="endPos">The desired end position. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="resultPos">Receives the resulting constrained position. [(x, y, z)].</param>
        /// <param name="visited">Receives the references of the polygons visited during the move.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Optimized for small movements and a limited number of polygons. If used for large distances the result
        /// may be incomplete.</para>
        /// <para><paramref name="resultPos"/> equals <paramref name="endPos"/> when the end is reachable; otherwise the
        /// closest reachable position is returned.</para>
        /// <para>The result position is not projected onto the mesh surface. Call <see cref="GetPolyHeight"/> if surface
        /// height information is required.</para>
        /// </remarks>
        public DtStatus MoveAlongSurface(
            DtPolyRef startRef, in float3 startPos, in float3 endPos, ref DtQueryFilter filter, out float3 resultPos, NativeList<DtPolyRef> visited)
        {
            resultPos = default;

            if (!this.navMesh->IsValidPolyRef(startRef) || !Detour.IsFinite(startPos) || !Detour.IsFinite(endPos))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var status = DtStatus.Success;

            const int maxStack = 48;
            var stack = stackalloc DtNode*[maxStack];
            var nStack = 0;

            this.tinyNodePool->Clear();

            var startNode = this.tinyNodePool->GetNode(startRef);
            startNode->ParentIndex = 0;
            startNode->cost = 0;
            startNode->total = 0;
            startNode->id = startRef;
            startNode->Flags = DtNodeFlags.DT_NODE_CLOSED;
            stack[nStack++] = startNode;

            var bestPos = startPos;
            var bestDist = float.MaxValue;
            DtNode* bestNode = null;

            // Search constraints
            var searchPos = math.lerp(startPos, endPos, 0.5f);
            var searchRad = (math.distance(startPos, endPos) * 0.5f) + 0.001f;
            var searchRadSqr = searchRad * searchRad;

            Span<float3> verts = stackalloc float3[Detour.DTVertsPerPolygon];
            const int maxNeis = 8;
            var neis = stackalloc DtPolyRef[maxNeis];

            while (nStack > 0)
            {
                // Pop front
                var curNode = stack[0];
                for (var i = 0; i < nStack - 1; ++i)
                {
                    stack[i] = stack[i + 1];
                }

                nStack--;

                // Get poly and tile
                var curRef = curNode->id;
                this.navMesh->GetTileAndPolyByRefUnsafe(curRef, out var curTile, out var curPoly);

                // Collect vertices
                var nverts = curPoly->vertCount;
                for (var i = 0; i < nverts; ++i)
                {
                    verts[i] = curTile->verts[curPoly->verts[i]];
                }

                // If target is inside the poly, stop search
                if (Detour.PointInPolygon(endPos, verts[..nverts]))
                {
                    bestNode = curNode;
                    bestPos = endPos;
                    break;
                }

                // Find wall edges and find nearest point inside the walls
                for (int i = 0, j = nverts - 1; i < nverts; j = i++)
                {
                    // Find links to neighbours
                    var nneis = 0;

                    if ((curPoly->neis[j] & Detour.DTExtLink) != 0)
                    {
                        // Tile border
                        for (var k = curPoly->firstLink; k != Detour.DTNullLink; k = curTile->links[k].next)
                        {
                            var link = &curTile->links[k];
                            if (link->edge == j)
                            {
                                if (link->polyRef != 0)
                                {
                                    this.navMesh->GetTileAndPolyByRefUnsafe(link->polyRef, out var neiTile, out var neiPoly);
                                    if (filter.PassFilter(link->polyRef, neiTile, neiPoly))
                                    {
                                        if (nneis < maxNeis)
                                        {
                                            neis[nneis++] = link->polyRef;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (curPoly->neis[j] != 0)
                    {
                        var idx = curPoly->neis[j] - 1;
                        var polyRef = this.navMesh->GetPolyRefBase(curTile) | (DtPolyRef)idx;
                        if (filter.PassFilter(polyRef, curTile, &curTile->polys[idx]))
                        {
                            // Internal edge, encode id
                            neis[nneis++] = polyRef;
                        }
                    }

                    if (nneis == 0)
                    {
                        // Wall edge, calc distance
                        var vj = verts[j];
                        var vi = verts[i];
                        var distSqr = Detour.DistancePtSegSqr2D(endPos, vj, vi, out var tseg);
                        if (distSqr < bestDist)
                        {
                            // Update nearest distance
                            bestPos = math.lerp(vj, vi, tseg);
                            bestDist = distSqr;
                            bestNode = curNode;
                        }
                    }
                    else
                    {
                        for (var k = 0; k < nneis; ++k)
                        {
                            // Skip if no node can be allocated
                            var neighbourNode = this.tinyNodePool->GetNode(neis[k]);
                            if (neighbourNode == null)
                            {
                                continue;
                            }

                            // Skip if already visited
                            if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                            {
                                continue;
                            }

                            // Skip the link if it is too far from search constraint
                            var vj = verts[j];
                            var vi = verts[i];
                            var distSqr = Detour.DistancePtSegSqr2D(searchPos, vj, vi, out _);
                            if (distSqr > searchRadSqr)
                            {
                                continue;
                            }

                            // Mark as the node as visited and push to queue
                            if (nStack < maxStack)
                            {
                                neighbourNode->ParentIndex = this.tinyNodePool->GetNodeIdx(curNode);
                                neighbourNode->Flags |= DtNodeFlags.DT_NODE_CLOSED;
                                stack[nStack++] = neighbourNode;
                            }
                        }
                    }
                }
            }

            if (bestNode != null)
            {
                // Reverse the path
                DtNode* prev = null;
                var node = bestNode;
                do
                {
                    var next = this.tinyNodePool->GetNodeAtIdx(node->ParentIndex);
                    node->ParentIndex = this.tinyNodePool->GetNodeIdx(prev);
                    prev = node;
                    node = next;
                }
                while (node != null);

                // Store result
                node = prev;
                do
                {
                    visited.Add(node->id);
                    node = this.tinyNodePool->GetNodeAtIdx(node->ParentIndex);
                }
                while (node != null);
            }

            resultPos = bestPos;

            return status;
        }

        /// <summary>
        /// Initializes a sliced path query.
        /// </summary>
        /// <param name="startRef">The reference id of the start polygon.</param>
        /// <param name="endRef">The reference id of the end polygon.</param>
        /// <param name="startPos">A position within the start polygon. [(x, y, z)].</param>
        /// <param name="endPos">A position within the end polygon. [(x, y, z)].</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="options">Query options (see <see cref="DtFindPathOptions"/>).</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>The filter pointer is stored and used for the duration of the sliced path query.</para>
        /// <para>Do not call non-sliced methods before completing the sliced query via
        /// <see cref="FinalizeSlicedFindPath"/> or <see cref="FinalizeSlicedFindPathPartial"/>; doing so may corrupt the
        /// internal state.</para>
        /// </remarks>
        public DtStatus InitSlicedFindPath(
            DtPolyRef startRef, DtPolyRef endRef, in float3 startPos, in float3 endPos, ref DtQueryFilter filter,
            DtFindPathOptions options = DtFindPathOptions.None)
        {
            // Init path state.
            this.queryData = default;
            this.queryData.Status = DtStatus.Failure;
            this.queryData.StartRef = startRef;
            this.queryData.EndRef = endRef;
            this.queryData.StartPos = startPos;
            this.queryData.EndPos = endPos;
            this.queryData.Filter = (DtQueryFilter*)UnsafeUtility.AddressOf(ref filter);
            this.queryData.Options = options;
            this.queryData.RaycastLimitSqr = float.MaxValue;

            // Validate input
            if (!this.navMesh->IsValidPolyRef(startRef) || !this.navMesh->IsValidPolyRef(endRef) || !Detour.IsFinite(startPos) || !Detour.IsFinite(endPos))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            // trade quality with performance?
            if ((options & DtFindPathOptions.FindpathAnyAngle) != 0)
            {
                // limiting to several times the character radius yields nice results. It is not sensitive
                // so it is enough to compute it from the first tile.
                var tile = this.navMesh->GetTileByRef(startRef);
                if (tile != null)
                {
                    var agentRadius = tile->header->walkableRadius;
                    this.queryData.RaycastLimitSqr = math.pow(agentRadius * Detour.DTRayCastLimitProportions, 2);
                }
            }

            if (startRef == endRef)
            {
                this.queryData.Status = DtStatus.Success;
                return DtStatus.Success;
            }

            this.nodePool->Clear();
            this.openList->Clear();

            var startNode = this.nodePool->GetNode(startRef);
            startNode->pos = startPos;
            startNode->ParentIndex = 0;
            startNode->cost = 0;
            startNode->total = math.distance(startPos, endPos) * HScale;
            startNode->id = startRef;
            startNode->Flags = DtNodeFlags.DT_NODE_OPEN;
            this.openList->Push(startNode);

            this.queryData.Status = DtStatus.InProgress;
            this.queryData.LastBestNode = startNode;
            this.queryData.LastBestNodeCost = startNode->total;

            return this.queryData.Status;
        }

        /// <summary>
        /// Updates an in-progress sliced path query.
        /// </summary>
        /// <param name="maxIter">The maximum number of iterations to perform.</param>
        /// <param name="doneIters">Receives the actual number of iterations completed.</param>
        /// <returns>The status flags for the query.</returns>
        public DtStatus UpdateSlicedFindPath(int maxIter, out int doneIters)
        {
            doneIters = 0;

            if (!Detour.StatusInProgress(this.queryData.Status))
            {
                return this.queryData.Status;
            }

            // Make sure the request is still valid.
            if (!this.navMesh->IsValidPolyRef(this.queryData.StartRef) || !this.navMesh->IsValidPolyRef(this.queryData.EndRef))
            {
                this.queryData.Status = DtStatus.Failure;
                return DtStatus.Failure;
            }

            var rayHit = new DtRaycastHit { maxPath = 0 };

            var iter = 0;
            while (iter < maxIter && !this.openList->Empty)
            {
                iter++;

                // Pop node from open list and add to closed list.
                var bestNode = this.openList->Pop();
                bestNode->Flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode->Flags |= DtNodeFlags.DT_NODE_CLOSED;

                // Reached the goal, stop searching.
                if (bestNode->id == this.queryData.EndRef)
                {
                    this.queryData.LastBestNode = bestNode;
                    var details = this.queryData.Status & DtStatus.StatusDetailMask;
                    this.queryData.Status = DtStatus.Success | details;
                    doneIters = iter;
                    return this.queryData.Status;
                }

                var bestRef = bestNode->id;
                if (Detour.StatusFailed(this.navMesh->GetTileAndPolyByRef(bestRef, out var bestTile, out var bestPoly)))
                {
                    this.queryData.Status = DtStatus.Failure;
                    doneIters = iter;
                    return this.queryData.Status;
                }

                // Get parent and grandparent poly and tile.
                DtPolyRef parentRef = 0, grandpaRef = 0;
                DtMeshTile* parentTile = null;
                DtPoly* parentPoly = null;
                DtNode* parentNode = null;
                if (bestNode->ParentIndex != 0)
                {
                    parentNode = this.nodePool->GetNodeAtIdx(bestNode->ParentIndex);
                    parentRef = parentNode->id;
                    if (parentNode->ParentIndex != 0)
                    {
                        grandpaRef = this.nodePool->GetNodeAtIdx(parentNode->ParentIndex)->id;
                    }
                }

                if (parentRef != 0)
                {
                    if (Detour.StatusFailed(this.navMesh->GetTileAndPolyByRef(parentRef, out parentTile, out parentPoly)) ||
                        (grandpaRef != 0 && !this.navMesh->IsValidPolyRef(grandpaRef)))
                    {
                        this.queryData.Status = DtStatus.Failure;
                        doneIters = iter;
                        return this.queryData.Status;
                    }
                }

                // Decide whether to test raycast to previous nodes
                var tryLOS = false;
                if ((this.queryData.Options & DtFindPathOptions.FindpathAnyAngle) != 0)
                {
                    if (parentRef != 0 && math.distancesq(parentNode->pos, bestNode->pos) < this.queryData.RaycastLimitSqr)
                    {
                        tryLOS = true;
                    }
                }

                for (var i = bestPoly->firstLink; i != Detour.DTNullLink; i = bestTile->links[i].next)
                {
                    var neighbourRef = bestTile->links[i].polyRef;
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    this.navMesh->GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);
                    if (!this.queryData.Filter->PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    var neighbourNode = this.nodePool->GetNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        this.queryData.Status |= DtStatus.OutOfNodes;
                        continue;
                    }

                    if (neighbourNode->ParentIndex != 0 && neighbourNode->ParentIndex == bestNode->ParentIndex)
                    {
                        continue;
                    }

                    if (neighbourNode->Flags == 0)
                    {
                        this.GetEdgeMidPoint(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, out neighbourNode->pos);
                    }

                    // Raycast parent
                    var foundShortCut = false;
                    rayHit.pathCost = rayHit.t = 0;
                    if (tryLOS)
                    {
                        this.Raycast(parentRef, parentNode->pos, neighbourNode->pos, ref *this.queryData.Filter, DtRaycastOptions.RaycastUseCosts, &rayHit,
                            grandpaRef);

                        foundShortCut = rayHit.t >= 1.0f;
                    }

                    // Update move cost
                    float cost;
                    if (foundShortCut)
                    {
                        cost = parentNode->cost + rayHit.pathCost;
                    }
                    else
                    {
                        var curCost = this.queryData.Filter->GetCost(bestNode->pos, neighbourNode->pos, parentRef, parentTile, parentPoly, bestRef, bestTile,
                            bestPoly, neighbourRef, neighbourTile, neighbourPoly);

                        cost = bestNode->cost + curCost;
                    }

                    // Heuristic
                    float heuristic;
                    if (neighbourRef == this.queryData.EndRef)
                    {
                        var endCost = this.queryData.Filter->GetCost(neighbourNode->pos, this.queryData.EndPos, bestRef, bestTile, bestPoly, neighbourRef,
                            neighbourTile, neighbourPoly, 0, null, null);

                        cost += endCost;
                        heuristic = 0;
                    }
                    else
                    {
                        heuristic = math.distance(neighbourNode->pos, this.queryData.EndPos) * HScale;
                    }

                    var total = cost + heuristic;

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode->total)
                    {
                        continue;
                    }

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_CLOSED) != 0 && total >= neighbourNode->total)
                    {
                        continue;
                    }

                    neighbourNode->ParentIndex = foundShortCut ? bestNode->ParentIndex : this.nodePool->GetNodeIdx(bestNode);
                    neighbourNode->id = neighbourRef;
                    neighbourNode->Flags &= ~(DtNodeFlags.DT_NODE_CLOSED | DtNodeFlags.DT_NODE_PARENT_DETACHED);
                    neighbourNode->cost = cost;
                    neighbourNode->total = total;
                    if (foundShortCut)
                    {
                        neighbourNode->Flags |= DtNodeFlags.DT_NODE_PARENT_DETACHED;
                    }

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        this.openList->Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode->Flags |= DtNodeFlags.DT_NODE_OPEN;
                        this.openList->Push(neighbourNode);
                    }

                    if (heuristic < this.queryData.LastBestNodeCost)
                    {
                        this.queryData.LastBestNodeCost = heuristic;
                        this.queryData.LastBestNode = neighbourNode;
                    }
                }
            }

            if (this.openList->Empty)
            {
                var details = this.queryData.Status & DtStatus.StatusDetailMask;
                this.queryData.Status = DtStatus.Success | details;
            }

            doneIters = iter;

            return this.queryData.Status;
        }

        /// <summary>
        /// Finalizes and returns the results of a sliced path query.
        /// </summary>
        /// <param name="path">Receives an ordered list of polygon references representing the path (start to end).</param>
        /// <param name="pathCount">Receives the number of polygons written to <paramref name="path"/>.</param>
        /// <param name="maxPath">The maximum number of polygons that <paramref name="path"/> can hold. [Limit: &gt;= 1].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Calling any non-sliced query method before this function may corrupt the in-progress sliced query state.</para>
        /// <para>If the full path does not fit in <paramref name="path"/>, the buffer is filled to capacity from the
        /// start polygon toward the end polygon.</para>
        /// </remarks>
        public DtStatus FinalizeSlicedFindPath(DtPolyRef* path, out int pathCount, int maxPath)
        {
            pathCount = 0;
            if (path == null || maxPath <= 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (Detour.StatusFailed(this.queryData.Status))
            {
                this.queryData = default;
                return DtStatus.Failure;
            }

            var n = 0;
            if (this.queryData.StartRef == this.queryData.EndRef)
            {
                path[n++] = this.queryData.StartRef;
            }
            else
            {
                if (this.queryData.LastBestNode->id != this.queryData.EndRef)
                {
                    this.queryData.Status |= DtStatus.PartialResult;
                }

                DtNode* prev = null;
                var node = this.queryData.LastBestNode;
                var prevRay = 0u;
                do
                {
                    var next = this.nodePool->GetNodeAtIdx(node->ParentIndex);
                    node->ParentIndex = this.nodePool->GetNodeIdx(prev);
                    prev = node;
                    var nextRay = (uint)node->Flags & (uint)DtNodeFlags.DT_NODE_PARENT_DETACHED;
                    node->Flags = (node->Flags & ~DtNodeFlags.DT_NODE_PARENT_DETACHED) | (DtNodeFlags)prevRay;
                    prevRay = nextRay;
                    node = next;
                }
                while (node != null);

                node = prev;
                do
                {
                    var next = this.nodePool->GetNodeAtIdx(node->ParentIndex);
                    DtStatus status;
                    if ((node->Flags & DtNodeFlags.DT_NODE_PARENT_DETACHED) != 0)
                    {
                        status = this.Raycast(node->id, node->pos, next->pos, ref *this.queryData.Filter, out _, out _, path + n, out var m, maxPath - n);
                        n += m;
                        if (path[n - 1] == next->id)
                        {
                            n--;
                        }
                    }
                    else
                    {
                        path[n++] = node->id;
                        status = n >= maxPath ? DtStatus.BufferTooSmall : DtStatus.Success;
                    }

                    if ((status & DtStatus.StatusDetailMask) != 0)
                    {
                        this.queryData.Status |= status & DtStatus.StatusDetailMask;
                        break;
                    }

                    node = next;
                }
                while (node != null);
            }

            var details = this.queryData.Status & DtStatus.StatusDetailMask;
            this.queryData = default;
            pathCount = n;
            return DtStatus.Success | details;
        }

        /// <summary>
        /// Finalizes and returns the results of a sliced path query using a dynamically sized buffer.
        /// </summary>
        /// <param name="path">Receives an ordered list of polygon references representing the path (start to end).</param>
        /// <returns>The status flags for the query.</returns>
        public DtStatus FinalizeSlicedFindPath(ref UnsafeList<DtPolyRef> path)
        {
            if (!path.IsCreated)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            path.Clear();

            if (Detour.StatusFailed(this.queryData.Status))
            {
                this.queryData = default;
                return DtStatus.Failure;
            }

            if (this.queryData.StartRef == this.queryData.EndRef)
            {
                path.Add(this.queryData.StartRef);
            }
            else
            {
                if (this.queryData.LastBestNode->id != this.queryData.EndRef)
                {
                    this.queryData.Status |= DtStatus.PartialResult;
                }

                DtNode* prev = null;
                var node = this.queryData.LastBestNode;
                var prevRay = 0u;
                do
                {
                    var next = this.nodePool->GetNodeAtIdx(node->ParentIndex);
                    node->ParentIndex = this.nodePool->GetNodeIdx(prev);
                    prev = node;
                    var nextRay = (uint)node->Flags & (uint)DtNodeFlags.DT_NODE_PARENT_DETACHED;
                    node->Flags = (node->Flags & ~DtNodeFlags.DT_NODE_PARENT_DETACHED) | (DtNodeFlags)prevRay;
                    prevRay = nextRay;
                    node = next;
                }
                while (node != null);

                node = prev;
                while (node != null)
                {
                    var next = this.nodePool->GetNodeAtIdx(node->ParentIndex);
                    DtStatus status;

                    if ((node->Flags & DtNodeFlags.DT_NODE_PARENT_DETACHED) != 0)
                    {
                        status = this.AppendRaycastSegment(node, next, ref path);
                    }
                    else
                    {
                        path.Add(node->id);
                        status = DtStatus.Success;
                    }

                    if ((status & DtStatus.StatusDetailMask) != 0)
                    {
                        this.queryData.Status |= status & DtStatus.StatusDetailMask;
                        break;
                    }

                    node = next;
                }
            }

            var details = this.queryData.Status & DtStatus.StatusDetailMask;
            this.queryData = default;
            return DtStatus.Success | details;
        }

        private DtStatus AppendRaycastSegment(DtNode* node, DtNode* next, ref UnsafeList<DtPolyRef> path)
        {
            const int InitialBufferSize = 32;

            var stackBuffer = stackalloc DtPolyRef[InitialBufferSize];
            DtPolyRef* buffer = stackBuffer;
            DtPolyRef* allocatedBuffer;
            var capacity = InitialBufferSize;
            var maxRaycastPath = this.nodePool != null ? this.nodePool->MaxNodes : int.MaxValue;

            DtStatus status;
            int count;

            while (true)
            {
                var hit = new DtRaycastHit
                {
                    path = buffer,
                    maxPath = capacity,
                };

                status = this.Raycast(node->id, node->pos, next->pos, ref *this.queryData.Filter, 0, &hit);
                count = hit.pathCount;

                if ((status & DtStatus.BufferTooSmall) == 0 || capacity >= maxRaycastPath)
                {
                    break;
                }

                capacity = math.min(capacity * 2, maxRaycastPath);

                allocatedBuffer = (DtPolyRef*)AllocatorManager.Allocate(Allocator.Temp, capacity * sizeof(DtPolyRef), UnsafeUtility.AlignOf<DtPolyRef>());
                buffer = allocatedBuffer;
            }

            if (count > 0 && Detour.StatusSucceed(status))
            {
                path.AddRange(buffer, count);

                if (next != null && path.Length > 0 && path[path.Length - 1] == next->id)
                {
                    path.Length -= 1;
                }
            }

            return status;
        }

        /// <summary>
        /// Finalizes and returns the results of an incomplete sliced path query.
        /// </summary>
        /// <param name="existing">An array of polygon references that describe an existing path.</param>
        /// <param name="existingSize">The number of polygons in <paramref name="existing"/>.</param>
        /// <param name="path">Receives an ordered list of polygon references representing the path (start to end).</param>
        /// <param name="pathCount">Receives the number of polygons written to <paramref name="path"/>.</param>
        /// <param name="maxPath">The maximum number of polygons that <paramref name="path"/> can hold. [Limit: &gt;= 1].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>The method returns a path to the furthest polygon on the existing path that was visited during the
        /// search.</para>
        /// <para>If the full path does not fit in <paramref name="path"/>, the buffer is filled to capacity from the
        /// start polygon toward the end polygon.</para>
        /// </remarks>
        public DtStatus FinalizeSlicedFindPathPartial(DtPolyRef* existing, int existingSize, DtPolyRef* path, out int pathCount, int maxPath)
        {
            pathCount = 0;
            if (existing == null || existingSize <= 0 || path == null || maxPath <= 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (Detour.StatusFailed(this.queryData.Status))
            {
                this.queryData = default;
                return DtStatus.Failure;
            }

            var n = 0;
            if (this.queryData.StartRef == this.queryData.EndRef)
            {
                path[n++] = this.queryData.StartRef;
            }
            else
            {
                DtNode* node = null;
                var nodes = stackalloc DtNode*[1];
                for (var i = existingSize - 1; i >= 0; --i)
                {
                    if (this.nodePool->FindNodes(existing[i], nodes, 1) > 0)
                    {
                        node = nodes[0];
                        break;
                    }
                }

                if (node == null)
                {
                    this.queryData.Status |= DtStatus.PartialResult;
                    node = this.queryData.LastBestNode;
                }

                DtNode* prev = null;
                var prevRay = 0u;
                do
                {
                    var next = this.nodePool->GetNodeAtIdx(node->ParentIndex);
                    node->ParentIndex = this.nodePool->GetNodeIdx(prev);
                    prev = node;
                    var nextRay = (uint)node->Flags & (uint)DtNodeFlags.DT_NODE_PARENT_DETACHED;
                    node->Flags = (node->Flags & ~DtNodeFlags.DT_NODE_PARENT_DETACHED) | (DtNodeFlags)prevRay;
                    prevRay = nextRay;
                    node = next;
                }
                while (node != null);

                node = prev;
                do
                {
                    var next = this.nodePool->GetNodeAtIdx(node->ParentIndex);
                    DtStatus status;
                    if ((node->Flags & DtNodeFlags.DT_NODE_PARENT_DETACHED) != 0)
                    {
                        status = this.Raycast(node->id, node->pos, next->pos, ref *this.queryData.Filter, out _, out _, path + n, out var m, maxPath - n);
                        n += m;
                        if (path[n - 1] == next->id)
                        {
                            n--;
                        }
                    }
                    else
                    {
                        path[n++] = node->id;
                        status = n >= maxPath ? DtStatus.BufferTooSmall : DtStatus.Success;
                    }

                    if ((status & DtStatus.StatusDetailMask) != 0)
                    {
                        this.queryData.Status |= status & DtStatus.StatusDetailMask;
                        break;
                    }

                    node = next;
                }
                while (node != null);
            }

            var details = this.queryData.Status & DtStatus.StatusDetailMask;
            this.queryData = default;
            pathCount = n;
            return DtStatus.Success | details;
        }

        /// <summary>
        /// Finds the polygons along the navigation graph that touch the specified circle.
        /// </summary>
        /// <param name="startRef">The reference id of the polygon where the search starts.</param>
        /// <param name="centerPos">The center of the search circle. [(x, y, z)].</param>
        /// <param name="radius">The radius of the search circle.</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="resultRef">Receives the reference ids of the polygons touched by the circle. [opt].</param>
        /// <param name="resultParent">Receives the parent polygon for each result (zero if none). [opt].</param>
        /// <param name="resultCost">Receives the search cost from <paramref name="centerPos"/> to each polygon. [opt].</param>
        /// <param name="resultCount">Receives the number of polygons returned.</param>
        /// <param name="maxResult">The maximum number of polygons the result arrays can hold.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>The results are ordered from lowest to highest traversal cost. If the result buffers are too small they
        /// are filled to capacity.</para>
        /// <para>Polygons are discovered by traversing the navigation graph beginning at
        /// <paramref name="startRef"/>.</para>
        /// </remarks>
        public DtStatus FindPolysAroundCircle(
            DtPolyRef startRef, in float3 centerPos, float radius, ref DtQueryFilter filter, DtPolyRef* resultRef, DtPolyRef* resultParent, float* resultCost,
            out int resultCount, int maxResult)
        {
            resultCount = 0;
            if (!this.navMesh->IsValidPolyRef(startRef) || !Detour.IsFinite(centerPos) || radius < 0 || !math.isfinite(radius) || maxResult < 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            this.nodePool->Clear();
            this.openList->Clear();

            var startNode = this.nodePool->GetNode(startRef);
            startNode->pos = centerPos;
            startNode->ParentIndex = 0;
            startNode->cost = 0;
            startNode->total = 0;
            startNode->id = startRef;
            startNode->Flags = DtNodeFlags.DT_NODE_OPEN;
            this.openList->Push(startNode);

            var status = DtStatus.Success;
            var n = 0;
            var radiusSqr = radius * radius;

            while (!this.openList->Empty)
            {
                var bestNode = this.openList->Pop();
                bestNode->Flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode->Flags |= DtNodeFlags.DT_NODE_CLOSED;

                var bestRef = bestNode->id;
                this.navMesh->GetTileAndPolyByRefUnsafe(bestRef, out var bestTile, out var bestPoly);

                DtPolyRef parentRef = 0;
                DtMeshTile* parentTile = null;
                DtPoly* parentPoly = null;
                if (bestNode->ParentIndex != 0)
                {
                    parentRef = this.nodePool->GetNodeAtIdx(bestNode->ParentIndex)->id;
                    if (parentRef != 0)
                    {
                        this.navMesh->GetTileAndPolyByRefUnsafe(parentRef, out parentTile, out parentPoly);
                    }
                }

                if (n < maxResult)
                {
                    if (resultRef != null)
                    {
                        resultRef[n] = bestRef;
                    }

                    if (resultParent != null)
                    {
                        resultParent[n] = parentRef;
                    }

                    if (resultCost != null)
                    {
                        resultCost[n] = bestNode->total;
                    }

                    n++;
                }
                else
                {
                    status |= DtStatus.BufferTooSmall;
                }

                for (var i = bestPoly->firstLink; i != Detour.DTNullLink; i = bestTile->links[i].next)
                {
                    var link = &bestTile->links[i];
                    var neighbourRef = link->polyRef;
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    this.navMesh->GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);
                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    if (!Detour.StatusSucceed(this.GetPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, out var va,
                        out var vb)))
                    {
                        continue;
                    }

                    if (Detour.DistancePtSegSqr2D(centerPos, va, vb, out _) > radiusSqr)
                    {
                        continue;
                    }

                    var neighbourNode = this.nodePool->GetNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        status |= DtStatus.OutOfNodes;
                        continue;
                    }

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    if (neighbourNode->Flags == 0)
                    {
                        neighbourNode->pos = math.lerp(va, vb, 0.5f);
                    }

                    var cost = filter.GetCost(bestNode->pos, neighbourNode->pos, parentRef, parentTile, parentPoly, bestRef, bestTile, bestPoly, neighbourRef,
                        neighbourTile, neighbourPoly);

                    var total = bestNode->total + cost;

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode->total)
                    {
                        continue;
                    }

                    neighbourNode->id = neighbourRef;
                    neighbourNode->ParentIndex = this.nodePool->GetNodeIdx(bestNode);
                    neighbourNode->total = total;

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        this.openList->Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode->Flags = DtNodeFlags.DT_NODE_OPEN;
                        this.openList->Push(neighbourNode);
                    }
                }
            }

            resultCount = n;
            return status;
        }

        /// <summary>
        /// Finds the polygons along the navigation graph that touch the specified convex polygon.
        /// </summary>
        /// <param name="startRef">The reference id of the polygon where the search starts.</param>
        /// <param name="verts">The vertices describing the convex polygon in counter-clockwise order. [(x, y, z) * <paramref name="nverts"/>].</param>
        /// <param name="nverts">The number of vertices in <paramref name="verts"/>.</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="resultRef">Receives the reference ids of the polygons touched by the search polygon. [opt].</param>
        /// <param name="resultParent">Receives the parent polygon for each result (zero if none). [opt].</param>
        /// <param name="resultCost">Receives the search cost from the polygon centroid to each result. [opt].</param>
        /// <param name="resultCount">Receives the number of polygons returned.</param>
        /// <param name="maxResult">The maximum number of polygons the result arrays can hold.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>The results are ordered from lowest to highest traversal cost. If the result buffers are too small they
        /// are filled to capacity.</para>
        /// <para>The search operates in 2D and projects the polygons onto the XZ plane. The centroid of
        /// <paramref name="verts"/> is used as the start position for cost calculations.</para>
        /// </remarks>
        public DtStatus FindPolysAroundShape(
            DtPolyRef startRef, float3* verts, int nverts, ref DtQueryFilter filter, DtPolyRef* resultRef, DtPolyRef* resultParent, float* resultCost,
            out int resultCount, int maxResult)
        {
            resultCount = 0;
            if (!this.navMesh->IsValidPolyRef(startRef) || verts == null || nverts < 3 || maxResult < 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            this.nodePool->Clear();
            this.openList->Clear();

            var centerPos = float3.zero;
            for (var i = 0; i < nverts; ++i)
            {
                centerPos += verts[i];
            }

            centerPos /= nverts;

            var startNode = this.nodePool->GetNode(startRef);
            startNode->pos = centerPos;
            startNode->ParentIndex = 0;
            startNode->cost = 0;
            startNode->total = 0;
            startNode->id = startRef;
            startNode->Flags = DtNodeFlags.DT_NODE_OPEN;
            this.openList->Push(startNode);

            var status = DtStatus.Success;
            var n = 0;

            while (!this.openList->Empty)
            {
                var bestNode = this.openList->Pop();
                bestNode->Flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode->Flags |= DtNodeFlags.DT_NODE_CLOSED;

                var bestRef = bestNode->id;
                this.navMesh->GetTileAndPolyByRefUnsafe(bestRef, out var bestTile, out var bestPoly);

                DtPolyRef parentRef = 0;
                DtMeshTile* parentTile = null;
                DtPoly* parentPoly = null;
                if (bestNode->ParentIndex != 0)
                {
                    parentRef = this.nodePool->GetNodeAtIdx(bestNode->ParentIndex)->id;
                    if (parentRef != 0)
                    {
                        this.navMesh->GetTileAndPolyByRefUnsafe(parentRef, out parentTile, out parentPoly);
                    }
                }

                if (n < maxResult)
                {
                    if (resultRef != null)
                    {
                        resultRef[n] = bestRef;
                    }

                    if (resultParent != null)
                    {
                        resultParent[n] = parentRef;
                    }

                    if (resultCost != null)
                    {
                        resultCost[n] = bestNode->total;
                    }

                    n++;
                }
                else
                {
                    status |= DtStatus.BufferTooSmall;
                }

                for (var i = bestPoly->firstLink; i != Detour.DTNullLink; i = bestTile->links[i].next)
                {
                    var link = &bestTile->links[i];
                    var neighbourRef = link->polyRef;
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    this.navMesh->GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);
                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    if (!Detour.StatusSucceed(this.GetPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, out var va,
                        out var vb)))
                    {
                        continue;
                    }

                    if (!Detour.IntersectSegmentPoly2D(va, vb, verts, nverts, out var tmin, out var tmax, out _, out _))
                    {
                        continue;
                    }

                    if (tmin > 1.0f || tmax < 0.0f)
                    {
                        continue;
                    }

                    var neighbourNode = this.nodePool->GetNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        status |= DtStatus.OutOfNodes;
                        continue;
                    }

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    if (neighbourNode->Flags == 0)
                    {
                        neighbourNode->pos = math.lerp(va, vb, 0.5f);
                    }

                    var cost = filter.GetCost(bestNode->pos, neighbourNode->pos, parentRef, parentTile, parentPoly, bestRef, bestTile, bestPoly, neighbourRef,
                        neighbourTile, neighbourPoly);

                    var total = bestNode->total + cost;

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode->total)
                    {
                        continue;
                    }

                    neighbourNode->id = neighbourRef;
                    neighbourNode->ParentIndex = this.nodePool->GetNodeIdx(bestNode);
                    neighbourNode->total = total;

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        this.openList->Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode->Flags = DtNodeFlags.DT_NODE_OPEN;
                        this.openList->Push(neighbourNode);
                    }
                }
            }

            resultCount = n;
            return status;
        }

        /// <summary>
        /// Gets a path from the explored nodes produced by the most recent Dijkstra-style search.
        /// </summary>
        /// <param name="endRef">The reference id of the end polygon.</param>
        /// <param name="path">Receives the polygon references representing the path (start to end).</param>
        /// <param name="pathCount">Receives the number of polygons written to <paramref name="path"/>.</param>
        /// <param name="maxPath">The capacity of the <paramref name="path"/> array. [Limit: &gt;= 0].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Returns <see cref="DtStatus.Failure"/> | <see cref="DtStatus.InvalidParam"/> if any parameter is
        /// invalid or if <paramref name="endRef"/> was not explored by the previous search.</para>
        /// <para>Returns <see cref="DtStatus.Success"/> | <see cref="DtStatus.BufferTooSmall"/> when the path does
        /// not fit in <paramref name="path"/>; the buffer is filled with the partial path. Otherwise returns
        /// <see cref="DtStatus.Success"/>.</para>
        /// </remarks>
        public DtStatus GetPathFromDijkstraSearch(DtPolyRef endRef, DtPolyRef* path, out int pathCount, int maxPath)
        {
            pathCount = 0;
            if (!this.navMesh->IsValidPolyRef(endRef) || path == null || maxPath < 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var endNodes = stackalloc DtNode*[1];
            if (this.nodePool->FindNodes(endRef, endNodes, 1) != 1 || (((*endNodes)->Flags & DtNodeFlags.DT_NODE_CLOSED) == 0))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            return this.GetPathToNode(endNodes[0], path, out pathCount, maxPath);
        }

        /// <summary>
        /// Finds the distance from the specified position to the nearest polygon wall.
        /// </summary>
        /// <param name="startRef">The reference id of the polygon containing <paramref name="centerPos"/>.</param>
        /// <param name="centerPos">The center of the search circle. [(x, y, z)].</param>
        /// <param name="maxRadius">The radius of the search circle.</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="hitDist">Receives the distance to the nearest wall from <paramref name="centerPos"/>.</param>
        /// <param name="hitPos">Receives the nearest point on the wall that was hit. [(x, y, z)].</param>
        /// <param name="hitNormal">Receives the normalized ray formed from the wall point to the source point. [(x, y, z)].</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>If no wall is found within the search radius, <paramref name="hitDist"/> equals <paramref name="maxRadius"/>
        /// and the values of <paramref name="hitPos"/> and <paramref name="hitNormal"/> are undefined.</para>
        /// <para>The normal becomes unreliable when the reported distance is extremely small.</para>
        /// </remarks>
        public DtStatus FindDistanceToWall(
            DtPolyRef startRef, in float3 centerPos, float maxRadius, ref DtQueryFilter filter, out float hitDist, out float3 hitPos, out float3 hitNormal)
        {
            hitDist = 0;
            hitPos = default;
            hitNormal = default;

            if (!this.navMesh->IsValidPolyRef(startRef) || !Detour.IsFinite(centerPos) || maxRadius < 0 || !math.isfinite(maxRadius))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            this.nodePool->Clear();
            this.openList->Clear();

            var startNode = this.nodePool->GetNode(startRef);
            startNode->pos = centerPos;
            startNode->ParentIndex = 0;
            startNode->cost = 0;
            startNode->total = 0;
            startNode->id = startRef;
            startNode->Flags = DtNodeFlags.DT_NODE_OPEN;
            this.openList->Push(startNode);

            var radiusSqr = maxRadius * maxRadius;
            var status = DtStatus.Success;

            while (!this.openList->Empty)
            {
                var bestNode = this.openList->Pop();
                bestNode->Flags &= ~DtNodeFlags.DT_NODE_OPEN;
                bestNode->Flags |= DtNodeFlags.DT_NODE_CLOSED;

                var bestRef = bestNode->id;
                this.navMesh->GetTileAndPolyByRefUnsafe(bestRef, out var bestTile, out var bestPoly);

                // Hit test walls
                for (int i = 0, j = bestPoly->vertCount - 1; i < bestPoly->vertCount; j = i++)
                {
                    var solid = true;
                    if ((bestPoly->neis[j] & Detour.DTExtLink) != 0)
                    {
                        for (var k = bestPoly->firstLink; k != Detour.DTNullLink; k = bestTile->links[k].next)
                        {
                            var link = &bestTile->links[k];
                            if (link->edge == j)
                            {
                                if (link->polyRef != 0)
                                {
                                    this.navMesh->GetTileAndPolyByRefUnsafe(link->polyRef, out var neiTile, out var neiPoly);
                                    if (filter.PassFilter(link->polyRef, neiTile, neiPoly))
                                    {
                                        solid = false;
                                    }
                                }

                                break;
                            }
                        }
                    }
                    else if (bestPoly->neis[j] != 0)
                    {
                        var idx = (uint)(bestPoly->neis[j] - 1);
                        var polyRef = this.navMesh->GetPolyRefBase(bestTile) | idx;
                        if (filter.PassFilter(polyRef, bestTile, &bestTile->polys[idx]))
                        {
                            solid = false;
                        }
                    }

                    if (!solid)
                    {
                        continue;
                    }

                    var vj = bestTile->verts[bestPoly->verts[j]];
                    var vi = bestTile->verts[bestPoly->verts[i]];
                    var distSqr = Detour.DistancePtSegSqr2D(centerPos, vj, vi, out var tseg);

                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    radiusSqr = distSqr;
                    hitPos = math.lerp(vj, vi, tseg);
                }

                DtPolyRef parentRef = 0;
                if (bestNode->ParentIndex != 0)
                {
                    parentRef = this.nodePool->GetNodeAtIdx(bestNode->ParentIndex)->id;
                }

                for (var i = bestPoly->firstLink; i != Detour.DTNullLink; i = bestTile->links[i].next)
                {
                    var link = &bestTile->links[i];
                    var neighbourRef = link->polyRef;
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                    {
                        continue;
                    }

                    this.navMesh->GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);
                    if (neighbourPoly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
                    {
                        continue;
                    }

                    var va = bestTile->verts[bestPoly->verts[link->edge]];
                    var vb = bestTile->verts[bestPoly->verts[(link->edge + 1) % bestPoly->vertCount]];
                    if (Detour.DistancePtSegSqr2D(centerPos, va, vb, out _) > radiusSqr)
                    {
                        continue;
                    }

                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    var neighbourNode = this.nodePool->GetNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        status |= DtStatus.OutOfNodes;
                        continue;
                    }

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    if (neighbourNode->Flags == 0)
                    {
                        this.GetEdgeMidPoint(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, out neighbourNode->pos);
                    }

                    var total = bestNode->total + math.distance(bestNode->pos, neighbourNode->pos);

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode->total)
                    {
                        continue;
                    }

                    neighbourNode->id = neighbourRef;
                    neighbourNode->ParentIndex = this.nodePool->GetNodeIdx(bestNode);
                    neighbourNode->total = total;

                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        this.openList->Modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode->Flags |= DtNodeFlags.DT_NODE_OPEN;
                        this.openList->Push(neighbourNode);
                    }
                }
            }

            hitNormal = math.normalize(centerPos - hitPos);
            hitDist = math.sqrt(radiusSqr);

            return status;
        }

        /// <summary>
        /// Finds the non-overlapping navigation polygons in the local neighbourhood around the center position.
        /// </summary>
        /// <param name="startRef">The reference id of the polygon where the search starts.</param>
        /// <param name="centerPos">The center of the query circle. [(x, y, z)].</param>
        /// <param name="radius">The radius of the query circle.</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="resultRef">Receives the reference ids of the polygons touched by the circle.</param>
        /// <param name="resultParent">Receives the parent polygon for each result (zero if none). [opt].</param>
        /// <param name="resultCount">Receives the number of polygons found.</param>
        /// <param name="maxResult">The maximum number of polygons the result arrays can hold.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>Optimized for small search radii and a small number of results. Candidate polygons are found by
        /// traversing the navigation graph beginning at <paramref name="startRef"/>.</para>
        /// <para>The search uses a 2D circle in the XZ plane. If the result buffers are too small they are filled to
        /// capacity.</para>
        /// </remarks>
        public DtStatus FindLocalNeighbourhood(
            DtPolyRef startRef, in float3 centerPos, float radius, ref DtQueryFilter filter, DtPolyRef* resultRef, DtPolyRef* resultParent, out int resultCount,
            int maxResult)
        {
            resultCount = 0;

            if (!this.navMesh->IsValidPolyRef(startRef) || !Detour.IsFinite(centerPos) || radius < 0 || !math.isfinite(radius) || maxResult < 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            const int maxStack = 48;
            var stack = stackalloc DtNode*[maxStack];
            var nstack = 0;

            this.tinyNodePool->Clear();

            var startNode = this.tinyNodePool->GetNode(startRef);
            startNode->ParentIndex = 0;
            startNode->id = startRef;
            startNode->Flags = DtNodeFlags.DT_NODE_CLOSED;
            stack[nstack++] = startNode;

            var radiusSqr = radius * radius;

            var pa = stackalloc float3[Detour.DTVertsPerPolygon];
            var pb = stackalloc float3[Detour.DTVertsPerPolygon];

            var status = DtStatus.Success;

            var n = 0;
            if (n < maxResult)
            {
                resultRef[n] = startNode->id;
                if (resultParent != null)
                {
                    resultParent[n] = 0;
                }

                ++n;
            }
            else
            {
                status |= DtStatus.BufferTooSmall;
            }

            while (nstack > 0)
            {
                // Pop front
                var curNode = stack[0];
                for (var i = 0; i < nstack - 1; ++i)
                {
                    stack[i] = stack[i + 1];
                }

                nstack--;

                // Get poly and tile
                var curRef = curNode->id;
                this.navMesh->GetTileAndPolyByRefUnsafe(curRef, out var curTile, out var curPoly);

                for (var i = curPoly->firstLink; i != Detour.DTNullLink; i = curTile->links[i].next)
                {
                    var link = &curTile->links[i];
                    var neighbourRef = link->polyRef;

                    // Skip invalid neighbours
                    if (neighbourRef == 0)
                    {
                        continue;
                    }

                    // Skip if cannot allocate more nodes
                    var neighbourNode = this.tinyNodePool->GetNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        continue;
                    }

                    // Skip visited
                    if ((neighbourNode->Flags & DtNodeFlags.DT_NODE_CLOSED) != 0)
                    {
                        continue;
                    }

                    // Expand to neighbour
                    this.navMesh->GetTileAndPolyByRefUnsafe(neighbourRef, out var neighbourTile, out var neighbourPoly);

                    // Skip off-mesh connections
                    if (neighbourPoly->GetPolyType() == DtPolyTypes.PolytypeOffMeshConnection)
                    {
                        continue;
                    }

                    // Do not advance if the polygon is excluded by the filter
                    if (!filter.PassFilter(neighbourRef, neighbourTile, neighbourPoly))
                    {
                        continue;
                    }

                    // Find edge and calc distance to the edge
                    if (!Detour.StatusSucceed(
                        this.GetPortalPoints(curRef, curPoly, curTile, neighbourRef, neighbourPoly, neighbourTile, out var va, out var vb)))
                    {
                        continue;
                    }

                    // If the circle is not touching the next polygon, skip it
                    var distSqr = Detour.DistancePtSegSqr2D(centerPos, va, vb, out _);
                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    // Mark node visited, this is done before the overlap test so that
                    // we will not visit the poly again if the test fails
                    neighbourNode->Flags |= DtNodeFlags.DT_NODE_CLOSED;
                    neighbourNode->ParentIndex = this.tinyNodePool->GetNodeIdx(curNode);

                    // Check that the polygon does not collide with existing polygons

                    // Collect vertices of the neighbour poly
                    var npa = neighbourPoly->vertCount;
                    for (var k = 0; k < npa; ++k)
                    {
                        pa[k] = neighbourTile->verts[neighbourPoly->verts[k]];
                    }

                    var overlap = false;
                    for (var j = 0; j < n; ++j)
                    {
                        var pastRef = resultRef[j];

                        // Connected polys do not overlap
                        var connected = false;
                        for (var k = curPoly->firstLink; k != Detour.DTNullLink; k = curTile->links[k].next)
                        {
                            if (curTile->links[k].polyRef == pastRef)
                            {
                                connected = true;
                                break;
                            }
                        }

                        if (connected)
                        {
                            continue;
                        }

                        // Potentially overlapping
                        this.navMesh->GetTileAndPolyByRefUnsafe(pastRef, out var pastTile, out var pastPoly);

                        // Get vertices and test overlap
                        var npb = pastPoly->vertCount;
                        for (var k = 0; k < npb; ++k)
                        {
                            pb[k] = pastTile->verts[pastPoly->verts[k]];
                        }

                        if (Detour.OverlapPolyPoly2D(pa, npa, pb, npb))
                        {
                            overlap = true;
                            break;
                        }
                    }

                    if (overlap)
                    {
                        continue;
                    }

                    // This poly is fine, store and advance to the poly
                    if (n < maxResult)
                    {
                        resultRef[n] = neighbourRef;
                        if (resultParent != null)
                        {
                            resultParent[n] = curRef;
                        }

                        ++n;
                    }
                    else
                    {
                        status |= DtStatus.BufferTooSmall;
                    }

                    if (nstack < maxStack)
                    {
                        stack[nstack++] = neighbourNode;
                    }
                }
            }

            resultCount = n;
            return status;
        }

        /// <summary>
        /// Returns the segments for the specified polygon, optionally including portals.
        /// </summary>
        /// <param name="polyRef">The reference id of the polygon.</param>
        /// <param name="filter">The polygon filter to apply to the query.</param>
        /// <param name="segmentVerts">Receives the segment endpoints. [(ax, ay, az, bx, by, bz) * <paramref name="segmentCount"/>].</param>
        /// <param name="segmentRefs">Receives the reference id of each segment's neighbour polygon, or zero if the segment is a wall. [opt].</param>
        /// <param name="segmentCount">Receives the number of segments returned.</param>
        /// <param name="maxSegments">The maximum number of segments the result arrays can hold.</param>
        /// <returns>The status flags for the query.</returns>
        /// <remarks>
        /// <para>If <paramref name="segmentRefs"/> is provided, both wall segments and portal segments are returned. If it
        /// is <see langword="null"/>, only wall segments are reported.</para>
        /// <para>If the result buffers are too small they are filled to capacity.</para>
        /// </remarks>
        public DtStatus GetPolyWallSegments(
            DtPolyRef polyRef, ref DtQueryFilter filter, float3* segmentVerts, DtPolyRef* segmentRefs, out int segmentCount, int maxSegments)
        {
            segmentCount = 0;

            var status = this.navMesh->GetTileAndPolyByRef(polyRef, out var tile, out var poly);
            if (Detour.StatusFailed(status))
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            if (segmentVerts == null || maxSegments < 0)
            {
                return DtStatus.Failure | DtStatus.InvalidParam;
            }

            var n = 0;
            const int maxIntervals = 16;
            var intervals = stackalloc DtSegInterval[maxIntervals];
            var storePortals = segmentRefs != null;

            var resultStatus = DtStatus.Success;

            for (int i = 0, j = poly->vertCount - 1; i < poly->vertCount; j = i++)
            {
                var intervalCount = 0;

                if ((poly->neis[j] & Detour.DTExtLink) != 0)
                {
                    // Tile border.
                    for (var k = poly->firstLink; k != Detour.DTNullLink; k = tile->links[k].next)
                    {
                        var link = &tile->links[k];
                        if (link->edge != j)
                        {
                            continue;
                        }

                        if (link->polyRef != 0)
                        {
                            this.navMesh->GetTileAndPolyByRefUnsafe(link->polyRef, out var neighbourTile, out var neighbourPoly);
                            if (filter.PassFilter(link->polyRef, neighbourTile, neighbourPoly))
                            {
                                InsertInterval(intervals, ref intervalCount, maxIntervals, link->bmin, link->bmax, link->polyRef);
                            }
                        }
                    }

                    InsertInterval(intervals, ref intervalCount, maxIntervals, -1, 0, 0);
                    InsertInterval(intervals, ref intervalCount, maxIntervals, 255, 256, 0);

                    var vj = tile->verts[poly->verts[j]];
                    var vi = tile->verts[poly->verts[i]];

                    for (var k = 1; k < intervalCount; ++k)
                    {
                        var current = intervals[k];
                        var previous = intervals[k - 1];

                        if (storePortals && current.PolyRef != 0)
                        {
                            var tmin = current.TMin / 255.0f;
                            var tmax = current.TMax / 255.0f;

                            if (n < maxSegments)
                            {
                                var segment = segmentVerts + (n * 2);
                                segment[0] = math.lerp(vj, vi, tmin);
                                segment[1] = math.lerp(vj, vi, tmax);

                                if (segmentRefs != null)
                                {
                                    segmentRefs[n] = current.PolyRef;
                                }

                                ++n;
                            }
                            else
                            {
                                resultStatus |= DtStatus.BufferTooSmall;
                            }
                        }

                        var imin = (int)previous.TMax;
                        var imax = (int)current.TMin;
                        if (imin == imax)
                        {
                            continue;
                        }

                        var tminWall = imin / 255.0f;
                        var tmaxWall = imax / 255.0f;

                        if (n < maxSegments)
                        {
                            var segment = segmentVerts + (n * 2);
                            segment[0] = math.lerp(vj, vi, tminWall);
                            segment[1] = math.lerp(vj, vi, tmaxWall);

                            if (segmentRefs != null)
                            {
                                segmentRefs[n] = 0;
                            }

                            ++n;
                        }
                        else
                        {
                            resultStatus |= DtStatus.BufferTooSmall;
                        }
                    }

                    continue;
                }

                // Internal edge.
                DtPolyRef neighbourRef = 0;
                if (poly->neis[j] != 0)
                {
                    var idx = (uint)(poly->neis[j] - 1);
                    neighbourRef = this.navMesh->GetPolyRefBase(tile) | idx;

                    if (!filter.PassFilter(neighbourRef, tile, &tile->polys[idx]))
                    {
                        neighbourRef = 0;
                    }
                }

                // If the edge leads to another polygon and portals are not stored, skip.
                if (neighbourRef != 0 && !storePortals)
                {
                    continue;
                }

                if (n < maxSegments)
                {
                    var segment = segmentVerts + (n * 2);
                    segment[0] = tile->verts[poly->verts[j]];
                    segment[1] = tile->verts[poly->verts[i]];

                    if (segmentRefs != null)
                    {
                        segmentRefs[n] = neighbourRef;
                    }

                    ++n;
                }
                else
                {
                    resultStatus |= DtStatus.BufferTooSmall;
                }
            }

            segmentCount = n;
            return resultStatus;
        }

        private static void InsertInterval(DtSegInterval* intervals, ref int count, int maxIntervals, short tmin, short tmax, DtPolyRef polyRef)
        {
            if (count + 1 > maxIntervals)
            {
                return;
            }

            var idx = 0;
            while (idx < count)
            {
                if (tmax <= intervals[idx].TMin)
                {
                    break;
                }

                ++idx;
            }

            for (var i = count; i > idx; --i)
            {
                intervals[i] = intervals[i - 1];
            }

            intervals[idx].PolyRef = polyRef;
            intervals[idx].TMin = tmin;
            intervals[idx].TMax = tmax;
            ++count;
        }

        private static float GetStraightPathAreaEpsilon(in float3 left, in float3 right)
        {
            // Intentional divergence from upstream zero-threshold funnel tests:
            // a portal-scaled epsilon suppresses near-collinear corner churn in live movement.
            var portalLenSq = math.lengthsq(right - left);
            return (portalLenSq * 1e-4f) + 1e-6f;
        }

        private struct DtSegInterval
        {
            public DtPolyRef PolyRef;
            public short TMin;
            public short TMax;
        }

        private struct DtQueryData
        {
            public DtStatus Status;
            public DtNode* LastBestNode;
            public float LastBestNodeCost;
            public DtPolyRef StartRef;
            public DtPolyRef EndRef;
            public float3 StartPos;
            public float3 EndPos;
            public DtQueryFilter* Filter;
            public DtFindPathOptions Options;
            public float RaycastLimitSqr;
        }

    }
}
