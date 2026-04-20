// <copyright file="MusicState.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    public struct MusicState : IComponentData
    {
        public int ActiveTrackId;
        public int TargetTrackId;
        public byte ActiveSlot;
        public float FadeTimer;
        public float FadeDuration;
        public float ActiveBaseVolume;
        public float TargetBaseVolume;
        public float ActiveFadeStartVolume;
        public float TargetFadeStartVolume;
        public bool IsFading;
    }
}
#endif
