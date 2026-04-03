// <copyright file="TimerDataLink.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data.Schedular
{
    using Unity.Entities;

    /// <summary>Buffer element linking to entities that have TimerData components referencing this timer.</summary>
    [InternalBufferCapacity(0)]
    public struct TimerDataLink : IBufferElementData
    {
        /// <summary>The entity with TimerData that references this timer.</summary>
        public Entity Value;
    }
}
