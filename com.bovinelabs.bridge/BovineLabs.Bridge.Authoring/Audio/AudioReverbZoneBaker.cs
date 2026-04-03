// <copyright file="AudioReverbZoneBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Entities;
    using UnityEngine;

    public class AudioReverbZoneBaker : Baker<AudioReverbZone>
    {
        /// <inheritdoc />
        public override void Bake(AudioReverbZone authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Renderable);

            this.AddComponent(entity, new AudioReverbZoneData
            {
                MinDistance = authoring.minDistance,
                MaxDistance = authoring.maxDistance,
                ReverbPreset = authoring.reverbPreset,
                Room = authoring.room,
                RoomHF = authoring.roomHF,
                RoomLF = authoring.roomLF,
                DecayTime = authoring.decayTime,
                DecayHFRatio = authoring.decayHFRatio,
                Reflections = authoring.reflections,
                ReflectionsDelay = authoring.reflectionsDelay,
                Reverb = authoring.reverb,
                ReverbDelay = authoring.reverbDelay,
                HFReference = authoring.HFReference,
                LFReference = authoring.LFReference,
                Diffusion = authoring.diffusion,
                Density = authoring.density,
            });
        }
    }
}
