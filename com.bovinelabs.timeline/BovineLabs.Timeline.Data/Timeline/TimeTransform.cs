// <copyright file="TimeTransform.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using System;
    using Unity.Entities;
    using Unity.IntegerTime;
    using Unity.Mathematics;

    /// <summary>
    /// Specifies which sides of the clip to apply extrapolation.
    /// </summary>
    [Flags]
    public enum ExtrapolationPosition
    {
        /// <summary>No extrapolation.</summary>
        None = 0,

        /// <summary>Apply extrapolation before the clip range.</summary>
        Pre = 1,

        /// <summary>Apply extrapolation after the clip range.</summary>
        Post = 2,

        /// <summary>Apply extrapolation before and after the clip range.</summary>
        Both = Pre | Post,
    }

    /// <summary> The transformation from the timer to the local clip space. </summary>
    public struct TimeTransform : IComponentData
    {
        /// <summary>The start time of the clip in the timeline.</summary>
        public DiscreteTime Start;

        /// <summary>The end time of the clip in the timeline.</summary>
        public DiscreteTime End;

        /// <summary>The local start time within the clip.</summary>
        public DiscreteTime ClipIn;

        /// <summary>The time scale multiplier for the clip.</summary>
        public double Scale;

        /// <summary>Determines whether two transforms are equal.</summary>
        /// <param name="options1">The first transform to compare.</param>
        /// <param name="options2">The second transform to compare.</param>
        /// <returns>True if the transforms are equal; otherwise, false.</returns>
        public static bool operator ==(TimeTransform options1, TimeTransform options2)
        {
            return options1.Equals(options2);
        }

        /// <summary>Determines whether two transforms are not equal.</summary>
        /// <param name="options1">The first transform to compare.</param>
        /// <param name="options2">The second transform to compare.</param>
        /// <returns>True if the transforms are not equal; otherwise, false.</returns>
        public static bool operator !=(TimeTransform options1, TimeTransform options2)
        {
            return !options1.Equals(options2);
        }

        /// <summary>Converts a timeline time to unbounded local clip time.</summary>
        /// <param name="time">The timeline time.</param>
        /// <returns>The local clip time, without clamping to the clip range.</returns>
        public readonly DiscreteTime ToLocalTimeUnbound(DiscreteTime time)
        {
            return ((time - this.Start) * this.Scale) + this.ClipIn;
        }

        /// <summary>Checks whether a local time is within the clip's range.</summary>
        /// <param name="time">The local time to test.</param>
        /// <returns>True if the time is within the clip range; otherwise, false.</returns>
        public readonly bool IsLocalTimeBounded(DiscreteTime time)
        {
            var length = (this.End - this.Start) * this.Scale;
            return time >= DiscreteTime.Zero && time <= length;
        }

        /// <summary>Returns true if the transform is equal to the given object.</summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the object is an equal transform; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is TimeTransform transform && this.Equals(transform);
        }

        /// <summary>Returns true if the same transform is represented.</summary>
        /// <param name="other">The transform to compare.</param>
        /// <returns>True if the transforms are equal; otherwise, false.</returns>
        public bool Equals(TimeTransform other)
        {
            return this.Start == other.Start && this.End == other.End && this.ClipIn == other.ClipIn && this.Scale == other.Scale;
        }

        /// <summary>Returns a hash code for the transform.</summary>
        /// <returns>A hash code for the transform.</returns>
        public override int GetHashCode()
        {
            return math.rol(this.Start.GetHashCode(), 1) +
                math.rol(this.End.GetHashCode(), 7) +
                math.rol(this.ClipIn.GetHashCode(), 12) +
                math.rol(this.Scale.GetHashCode(), 18);
        }
    }
}
