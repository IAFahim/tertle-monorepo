// <copyright file="ActionCreated.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Actions
{
    using Unity.Entities;

    /// <summary>
    /// Buffer element tracking entities created from ActionCreate actions that may need to be destroyed when reactions are disabled.
    /// </summary>
    [InternalBufferCapacity(1)]
    public struct ActionCreated : IBufferElementData
    {
        public Entity Value;
    }
}
