// <copyright file="ReactionEnableables.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Core
{
    using BovineLabs.Core.Iterators;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    internal struct ReactionEnableables : IDynamicHashSet<ulong>
    {
        byte IDynamicHashSet<ulong>.Value { get; }
    }
}
