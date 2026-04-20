// <copyright file="AudioSourceIndex.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using BovineLabs.Core;
    using Unity.Entities;

    /// <summary>
    /// Tracks which pooled AudioSource is assigned to this entity.
    /// Entities with this component use the pooling system instead of individual managed AudioSource components.
    /// </summary>
    [ChangeFilterTracking]
    public struct AudioSourceIndex : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Index into the pool of AudioSources. -1 means not currently assigned.
        /// </summary>
        public int PoolIndex;
    }
}
#endif
