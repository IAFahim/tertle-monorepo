// <copyright file="DtQueryFilter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    /// <summary>
    /// Defines polygon filtering and traversal costs for navigation mesh query operations.
    ///
    /// At construction: All area costs default to 1.0. All flags are included and none are excluded.
    /// If a polygon has both an include and an exclude flag, it will be excluded.
    /// Setting the include flags to 0 will result in all polygons being excluded.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DtQueryFilter
    {
        public static readonly DtQueryFilter IncludeAll = CreateDefault();

        public fixed float areaCost[Detour.DTMaxAreas];  // Cost per area type
        public ushort includeFlags;                        // Flags for polygons that can be visited
        public ushort excludeFlags;                        // Flags for polygons that should not be visited

        /// <summary> Creates a default query filter with all area costs set to 1.0 and all flags included. </summary>
        /// <returns>A default query filter.</returns>
        public static DtQueryFilter CreateDefault()
        {
            var filter = new DtQueryFilter();
            filter.includeFlags = 0xffff;
            filter.excludeFlags = 0;

            for (var i = 0; i < Detour.DTMaxAreas; ++i)
            {
                filter.areaCost[i] = 1.0f;
            }

            return filter;
        }

        /// <summary>
        /// Returns true if the polygon can be visited (i.e. is traversable).
        /// </summary>
        /// <param name="polyRef">The reference id of the polygon to test.</param>
        /// <param name="tile">The tile containing the polygon.</param>
        /// <param name="poly">The polygon to test.</param>
        /// <returns>True if the polygon passes the filter.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PassFilter(DtPolyRef polyRef, DtMeshTile* tile, DtPoly* poly)
        {
            return (poly->flags & this.includeFlags) != 0 && (poly->flags & this.excludeFlags) == 0;
        }

        /// <summary>
        /// Returns cost to move from the beginning to the end of a line segment that is fully contained within a polygon.
        /// </summary>
        /// <param name="pa">The start position on the edge of the previous and current polygon.</param>
        /// <param name="pb">The end position on the edge of the current and next polygon.</param>
        /// <param name="prevRef">The reference id of the previous polygon.</param>
        /// <param name="prevTile">The tile containing the previous polygon.</param>
        /// <param name="prevPoly">The previous polygon.</param>
        /// <param name="curRef">The reference id of the current polygon.</param>
        /// <param name="curTile">The tile containing the current polygon.</param>
        /// <param name="curPoly">The current polygon.</param>
        /// <param name="nextRef">The reference id of the next polygon.</param>
        /// <param name="nextTile">The tile containing the next polygon.</param>
        /// <param name="nextPoly">The next polygon.</param>
        /// <returns>The cost of moving from pa to pb.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetCost(in float3 pa, in float3 pb,
                            DtPolyRef prevRef, DtMeshTile* prevTile, DtPoly* prevPoly,
                            DtPolyRef curRef, DtMeshTile* curTile, DtPoly* curPoly,
                            DtPolyRef nextRef, DtMeshTile* nextTile, DtPoly* nextPoly)
        {
            return math.distance(pa, pb) * this.areaCost[curPoly->GetArea()];
        }

        /// <summary>
        /// Returns the traversal cost of the area.
        /// </summary>
        /// <param name="i">The id of the area.</param>
        /// <returns>The traversal cost of the area.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetAreaCost(int i)
        {
            return this.areaCost[i];
        }

        /// <summary>
        /// Sets the traversal cost of the area.
        /// </summary>
        /// <param name="i">The id of the area.</param>
        /// <param name="cost">The new cost of traversing the area.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAreaCost(int i, float cost)
        {
            this.areaCost[i] = cost;
        }

        /// <summary>
        /// Returns the include flags for the filter.
        /// Any polygons that include one or more of these flags will be included in the operation.
        /// </summary>
        /// <returns>The include flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetIncludeFlags()
        {
            return this.includeFlags;
        }

        /// <summary>
        /// Sets the include flags for the filter.
        /// </summary>
        /// <param name="flags">The new flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIncludeFlags(ushort flags)
        {
            this.includeFlags = flags;
        }

        /// <summary>
        /// Returns the exclude flags for the filter.
        /// Any polygons that include one or more of these flags will be excluded from the operation.
        /// </summary>
        /// <returns>The exclude flags.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort GetExcludeFlags()
        {
            return this.excludeFlags;
        }

        /// <summary>
        /// Sets the exclude flags for the filter.
        /// </summary>
        /// <param name="flags">The new flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetExcludeFlags(ushort flags)
        {
            this.excludeFlags = flags;
        }
    }
}