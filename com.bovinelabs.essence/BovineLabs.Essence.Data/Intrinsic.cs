// <copyright file="Intrinsic.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using BovineLabs.Core.Iterators;
    using JetBrains.Annotations;
    using Unity.Entities;

    /// <summary>
    /// Intrinsics are properties of an entity that only go up and down, never modified.
    /// Examples include Health, Experience, Reputation etc.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct Intrinsic : IDynamicHashMap<IntrinsicKey, int>
    {
        /// <inheritdoc/>
        [UsedImplicitly]
        byte IDynamicHashMap<IntrinsicKey, int>.Value { get; }
    }
}
