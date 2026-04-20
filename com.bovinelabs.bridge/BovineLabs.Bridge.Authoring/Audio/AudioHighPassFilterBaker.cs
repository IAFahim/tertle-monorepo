// <copyright file="AudioHighPassFilterBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Entities;
    using UnityEngine;

    public class AudioHighPassFilterBaker : Baker<AudioHighPassFilter>
    {
        /// <inheritdoc />
        public override void Bake(AudioHighPassFilter authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Renderable);

            this.AddComponent(entity, new AudioHighPassFilterData
            {
                CutoffFrequency = authoring.cutoffFrequency,
                HighpassResonanceQ = authoring.highpassResonanceQ,
            });
        }
    }
}
#endif
