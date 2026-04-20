// <copyright file="MusicTrackDefinition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Authoring;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.PropertyDrawers;
    using UnityEngine;

    [AutoRef(nameof(BridgeSettings), "musicTracks", nameof(MusicTrackDefinition), "Audio")]
    public sealed class MusicTrackDefinition : ScriptableObject, IUID
    {
        [InspectorReadOnly]
        [SerializeField]
        private int id;

        [SerializeField]
        private AudioClip clip;

        [SerializeField]
        [Min(0f)]
        private float baseVolume = 1f;

        [SerializeField]
        [Min(0f)]
        private float blendOverrideSeconds;

        public int Id => this.id;

        public AudioClip Clip => this.clip;

        public float BaseVolume => this.baseVolume;

        public float BlendOverrideSeconds => this.blendOverrideSeconds;

        int IUID.ID
        {
            get => this.id;
            set => this.id = value;
        }
    }
}
#endif
