// <copyright file="ClockData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data.Schedular
{
    using Unity.Entities;
    using Unity.IntegerTime;

    /// <summary>
    /// Component data representing the clock values required to update a timer.
    /// This must be paired with a single clock type. This information is written first in the ClockUpdateSystem
    /// then used in the TimerUpdateSystem to update the timer.
    /// </summary>
    public struct ClockData : IComponentData
    {
        /// <summary>The delta time provided by the clock.</summary>
        public DiscreteTime DeltaTime;

        /// <summary>The scale provided by the clock.</summary>
        public double Scale;
    }
}
