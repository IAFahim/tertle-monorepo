// <copyright file="AudioEchoFilterBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Entities;
    using UnityEngine;

    public class AudioEchoFilterBaker : Baker<AudioEchoFilter>
    {
        /// <inheritdoc />
        public override void Bake(AudioEchoFilter authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Renderable);

            this.AddComponent(entity, new AudioEchoFilterData
            {
                Delay = authoring.delay,
                DecayRatio = authoring.decayRatio,
                WetMix = authoring.wetMix,
                DryMix = authoring.dryMix,
            });
        }
    }
}
