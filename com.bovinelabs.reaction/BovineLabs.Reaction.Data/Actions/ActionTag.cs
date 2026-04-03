// <copyright file="ActionTag.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Actions
{
    using BovineLabs.Reaction.Data.Core;
    using Unity.Entities;

    /// <summary>
    /// Buffer element defining tag component addition/removal actions on target entities using stable type hashes.
    /// </summary>
    [InternalBufferCapacity(1)]
    public struct ActionTag : IBufferElementData
    {
        // Stable hash type of the tag component
        public ulong Value;
        public Target Target;
    }
}
