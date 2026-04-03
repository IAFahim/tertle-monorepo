// <copyright file="LocalTime.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;
    using Unity.IntegerTime;

    /// <summary> The local time of the clip entity. </summary>
    public struct LocalTime : IComponentData
    {
        /// <summary> The current local time value. </summary>
        public DiscreteTime Value;
    }
}
