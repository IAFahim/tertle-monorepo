// <copyright file="ConditionMeta.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using Unity.Entities;

    /// <summary>
    /// Component storing blob asset reference to condition metadata for reaction entities.
    /// </summary>
    public struct ConditionMeta : IComponentData
    {
        public BlobAssetReference<ConditionMetaData> Value;
    }

    /// <summary>
    /// Blob data structure containing an array of condition configurations for reaction processing.
    /// </summary>
    public struct ConditionMetaData
    {
        public BlobArray<ConditionData> Conditions;
    }
}
