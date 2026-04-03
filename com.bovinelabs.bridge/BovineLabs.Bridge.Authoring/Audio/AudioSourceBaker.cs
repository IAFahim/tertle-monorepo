// <copyright file="AudioSourceBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Entities;
    using UnityEngine;

    [ForceBakingOnDisabledComponents]
    public class AudioSourceBaker : Baker<AudioSource>
    {
        /// <inheritdoc />
        public override void Bake(AudioSource authoring)
        {
            // If user drags playing audio source into an open subscene
            if (authoring.isPlaying)
            {
                authoring.Stop();
            }

            var entity = this.GetEntity(TransformUsageFlags.Renderable);

            // TODO this will determine if its pooled or not
            // Loop = authoring.loop,
            // Mute = authoring.mute,

            var components = new ComponentTypeSet(typeof(AudioSourceData), typeof(AudioSourceDataExtended), typeof(AudioSourceIndex), typeof(AudioSourceEnabled),
                typeof(AudioSourceEnabledPrevious));

            this.AddComponent(entity, components);

            this.SetComponent(entity, new AudioSourceData
            {
                Volume = authoring.volume,
                Pitch = authoring.pitch,
            });

            this.SetComponent(entity, new AudioSourceDataExtended
            {
                Clip = authoring.clip,
                PanStereo = authoring.panStereo,
                SpatialBlend = authoring.spatialBlend,
                MinDistance = authoring.minDistance,
                MaxDistance = authoring.maxDistance,
                DopplerLevel = authoring.dopplerLevel,
                Spread = authoring.spread,
                RolloffMode = authoring.rolloffMode,
                Priority = authoring.priority,
                ReverbZoneMix = authoring.reverbZoneMix,
            });

            this.SetComponent(entity, new AudioSourceIndex { PoolIndex = -1 });
            this.SetComponentEnabled<AudioSourceIndex>(entity, false);

            this.SetComponentEnabled<AudioSourceEnabled>(entity, this.IsActiveAndEnabled());
            this.SetComponentEnabled<AudioSourceEnabledPrevious>(entity, false);
        }
    }
}
