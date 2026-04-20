// <copyright file="AudioReverbFilterBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Entities;
    using UnityEngine;

    public class AudioReverbFilterBaker : Baker<AudioReverbFilter>
    {
        /// <inheritdoc />
        public override void Bake(AudioReverbFilter authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Renderable);

            this.AddComponent(entity, new AudioReverbFilterData
            {
                ReverbPreset = authoring.reverbPreset,
                DryLevel = authoring.dryLevel,
                Room = authoring.room,
                RoomHF = authoring.roomHF,
                RoomLF = authoring.roomLF,
                DecayTime = authoring.decayTime,
                DecayHFRatio = authoring.decayHFRatio,
                ReflectionsLevel = authoring.reflectionsLevel,
                ReflectionsDelay = authoring.reflectionsDelay,
                ReverbLevel = authoring.reverbLevel,
                ReverbDelay = authoring.reverbDelay,
                HFReference = authoring.hfReference,
                LFReference = authoring.lfReference,
                Diffusion = authoring.diffusion,
                Density = authoring.density,
            });
        }
    }
}
#endif
