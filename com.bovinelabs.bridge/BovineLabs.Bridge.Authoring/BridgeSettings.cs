// <copyright file="BridgeSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;
#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
    using BovineLabs.Bridge.Authoring.Audio;
    using BovineLabs.Bridge.Data.Audio;
#endif

    [SettingsWorld("Client")]
    [SettingsGroup("Bridge")]
    public class BridgeSettings : SettingsBase
    {
#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
        [Header("Audio")]
        [SerializeField]
        [Min(1)]
        private int loopedAudioPoolSize = 32;

        [SerializeField]
        [Min(1)]
        private int oneShotAudioPoolSize = 32;

        [Header("Music")]
        [SerializeField]
        private MusicTrackDefinition[] musicTracks = Array.Empty<MusicTrackDefinition>();

        [SerializeField]
        [Min(0f)]
        private float defaultMusicBlendSeconds = 2f;

        public AudioSourcePoolConfig AudioSourcePoolConfig => new()
        {
            LoopedAudioPoolSize = this.loopedAudioPoolSize,
            OneShotAudioPoolSize = this.oneShotAudioPoolSize,
        };
#endif

        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);
#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
            baker.AddComponent(entity, this.AudioSourcePoolConfig);

            baker.AddComponent(entity, new MusicBlendConfig { DefaultBlendSeconds = this.defaultMusicBlendSeconds });
            baker.AddComponent(entity, default(MusicSelection));
            baker.AddComponent(entity, new MusicState { ActiveSlot = 0 });

            var musicTracksBuffer = baker.AddBuffer<MusicTrackEntry>(entity);

            var validTracks = this.musicTracks.Where(t => t).ToArray();

            var count = (validTracks.Length > 0 ? validTracks.Max(t => t.Id) : 0) + 1;
            musicTracksBuffer.ResizeInitialized(count);

            foreach (var track in validTracks)
            {
                musicTracksBuffer[track.Id] = new MusicTrackEntry
                {
                    Clip = track.Clip,
                    BaseVolume = track.BaseVolume,
                    BlendOverrideSeconds = track.BlendOverrideSeconds,
                };
            }
#endif
        }
    }
}
