// <copyright file="Detour.Math.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;

    /// <summary>Math utilities for Detour using Unity.Mathematics.</summary>
    public static unsafe partial class Detour
    {
        private const float EqualThresholdSqr = 1.0f / 16384.0f * (1.0f / 16384.0f);

        /// <summary>Calculates the signed area of a triangle in 2D (XZ plane).</summary>
        /// <param name="a">The first vertex of the triangle.</param>
        /// <param name="b">The second vertex of the triangle.</param>
        /// <param name="c">The third vertex of the triangle.</param>
        /// <returns>The signed area on the XZ plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float TriArea2D(in float3 a, in float3 b, in float3 c)
        {
            var ab = b.xz - a.xz;
            var ac = c.xz - a.xz;
            return (ac.x * ab.y) - (ab.x * ac.y);
        }

        /// <summary>Checks if two points are approximately equal.</summary>
        /// <param name="p0">The first point.</param>
        /// <param name="p1">The second point.</param>
        /// <returns>True if the distance between the points is below the configured threshold.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Equal(in float3 p0, in float3 p1)
        {
            return math.distancesq(p0, p1) < EqualThresholdSqr;
        }

        /// <summary>Checks if all components of a vector are finite.</summary>
        /// <param name="v">The vector to test.</param>
        /// <returns>True if all components are finite; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsFinite(in float3 v)
        {
            return math.all(math.isfinite(v));
        }

        /// <summary>Checks if 2D components of a vector are finite.</summary>
        /// <param name="v">The vector to test.</param>
        /// <returns>True if the X and Z components are finite; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsFinite2D(in float3 v)
        {
            return math.isfinite(v.x) && math.isfinite(v.z);
        }

        /// <summary>Returns the squared distance from a point to a line segment in 2D.</summary>
        /// <param name="pt">The point to test.</param>
        /// <param name="p">The start of the segment.</param>
        /// <param name="q">The end of the segment.</param>
        /// <param name="t">The normalized distance along the segment of the closest point.</param>
        /// <returns>The squared distance from the point to the closest point on the segment.</returns>
        internal static float DistancePtSegSqr2D(in float3 pt, in float3 p, in float3 q, out float t)
        {
            var pq = q.xz - p.xz;
            var d = pt.xz - p.xz;
            var dot = math.dot(pq, d);
            var pqLenSqr = math.lengthsq(pq);

            if (pqLenSqr > 0)
            {
                t = dot / pqLenSqr;
            }
            else
            {
                t = 0;
            }

            t = math.clamp(t, 0, 1);
            var closest = p.xz + (t * pq);
            var diff = pt.xz - closest;
            return math.lengthsq(diff);
        }

        /// <summary>Tests if a point is inside a convex polygon in 2D.</summary>
        /// <param name="pt">The point to test.</param>
        /// <param name="verts">The polygon vertices.</param>
        /// <returns>True if the point lies inside the polygon; otherwise, false.</returns>
        internal static bool PointInPolygon(in float3 pt, ReadOnlySpan<float3> verts)
        {
            var c = false;
            for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
            {
                var vi = verts[i];
                var vj = verts[j];
                if (((vi.z > pt.z) != (vj.z > pt.z)) &&
                    (pt.x < ((vj.x - vi.x) * (pt.z - vi.z) / (vj.z - vi.z)) + vi.x))
                {
                    c = !c;
                }
            }

            return c;
        }

        /// <summary>Calculates distance to polygon edges and tests if point is inside.</summary>
        /// <param name="pt">The point to test.</param>
        /// <param name="verts">The polygon vertices.</param>
        /// <param name="nverts">The number of vertices.</param>
        /// <param name="ed">The buffer that receives squared edge distances for each vertex.</param>
        /// <param name="et">The buffer that receives the parametric position along each edge.</param>
        /// <returns>True if the point is inside the polygon; otherwise, false.</returns>
        internal static bool DistancePtPolyEdgesSqr(in float3 pt, float3* verts, int nverts, float* ed, float* et)
        {
            var c = false;
            for (int i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                var vi = verts[i];
                var vj = verts[j];
                if (((vi.z > pt.z) != (vj.z > pt.z)) &&
                    (pt.x < ((vj.x - vi.x) * (pt.z - vi.z) / (vj.z - vi.z)) + vi.x))
                {
                    c = !c;
                }

                ed[j] = DistancePtSegSqr2D(pt, vj, vi, out et[j]);
            }

            return c;
        }

        /// <summary>Tests intersection between a line segment and a polygon in 2D.</summary>
        /// <param name="p0">The start point of the segment.</param>
        /// <param name="p1">The end point of the segment.</param>
        /// <param name="verts">The polygon vertices.</param>
        /// <param name="nverts">The number of polygon vertices.</param>
        /// <param name="tmin">The normalized distance where the segment enters the polygon.</param>
        /// <param name="tmax">The normalized distance where the segment exits the polygon.</param>
        /// <param name="segMin">The edge index where the segment enters.</param>
        /// <param name="segMax">The edge index where the segment exits.</param>
        /// <returns>True if the segment intersects the polygon; otherwise, false.</returns>
        internal static bool IntersectSegmentPoly2D(in float3 p0, in float3 p1, float3* verts, int nverts,
                                                  out float tmin, out float tmax, out int segMin, out int segMax)
        {
            const float eps = 0.000001f;

            tmin = 0;
            tmax = 1;
            segMin = -1;
            segMax = -1;

            var dir = p1 - p0;

            for (int i = 0, j = nverts - 1; i < nverts; j = i++)
            {
                var edge = verts[i] - verts[j];
                var diff = p0 - verts[j];
                var n = Perp2D(edge, diff);
                var d = Perp2D(dir, edge);

                if (math.abs(d) < eps)
                {
                    // S is nearly parallel to this edge
                    if (n < 0)
                    {
                        return false;
                    }
                    else
                    {
                        continue;
                    }
                }

                var t = n / d;
                if (d < 0)
                {
                    // segment S is entering across this edge
                    if (t > tmin)
                    {
                        tmin = t;
                        segMin = j;

                        // S enters after leaving polygon
                        if (tmin > tmax)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // segment S is leaving across this edge
                    if (t < tmax)
                    {
                        tmax = t;
                        segMax = j;

                        // S leaves before entering polygon
                        if (tmax < tmin)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>Tests intersection between two line segments in 2D.</summary>
        /// <param name="ap">The start point of the first segment.</param>
        /// <param name="aq">The end point of the first segment.</param>
        /// <param name="bp">The start point of the second segment.</param>
        /// <param name="bq">The end point of the second segment.</param>
        /// <param name="s">The normalized intersection distance along the first segment.</param>
        /// <param name="t">The normalized intersection distance along the second segment.</param>
        /// <returns>True if the segments intersect; otherwise, false.</returns>
        internal static bool IntersectSegSeg2D(in float3 ap, in float3 aq, in float3 bp, in float3 bq, out float s, out float t)
        {
            var u = aq - ap;
            var v = bq - bp;
            var w = ap - bp;
            var d = Perp2D(u, v);

            if (math.abs(d) < 1e-6f)
            {
                s = t = 0;
                return false;
            }

            s = Perp2D(v, w) / d;
            t = Perp2D(u, w) / d;
            return true;
        }

        /// <summary>Tests if two polygons overlap in 2D.</summary>
        /// <param name="polya">The vertices of the first polygon.</param>
        /// <param name="npolya">The number of vertices in the first polygon.</param>
        /// <param name="polyb">The vertices of the second polygon.</param>
        /// <param name="npolyb">The number of vertices in the second polygon.</param>
        /// <returns>True if the polygons overlap; otherwise, false.</returns>
        internal static bool OverlapPolyPoly2D(float3* polya, int npolya, float3* polyb, int npolyb)
        {
            const float eps = 1e-4f;

            // Test separation along edges of polygon A
            for (int i = 0, j = npolya - 1; i < npolya; j = i++)
            {
                var va = polya[j];
                var vb = polya[i];
                var n = new float3(vb.z - va.z, 0, -(vb.x - va.x));

                ProjectPoly(n, polya, npolya, out var amin, out var amax);
                ProjectPoly(n, polyb, npolyb, out var bmin, out var bmax);

                if (!OverlapRange(amin, amax, bmin, bmax, eps))
                {
                    return false; // Found separating axis
                }
            }

            // Test separation along edges of polygon B
            for (int i = 0, j = npolyb - 1; i < npolyb; j = i++)
            {
                var va = polyb[j];
                var vb = polyb[i];
                var n = new float3(vb.z - va.z, 0, -(vb.x - va.x));

                ProjectPoly(n, polya, npolya, out var amin, out var amax);
                ProjectPoly(n, polyb, npolyb, out var bmin, out var bmax);

                if (!OverlapRange(amin, amax, bmin, bmax, eps))
                {
                    return false; // Found separating axis
                }
            }

            return true;
        }

        /// <summary>Generates a random point inside a convex polygon.</summary>
        /// <param name="pts">The polygon vertices.</param>
        /// <param name="npts">The number of polygon vertices.</param>
        /// <param name="areas">Scratch buffer that stores per-triangle areas.</param>
        /// <param name="s">A random value in the range [0, 1] used to select a triangle.</param>
        /// <param name="t">A random value in the range [0, 1] used to interpolate inside the triangle.</param>
        /// <returns>A uniformly distributed point inside the polygon.</returns>
        internal static float3 RandomPointInConvexPoly(float3* pts, int npts, float* areas, float s, float t)
        {
            // Calc triangle areas
            var areasum = 0.0f;
            for (var i = 2; i < npts; i++)
            {
                areas[i] = TriArea2D(pts[0], pts[i - 1], pts[i]);
                areasum += math.max(0.001f, areas[i]);
            }

            // Find sub triangle weighted by area
            var thr = s * areasum;
            var acc = 0.0f;
            var u = 1.0f;
            var tri = npts - 1;
            for (var i = 2; i < npts; i++)
            {
                var dacc = areas[i];
                if (thr >= acc && thr < (acc + dacc))
                {
                    u = (thr - acc) / dacc;
                    tri = i;
                    break;
                }

                acc += dacc;
            }

            var v = math.sqrt(t);

            var a = 1 - v;
            var b = (1 - u) * v;
            var c = u * v;
            var pa = pts[0];
            var pb = pts[tri - 1];
            var pc = pts[tri];

            return (a * pa) + (b * pb) + (c * pc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ProjectPoly(in float3 axis, float3* poly, int npoly, out float rmin, out float rmax)
        {
            rmin = rmax = Dot2D(axis, poly[0]);
            for (var i = 1; i < npoly; ++i)
            {
                var d = Dot2D(axis, poly[i]);
                rmin = math.min(rmin, d);
                rmax = math.max(rmax, d);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool OverlapRange(float amin, float amax, float bmin, float bmax, float eps)
        {
            return !(amin + eps > bmax || amax - eps < bmin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Perp2D(in float3 u, in float3 v)
        {
            return (u.z * v.x) - (u.x * v.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Dot2D(in float3 u, in float3 v)
        {
            return (u.x * v.x) + (u.z * v.z);
        }
    }
}
