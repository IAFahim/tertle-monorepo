// <copyright file="BridgeSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;

    [SettingsWorld("Client")]
    public class BridgeSettings : SettingsBase
    {
        [Header("Audio")]
        [SerializeField]
        [Min(1)]
        private int loopedAudioPoolSize = 32;

        [SerializeField]
        [Min(1)]
        private float maxListenDistance = 200;

        public AudioSourcePoolConfig AudioSourcePoolConfig => new()
        {
            LoopedAudioPoolSize = this.loopedAudioPoolSize,
            MaxListenDistanceSq = this.maxListenDistance * this.maxListenDistance,
        };

        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);
            baker.AddComponent(entity, this.AudioSourcePoolConfig);
        }
    }
}
