// <copyright file="LightUniversalBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP

namespace BovineLabs.Bridge.Authoring.Light
{
    using BovineLabs.Bridge.Data.Light;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    public class LightUniversalBaker : Baker<UniversalAdditionalLightData>
    {
        /// <inheritdoc />
        public override void Bake(UniversalAdditionalLightData authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.None);
            this.AddComponent(entity, new LightUniversalData
            {
                UsePipelineSettings = authoring.usePipelineSettings,
                SoftShadowQuality = authoring.softShadowQuality,
                RenderingLayers = authoring.renderingLayers,
                CookieSize = authoring.lightCookieSize,
                CookieOffset = authoring.lightCookieOffset,
            });
        }
    }
}

#endif
