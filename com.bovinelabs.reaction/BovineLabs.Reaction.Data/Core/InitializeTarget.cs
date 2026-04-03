// <copyright file="InitializeTarget.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Core
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Entities;

    /// <summary>
    /// Dynamic hash map buffer for configuring target assignment during entity initialization.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct InitializeTarget : IDynamicHashMap<ObjectId, InitializeTarget.Data>
    {
        /// <inheritdoc />
        byte IDynamicHashMap<ObjectId, Data>.Value { get; }

        /// <summary>
        /// Configuration data specifying which target to assign to newly created entities.
        /// </summary>
        public struct Data
        {
            public Target Target;
        }
    }
}
