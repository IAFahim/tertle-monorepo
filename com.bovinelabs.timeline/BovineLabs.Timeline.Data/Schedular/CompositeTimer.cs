// <copyright file="CompositeTimer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data.Schedular
{
    using Unity.Entities;
    using Unity.IntegerTime;

    /// <summary>
    /// Composite timer is a timer whose time is a transformation from a non-composite timer.
    /// </summary>
    public struct CompositeTimer : IComponentData
    {
        /// <summary>The timer this is a transformation of.</summary>
        public Entity SourceTimer;

        /// <summary>Time offset from the source timer.</summary>
        public DiscreteTime Offset;

        /// <summary>The scale offset from the source timer.</summary>
        public double Scale;

        /// <summary>The active range of the source timer.</summary>
        public ActiveRange ActiveRange;
    }
}
