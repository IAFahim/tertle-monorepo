// <copyright file="InitializeStats.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Data.Core;
    using JetBrains.Annotations;
    using Unity.Entities;
    using Unity.NetCode;

    /// <summary>
    /// A dynamic hash map buffer for tracking stat initialization data by object IDs.
    /// </summary>
    [InternalBufferCapacity(0)]
    [GhostComponent(PrefabType=GhostPrefabType.Server)]
    public struct InitializeStats : IDynamicHashMap<ObjectId, InitializeStats.Data>
    {
        /// <inheritdoc/>
        [UsedImplicitly]
        byte IDynamicHashMap<ObjectId, Data>.Value { get; }

        /// <summary>
        /// Initialization data for stats including the source target.
        /// </summary>
        public struct Data
        {
            public Target Source;
        }
    }
}
