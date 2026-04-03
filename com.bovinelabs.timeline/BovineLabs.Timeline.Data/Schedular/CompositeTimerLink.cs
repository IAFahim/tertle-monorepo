// <copyright file="CompositeTimerLink.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data.Schedular
{
    using Unity.Entities;

    /// <summary>Buffer element linking to composite timers that depend on this timer.</summary>
    [InternalBufferCapacity(0)]
    public struct CompositeTimerLink : IBufferElementData
    {
        /// <summary>The composite timer entity that is linked to this timer.</summary>
        public Entity Value;
    }
}
