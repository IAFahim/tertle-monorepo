// <copyright file="ConditionEvent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using BovineLabs.Core.Iterators;
    using JetBrains.Annotations;
    using Unity.Entities;

    /// <summary>
    /// Dynamic hash map buffer storing event-based condition data with condition keys mapped to values.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct ConditionEvent : IDynamicHashMap<ConditionKey, int>
    {
        [UsedImplicitly]
        byte IDynamicHashMap<ConditionKey, int>.Value { get; }
    }
}
