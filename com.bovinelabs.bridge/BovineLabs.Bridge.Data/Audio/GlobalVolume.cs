// <copyright file="GlobalVolume.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using BovineLabs.Core;
    using Unity.Entities;

    /// <summary>Global volume multiplier applied to audio sources.</summary>
    [ChangeFilterTracking]
    public struct GlobalVolume : IComponentData
    {
        public float Volume;
    }
}
#endif
