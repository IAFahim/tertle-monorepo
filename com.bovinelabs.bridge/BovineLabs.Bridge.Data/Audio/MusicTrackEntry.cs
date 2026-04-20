// <copyright file="MusicTrackEntry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;
    using UnityEngine;

    public struct MusicTrackEntry : IBufferElementData
    {
        public UnityObjectRef<AudioClip> Clip;
        public float BaseVolume;
        public float BlendOverrideSeconds;
    }
}
#endif
