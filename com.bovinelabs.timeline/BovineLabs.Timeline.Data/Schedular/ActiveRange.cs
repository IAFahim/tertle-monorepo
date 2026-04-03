// <copyright file="ActiveRange.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data.Schedular
{
    using Unity.Entities;
    using Unity.IntegerTime;

    /// <summary>
    /// Component that defines the timer range where an entity will have an active tag.
    /// </summary>
    public struct ActiveRange : IComponentData
    {
        /// <summary>The inclusive start of the range.</summary>
        public DiscreteTime Start;

        /// <summary>The exclusive end of the range.</summary>
        public DiscreteTime End;

        /// <summary>A range object representing the full representable range.</summary>
        public static readonly ActiveRange CompleteRange = new()
        {
            Start = DiscreteTime.MinValue,
            End = DiscreteTime.MaxValue,
        };
    }

    /// <summary>Extension methods for <see cref="ActiveRange"/>.</summary>
    public static class ActiveRangeExtensions
    {
        /// <summary>Returns true if the range is valid.</summary>
        /// <param name="range">The range to validate.</param>
        /// <returns>True if <see cref="ActiveRange.Start"/> is less than <see cref="ActiveRange.End"/>; otherwise, false.</returns>
        public static bool IsValid(this ActiveRange range)
        {
            return range.Start < range.End;
        }

        /// <summary>Returns true if the ranges overlap.</summary>
        /// <param name="range">The first range.</param>
        /// <param name="other">The other range to test.</param>
        /// <returns>True if the ranges overlap; otherwise, false.</returns>
        public static bool Overlaps(this ActiveRange range, ActiveRange other)
        {
            return range.IsValid() && other.IsValid() && range.Start < other.End && other.Start < range.End;
        }

        /// <summary>Returns whether the time is within the range.</summary>
        /// <param name="range">The range to test.</param>
        /// <param name="t">The time to test.</param>
        /// <returns>True if the time is within the range; otherwise, false.</returns>
        public static bool Contains(this ActiveRange range, DiscreteTime t)
        {
            return range.Start <= t && range.End > t;
        }

        /// <summary>Returns whether the given range is completely contained within this range.</summary>
        /// <param name="range">The containing range.</param>
        /// <param name="other">The range to test.</param>
        /// <returns>True if <paramref name="other"/> is fully contained; otherwise, false.</returns>
        public static bool Contains(this ActiveRange range, ActiveRange other)
        {
            return range.Start <= other.Start && range.End >= other.End;
        }

        /// <summary>Gets the length of the active range.</summary>
        /// <param name="range">The range to measure.</param>
        /// <returns>The length of the range.</returns>
        public static DiscreteTime Length(this ActiveRange range)
        {
            return range.End - range.Start;
        }
    }
}
