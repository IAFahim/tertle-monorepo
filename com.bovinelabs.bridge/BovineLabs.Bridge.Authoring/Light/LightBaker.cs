// <copyright file="LightBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring.Light
{
    using BovineLabs.Bridge.Data.Light;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [ForceBakingOnDisabledComponents]
    public class LightBaker : Baker<Light>
    {
        /// <inheritdoc />
        public override void Bake(Light authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Renderable);
            this.AddComponent(entity, new ComponentTypeSet(typeof(LightData), typeof(LightDataExtended), typeof(LightEnabled)));

            this.SetComponent(entity, new LightData
            {
                Color = authoring.color,
                Intensity = authoring.intensity,
                ColorTemperature = authoring.colorTemperature,
            });

            this.SetComponent(entity, new LightDataExtended
            {
                Type = authoring.type,
                Range = authoring.range,
                SpotAngle = authoring.spotAngle,
                InnerSpotAngle = authoring.innerSpotAngle,
                CookieSize = new float2(authoring.cookieSize2D.x, authoring.cookieSize2D.y),
                Cookie = authoring.cookie,
                BounceIntensity = authoring.bounceIntensity,
                Shadows = authoring.shadows,
                ShadowStrength = authoring.shadowStrength,
                ShadowBias = authoring.shadowBias,
                ShadowNormalBias = authoring.shadowNormalBias,
                ShadowNearPlane = authoring.shadowNearPlane,
                RenderMode = authoring.renderMode,
                CullingMask = authoring.cullingMask,
                RenderingLayerMask = authoring.renderingLayerMask,
            });

            this.SetComponentEnabled<LightEnabled>(entity, this.IsActiveAndEnabled());
        }
    }
}
