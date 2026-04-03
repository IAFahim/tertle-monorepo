// <copyright file="EventSubscriber.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using BovineLabs.Reaction.Data.Core;
    using Unity.Entities;

    /// <summary>
    /// Buffer element linking entities to event-based condition subscriptions with operation and value data.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct EventSubscriber : IBufferElementData
    {
        public Entity Subscriber;
        public ushort Key;
        public byte ConditionType; // Event, Stat, Intrinsic etc
        public ConditionFeature Feature;
        public byte Index;
        public Equality Operation;
        public bool CustomComparison;
        public ValueIndex ValueIndex;
    }
}
