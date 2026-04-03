// <copyright file="AudioSourceIndex.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    /// <summary>
    /// Tracks which pooled AudioSource is assigned to this entity.
    /// Entities with this component use the pooling system instead of individual managed AudioSource components.
    /// </summary>
    public struct AudioSourceIndex : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Index into the pool of AudioSources. -1 means not currently assigned.
        /// </summary>
        public int PoolIndex;
    }
}
