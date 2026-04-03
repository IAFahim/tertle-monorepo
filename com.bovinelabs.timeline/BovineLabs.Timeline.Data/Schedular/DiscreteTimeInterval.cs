// <copyright file="DiscreteTimeInterval.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data.Schedular
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Unity.IntegerTime;

    /// <summary>
    /// Represents a closed interval between two <see cref="DiscreteTime"/> values.
    /// </summary>
    public struct DiscreteTimeInterval : IEquatable<DiscreteTimeInterval>, IFormattable
    {
        /// <summary>The largest representable interval.</summary>
        /// <remarks>The duration may overflow and should not be used for arithmetic.</remarks>
        public static readonly DiscreteTimeInterval MaxRange = new(DiscreteTime.MinValue, DiscreteTime.MaxValue);

        /// <summary>The inclusive start of the interval.</summary>
        public DiscreteTime Start;

        /// <summary>The inclusive end of the interval.</summary>
        public DiscreteTime End;

        /// <summary>Initializes a new instance of the <see cref="DiscreteTimeInterval"/> struct.</summary>
        /// <param name="time0">One endpoint of the interval.</param>
        /// <param name="time1">The other endpoint of the interval.</param>
        public DiscreteTimeInterval(DiscreteTime time0, DiscreteTime time1)
        {
            this.Start = time0.Min(time1);
            this.End = time0.Max(time1);
        }

        /// <summary>Gets the duration of the interval.</summary>
        /// <value>The duration between <see cref="Start"/> and <see cref="End"/>.</value>
        /// <remarks>
        /// The duration is capped at <see cref="DiscreteTime.MaxValue"/>, so very large intervals may not satisfy
        /// end = start + duration.
        /// </remarks>
        public DiscreteTime Duration
        {
            get
            {
                // overflow
                if (unchecked(this.End.Value - this.Start.Value) < 0)
                {
                    return DiscreteTime.MaxValue;
                }

                return DiscreteTime.FromTicks(this.End.Value - this.Start.Value);
            }
        }

        /// <summary>Gets the duration of the interval as an unsigned long.</summary>
        /// <value>The unsigned tick difference between <see cref="End"/> and <see cref="Start"/>.</value>
        /// <remarks>Use this when the duration may exceed <see cref="DiscreteTime.MaxValue"/>.</remarks>
        public ulong DurationAsTick => (ulong)this.End.Value - (ulong)this.Start.Value;

        /// <summary>Returns true if the time is inside the interval.</summary>
        /// <param name="t">The time to test.</param>
        /// <returns>True if the time is within the interval; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(DiscreteTime t)
        {
            return t >= this.Start && t <= this.End;
        }

        /// <summary>Returns true if this interval overlaps another.</summary>
        /// <param name="other">The other interval to test.</param>
        /// <returns>True if the intervals overlap; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Overlaps(DiscreteTimeInterval other)
        {
            return this.Start <= other.End && other.Start <= this.End;
        }

        /// <summary>Clamps a time to the interval.</summary>
        /// <param name="time">The time to clamp.</param>
        /// <returns>The clamped time.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly DiscreteTime Clamp(DiscreteTime time)
        {
            return this.End.Min(this.Start.Max(time));
        }

        /// <summary>Returns true if the same range is represented.</summary>
        /// <param name="other">The interval to compare.</param>
        /// <returns>True if the intervals are equal; otherwise, false.</returns>
        public bool Equals(DiscreteTimeInterval other)
        {
            return this.Start == other.Start && this.End == other.End;
        }

        /// <summary>Returns a hash code for the interval.</summary>
        /// <returns>A hash code for the interval.</returns>
        public override int GetHashCode()
        {
            return (this.Start.GetHashCode() * 397) ^ this.End.GetHashCode();
        }

        /// <summary>Returns a string representation of the interval.</summary>
        /// <returns>A string representation of the interval.</returns>
        public override string ToString()
        {
            return $"{nameof(DiscreteTimeInterval)}({this.Start}, {this.End})";
        }

        /// <summary>Returns a string representation of the interval using a specified format and culture.</summary>
        /// <param name="format">The format string to use.</param>
        /// <param name="formatProvider">The provider to use for formatting.</param>
        /// <returns>A string representation of the interval.</returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            var builder = new StringBuilder(64);
            builder.Append(nameof(DiscreteTimeInterval));
            builder.Append('(');
            builder.Append(this.Start.ToString(format, formatProvider));
            builder.Append(", ");
            builder.Append(this.End.ToString(format, formatProvider));
            builder.Append(')');
            return builder.ToString();
        }
    }
}
