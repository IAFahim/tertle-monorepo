// <copyright file="ActionCreate.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Actions
{
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Entities;

    /// <summary>
    /// Buffer element defining entity creation actions with target assignment and destruction settings.
    /// </summary>
    [InternalBufferCapacity(1)]
    public struct ActionCreate : IBufferElementData
    {
        public ObjectId Id;
        public Target Target;
        public bool DestroyOnDisabled;
    }
}
