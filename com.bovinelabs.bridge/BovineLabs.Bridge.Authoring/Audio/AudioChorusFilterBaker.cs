// <copyright file="AudioChorusFilterBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Entities;
    using UnityEngine;

    public class AudioChorusFilterBaker : Baker<AudioChorusFilter>
    {
        /// <inheritdoc />
        public override void Bake(AudioChorusFilter authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Renderable);

            this.AddComponent(entity, new AudioChorusFilterData
            {
                DryMix = authoring.dryMix,
                WetMix1 = authoring.wetMix1,
                WetMix2 = authoring.wetMix2,
                WetMix3 = authoring.wetMix3,
                Delay = authoring.delay,
                Rate = authoring.rate,
                Depth = authoring.depth,
            });
        }
    }
}
