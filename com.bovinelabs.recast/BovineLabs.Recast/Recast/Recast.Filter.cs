// <copyright file="Recast.Filter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using Unity.Mathematics;

    /// <summary>
    /// Recast filtering functions for removing unwalkable spans from heightfields.
    /// These filters help clean up artifacts from conservative voxelization and enforce
    /// agent movement constraints.
    /// </summary>
    public static unsafe partial class Recast
    {
        private const int MaxHeightfieldHeight = 0xffff;

        /// <summary>
        /// Marks non-walkable spans as walkable if their maximum is within walkableClimb of the span below them.
        /// This removes small obstacles and rasterization artifacts that the agent would be able to walk over
        /// such as curbs. It also allows agents to move up terraced structures like stairs.
        /// </summary>
        /// <param name="walkableClimb">Maximum ledge height that is considered to still be traversable [Units: vx].</param>
        /// <param name="heightfield">A fully built heightfield (all spans have been added).</param>
        /// <remarks>
        /// Equivalent to rcFilterLowHangingWalkableObstacles() in C++.
        ///
        /// Warning: Will override the effect of FilterLedgeSpans. If both filters are used,
        /// call FilterLedgeSpans only after applying this filter.
        ///
        /// Obstacle spans are marked walkable if: obstacleSpan.smax - walkableSpan.smax &lt; walkableClimb.
        /// </remarks>
        public static void FilterLowHangingWalkableObstacles(int walkableClimb, RcHeightfield* heightfield)
        {
            var w = heightfield->Width;
            var h = heightfield->Height;

            for (var z = 0; z < h; z++)
            {
                for (var x = 0; x < w; x++)
                {
                    RcSpan* previousSpan = null;
                    var previousWasWalkable = false;
                    uint previousAreaID = RCNullArea;

                    // For each span in the column...
                    for (var span = heightfield->Spans[x + (z * w)]; span != null; span = span->Next)
                    {
                        var walkable = span->Area != RCNullArea;

                        // If current span is not walkable, but there is walkable span just below it and the height difference
                        // is small enough for the agent to walk over, mark the current span as walkable too.
                        if (!walkable && previousWasWalkable && (int)span->SMax - (int)previousSpan->SMax <= walkableClimb)
                        {
                            span->Area = previousAreaID;
                        }

                        // Copy the original walkable value regardless of whether we changed it.
                        // This prevents multiple consecutive non-walkable spans from being erroneously marked as walkable.
                        previousWasWalkable = walkable;
                        previousAreaID = span->Area;

                        previousSpan = span;
                    }
                }
            }
        }

        /// <summary>
        /// Marks spans that are ledges as not-walkable.
        /// A ledge is a span with one or more neighbors whose maximum is further away than walkableClimb
        /// from the current span's maximum. This method removes the impact of the overestimation of
        /// conservative voxelization so the resulting mesh will not have regions hanging in the air over ledges.
        /// </summary>
        /// <param name="walkableHeight">Minimum floor to 'ceiling' height that will still allow the floor area to be considered walkable [Limit: >= 3] [Units: vx].</param>
        /// <param name="walkableClimb">Maximum ledge height that is considered to still be traversable [Limit: >=0] [Units: vx].</param>
        /// <param name="heightfield">A fully built heightfield (all spans have been added).</param>
        /// <remarks>
        /// Equivalent to rcFilterLedgeSpans() in C++.
        ///
        /// A span is a ledge if: rcAbs(currentSpan.smax - neighborSpan.smax) > walkableClimb.
        /// </remarks>
        public static void FilterLedgeSpans(int walkableHeight, int walkableClimb, RcHeightfield* heightfield)
        {
            var w = heightfield->Width;
            var h = heightfield->Height;

            // Mark spans that are adjacent to a ledge as unwalkable
            for (var z = 0; z < h; z++)
            {
                for (var x = 0; x < w; x++)
                {
                    for (var span = heightfield->Spans[x + (z * w)]; span != null; span = span->Next)
                    {
                        // Skip non-walkable spans
                        if (span->Area == RCNullArea)
                        {
                            continue;
                        }

                        var floor = (int)span->SMax;
                        var ceiling = span->Next != null ? (int)span->Next->SMin : MaxHeightfieldHeight;

                        // The difference between this walkable area and the lowest neighbor walkable area.
                        // This is the difference between the current span and all neighbor spans that have
                        // enough space for an agent to move between, but not accounting at all for surface slope.
                        var lowestNeighborFloorDifference = MaxHeightfieldHeight;

                        // Min and max height of accessible neighbors
                        var lowestTraversableNeighborFloor = (int)span->SMax;
                        var highestTraversableNeighborFloor = (int)span->SMax;

                        // Check 4 cardinal directions
                        for (var direction = 0; direction < 4; direction++)
                        {
                            var neighborX = x + GetDirOffsetX(direction);
                            var neighborZ = z + GetDirOffsetY(direction);

                            // Skip neighbors which are out of bounds
                            if (neighborX < 0 || neighborZ < 0 || neighborX >= w || neighborZ >= h)
                            {
                                lowestNeighborFloorDifference = -walkableClimb - 1;
                                break;
                            }

                            var neighborSpan = heightfield->Spans[neighborX + (neighborZ * w)];

                            // The most we can step down to the neighbor is the walkableClimb distance.
                            // Start with the area under the neighbor span
                            var neighborCeiling = neighborSpan != null ? (int)neighborSpan->SMin : MaxHeightfieldHeight;

                            // Skip neighbor if the gap between the spans is too small
                            if (math.min(ceiling, neighborCeiling) - floor >= walkableHeight)
                            {
                                lowestNeighborFloorDifference = -walkableClimb - 1;
                                break;
                            }

                            // For each span in the neighboring column...
                            for (; neighborSpan != null; neighborSpan = neighborSpan->Next)
                            {
                                var neighborFloor = (int)neighborSpan->SMax;
                                neighborCeiling = neighborSpan->Next != null ? (int)neighborSpan->Next->SMin : MaxHeightfieldHeight;

                                // Only consider neighboring areas that have enough overlap to be potentially traversable
                                if (math.min(ceiling, neighborCeiling) - math.max(floor, neighborFloor) < walkableHeight)
                                {
                                    // No space to traverse between them
                                    continue;
                                }

                                var neighborFloorDifference = neighborFloor - floor;
                                lowestNeighborFloorDifference = math.min(lowestNeighborFloorDifference, neighborFloorDifference);

                                // Find min/max accessible neighbor height.
                                // Only consider neighbors that are at most walkableClimb away.
                                if (math.abs(neighborFloorDifference) <= walkableClimb)
                                {
                                    // There is space to move to the neighbor cell and the slope isn't too much
                                    lowestTraversableNeighborFloor = math.min(lowestTraversableNeighborFloor, neighborFloor);
                                    highestTraversableNeighborFloor = math.max(highestTraversableNeighborFloor, neighborFloor);
                                }
                                else if (neighborFloorDifference < -walkableClimb)
                                {
                                    // We already know this will be considered a ledge span so we can early-out
                                    break;
                                }
                            }
                        }

                        // The current span is close to a ledge if the magnitude of the drop to any neighbor span is greater than the walkableClimb distance.
                        // That is, there is a gap that is large enough to let an agent move between them, but the drop (surface slope) is too large to allow it.
                        // (If this is the case, then lowestNeighborFloorDifference will be negative, so compare against the negative walkableClimb as a means of checking
                        // the magnitude of the delta)
                        if (lowestNeighborFloorDifference < -walkableClimb)
                        {
                            span->Area = RCNullArea;
                        }

                        // If the difference between all neighbor floors is too large, this is a steep slope, so mark the span as an unwalkable ledge
                        else if (highestTraversableNeighborFloor - lowestTraversableNeighborFloor > walkableClimb)
                        {
                            span->Area = RCNullArea;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Marks walkable spans as not walkable if the clearance above the span is less than the specified walkableHeight.
        /// For this filter, the clearance above the span is the distance from the span's maximum to the minimum of the next higher span in the same column.
        /// If there is no higher span in the column, the clearance is computed as the distance from the top of the span to the maximum heightfield height.
        /// </summary>
        /// <param name="walkableHeight">Minimum floor to 'ceiling' height that will still allow the floor area to be considered walkable [Limit: >= 3] [Units: vx].</param>
        /// <param name="heightfield">A fully built heightfield (all spans have been added).</param>
        /// <remarks>Equivalent to rcFilterWalkableLowHeightSpans() in C++.</remarks>
        public static void FilterWalkableLowHeightSpans(int walkableHeight, RcHeightfield* heightfield)
        {
            var w = heightfield->Width;
            var h = heightfield->Height;

            // Remove walkable flag from spans which do not have enough
            // space above them for the agent to stand there
            for (var z = 0; z < h; z++)
            {
                for (var x = 0; x < w; x++)
                {
                    for (var span = heightfield->Spans[x + (z * w)]; span != null; span = span->Next)
                    {
                        var floor = (int)span->SMax;
                        var ceiling = span->Next != null ? (int)span->Next->SMin : MaxHeightfieldHeight;

                        if (ceiling - floor < walkableHeight)
                        {
                            span->Area = RCNullArea;
                        }
                    }
                }
            }
        }
    }
}