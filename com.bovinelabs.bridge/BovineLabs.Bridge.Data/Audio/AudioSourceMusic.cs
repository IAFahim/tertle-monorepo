// <copyright file="AudioSourceMusic.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    /// <summary>Marks an audio entity as a dedicated music slot.</summary>
    [WriteGroup(typeof(GlobalVolume))]
    public struct AudioSourceMusic : IComponentData
    {
    }
}
#endif
