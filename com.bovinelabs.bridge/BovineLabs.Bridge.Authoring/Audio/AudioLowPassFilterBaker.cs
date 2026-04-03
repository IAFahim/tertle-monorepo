// <copyright file="AudioLowPassFilterBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Entities;
    using UnityEngine;

    public class AudioLowPassFilterBaker : Baker<AudioLowPassFilter>
    {
        /// <inheritdoc />
        public override void Bake(AudioLowPassFilter authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Renderable);

            this.AddComponent(entity, new AudioLowPassFilterData
            {
                CutoffFrequency = authoring.cutoffFrequency,
                LowpassResonanceQ = authoring.lowpassResonanceQ,
            });
        }
    }
}
