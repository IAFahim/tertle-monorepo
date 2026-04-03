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
    /// Which sides of the clip to apply extrapolation
    /// </summary>
    [Flags]
    public enum ExtrapolationPosition
    {
        None = 0,
        Pre = 1,
        Post = 2,
        Both = Pre | Post,
    }

    /// <summary> The transformation from the timer to the local clip space. </summary>
    public struct TimeTransform : IComponentData
    {
        /// <summary> The start time of the clip in the timeline. </summary>
        public DiscreteTime Start;

        /// <summary> The end time of the clip in the timeline. </summary>
        public DiscreteTime End;

        /// <summary> The local start time within the clip. </summary>
        public DiscreteTime ClipIn;

        /// <summary> The time scale multiplier for the clip. </summary>
        public double Scale;

        public static bool operator ==(TimeTransform options1, TimeTransform options2)
        {
            return options1.Equals(options2);
        }

        public static bool operator !=(TimeTransform options1, TimeTransform options2)
        {
            return !options1.Equals(options2);
        }

        public readonly DiscreteTime ToLocalTimeUnbound(DiscreteTime time)
        {
            return ((time - this.Start) * this.Scale) + this.ClipIn;
        }

        public readonly bool IsLocalTimeBounded(DiscreteTime time)
        {
            var length = (this.End - this.Start) * this.Scale;
            return time >= DiscreteTime.Zero && time <= length;
        }

        public override bool Equals(object? obj)
        {
            return obj is TimeTransform transform && this.Equals(transform);
        }

        public bool Equals(TimeTransform other)
        {
            return this.Start == other.Start && this.End == other.End && this.ClipIn == other.ClipIn && this.Scale == other.Scale;
        }

        public override int GetHashCode()
        {
            return math.rol(this.Start.GetHashCode(), 1) +
                math.rol(this.End.GetHashCode(), 7) +
                math.rol(this.ClipIn.GetHashCode(), 12) +
                math.rol(this.Scale.GetHashCode(), 18);
        }
    }
}
