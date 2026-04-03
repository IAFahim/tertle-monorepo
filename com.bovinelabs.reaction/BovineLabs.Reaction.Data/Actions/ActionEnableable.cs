// <copyright file="ActionEnableable.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Actions
{
    using BovineLabs.Reaction.Data.Core;
    using Unity.Entities;

    /// <summary>
    /// Buffer element defining component enable/disable actions on target entities using stable type hashes.
    /// </summary>
    [InternalBufferCapacity(1)]
    public struct ActionEnableable : IBufferElementData
    {
        // Stable type has of the enableable component
        public ulong Value;
        public Target Target;
    }
}
