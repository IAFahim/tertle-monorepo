// <copyright file="Stat.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Reaction.Data.Active;
    using JetBrains.Annotations;
    using Unity.Entities;

    /// <summary>
    /// A dynamic hash map buffer that stores stat values by their keys.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct Stat : IDynamicHashMap<StatKey, StatValue>
    {
        /// <inheritdoc/>
        [UsedImplicitly]
        byte IDynamicHashMap<StatKey, StatValue>.Value { get; }
    }
}
