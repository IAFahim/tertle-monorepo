// <copyright file="StatDefaults.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using Unity.Entities;
    using Unity.NetCode;

    /// <summary>
    /// Contains blob asset reference to default stat modifier configurations.
    /// </summary>
    [GhostComponent(PrefabType=GhostPrefabType.Server)]
    public struct StatDefaults : IComponentData
    {
        public BlobAssetReference<Data> Value;

        /// <summary>
        /// Default stat modifier data stored in a blob asset.
        /// </summary>
        public struct Data
        {
            public BlobArray<StatModifier> Default;
        }
    }
}
