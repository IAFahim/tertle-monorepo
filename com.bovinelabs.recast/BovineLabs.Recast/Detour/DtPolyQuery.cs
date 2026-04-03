// <copyright file="DtPolyQuery.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using Unity.Mathematics;

    /// <summary>
    /// Provides custom polygon query behavior.
    /// Used by dtNavMeshQuery::queryPolygons.
    /// </summary>
    public unsafe interface IDtPolyQuery
    {
        /// <summary>
        /// Called for each batch of unique polygons touched by the search area in dtNavMeshQuery::queryPolygons.
        /// This can be called multiple times for a single query.
        /// </summary>
        /// <param name="tile">The tile containing the polygons.</param>
        /// <param name="refs">Array of polygon references.</param>
        /// <param name="count">Number of polygons in this batch.</param>
        void Process(DtMeshTile* tile, DtPolyRef* refs, int count);
    }

    /// <summary> A simple polygon query that collects polygon references into an array. </summary>
    public unsafe struct DtCollectPolysQuery : IDtPolyQuery
    {
        private DtPolyRef* polys;
        private readonly int maxPolys;
        private int numCollected;
        private bool overflow;

        public DtCollectPolysQuery(DtPolyRef* polys, int maxPolys)
        {
            this.polys = polys;
            this.maxPolys = maxPolys;
            this.numCollected = 0;
            this.overflow = false;
        }

        public int NumCollected => this.numCollected;

        public bool Overflowed => this.overflow;

        /// <inheritdoc/>
        public void Process(DtMeshTile* tile, DtPolyRef* refs, int count)
        {
            var numLeft = this.maxPolys - this.numCollected;
            var toCopy = count;
            if (toCopy > numLeft)
            {
                this.overflow = true;
                toCopy = numLeft;
            }

            for (var i = 0; i < toCopy; i++)
            {
                this.polys[this.numCollected + i] = refs[i];
            }

            this.numCollected += toCopy;
        }
    }

    /// <summary> A polygon query that finds the nearest polygon to a center point. </summary>
    public unsafe struct DtFindNearestPolyQuery : IDtPolyQuery
    {
        private readonly DtNavMeshQuery query;
        private readonly float3 center;
        private float nearestDistanceSqr;
        private DtPolyRef nearestRef;
        private float3 nearestPoint;
        private bool overPoly;

        public DtFindNearestPolyQuery(DtNavMeshQuery query, in float3 center)
        {
            this.query = query;
            this.center = center;
            this.nearestDistanceSqr = float.MaxValue;
            this.nearestRef = 0;
            this.nearestPoint = float3.zero;
            this.overPoly = false;
        }

        public DtPolyRef NearestRef => this.nearestRef;

        public float3 NearestPoint => this.nearestPoint;

        public bool IsOverPoly => this.overPoly;

        /// <inheritdoc/>
        public void Process(DtMeshTile* tile, DtPolyRef* refs, int count)
        {
            for (var i = 0; i < count; ++i)
            {
                var polyRef = refs[i];
                this.query.ClosestPointOnPoly(polyRef, this.center, out var closestPtPoly, out var posOverPoly);

                // If a point is directly over a polygon and closer than climb height, favor that instead of straight line nearest point.
                var diff = this.center - closestPtPoly;
                float d;
                if (posOverPoly)
                {
                    d = math.abs(diff.y) - tile->header->walkableClimb;
                    d = d > 0 ? d * d : 0;
                }
                else
                {
                    d = math.lengthsq(diff);
                }

                if (d < this.nearestDistanceSqr)
                {
                    this.nearestPoint = closestPtPoly;
                    this.nearestDistanceSqr = d;
                    this.nearestRef = polyRef;
                    this.overPoly = posOverPoly;
                }
            }
        }
    }
}