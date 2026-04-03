// <copyright file="ClockSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data.Schedular
{
    using Unity.Entities;
    using Unity.IntegerTime;

    /// <summary>The update mode used by a clock.</summary>
    public enum ClockUpdateMode
    {
        /// <summary>Updates using game time (scaled).</summary>
        GameTime,

        /// <summary>Updates using unscaled game time.</summary>
        UnscaledGameTime,

        /// <summary>Updates using a constant delta time.</summary>
        Constant,
    }

    /// <summary>
    /// Settings that control how a timer's clock is updated.
    /// </summary>
    public struct ClockSettings : IComponentData
    {
        /// <summary>The update mode for this clock.</summary>
        public ClockUpdateMode UpdateMode;

        /// <summary>The delta time to use when in constant mode.</summary>
        public DiscreteTime DeltaTime;

        /// <summary>The time scale to use when in constant mode.</summary>
        public float TimeScale;

        /// <summary>Whether the clock runs in reverse.</summary>
        public bool Reverse;
    }
}
