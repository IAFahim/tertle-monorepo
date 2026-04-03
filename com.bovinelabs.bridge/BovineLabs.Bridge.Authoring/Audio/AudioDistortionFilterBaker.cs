// <copyright file="AudioDistortionFilterBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Entities;
    using UnityEngine;

    public class AudioDistortionFilterBaker : Baker<AudioDistortionFilter>
    {
        /// <inheritdoc />
        public override void Bake(AudioDistortionFilter authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Renderable);

            this.AddComponent(entity, new AudioDistortionFilterData
            {
                DistortionLevel = authoring.distortionLevel,
            });
        }
    }
}
