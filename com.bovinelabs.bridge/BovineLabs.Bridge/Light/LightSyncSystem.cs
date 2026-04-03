// <copyright file="LightSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Light
{
    using System.Diagnostics;
    using BovineLabs.Bridge.Data.Light;
    using Unity.Entities;
    using UnityEngine;
    using LightData = BovineLabs.Bridge.Data.Light.LightData;
#if UNITY_URP
    using UnityEngine.Rendering.Universal;
#endif
#if UNITY_HDRP
    using UnityEngine.Rendering.HighDefinition;
#endif

    [UpdateInGroup(typeof(BridgeSystemGroup))]
    public partial class LightSyncSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            this.SyncEnabled();
            this.SyncLightData();
            this.SyncUniversalData();
            this.SyncHdData();
        }

        private void SyncEnabled()
        {
            foreach (var (enabled, light) in SystemAPI
                .Query<EnabledRefRO<LightEnabled>, SystemAPI.ManagedAPI.UnityEngineComponent<Light>>())
            {
                light.Value.enabled = enabled.ValueRO;
            }
        }

        private void SyncLightData()
        {
            foreach (var (data, light) in SystemAPI
                .Query<RefRO<LightData>, SystemAPI.ManagedAPI.UnityEngineComponent<Light>>()
                .WithChangeFilter<LightData>())
            {
                light.Value.color = data.ValueRO.Color;
                light.Value.intensity = data.ValueRO.Intensity;
                light.Value.colorTemperature = data.ValueRO.ColorTemperature;
            }

            foreach (var (data, light) in SystemAPI
                .Query<RefRO<LightDataExtended>, SystemAPI.ManagedAPI.UnityEngineComponent<Light>>()
                .WithChangeFilter<LightDataExtended>())
            {
                light.Value.type = data.ValueRO.Type;

                light.Value.range = data.ValueRO.Range;
                light.Value.spotAngle = data.ValueRO.SpotAngle;
                light.Value.innerSpotAngle = data.ValueRO.InnerSpotAngle;
                light.Value.cookieSize2D = data.ValueRO.CookieSize;
                light.Value.cookie = data.ValueRO.Cookie.Value;

                light.Value.bounceIntensity = data.ValueRO.BounceIntensity;

                light.Value.shadows = data.ValueRO.Shadows;
                light.Value.shadowStrength = data.ValueRO.ShadowStrength;
                light.Value.shadowBias = data.ValueRO.ShadowBias;
                light.Value.shadowNormalBias = data.ValueRO.ShadowNormalBias;
                light.Value.shadowNearPlane = data.ValueRO.ShadowNearPlane;

                light.Value.renderMode = data.ValueRO.RenderMode;
                light.Value.cullingMask = data.ValueRO.CullingMask;
                light.Value.renderingLayerMask = data.ValueRO.RenderingLayerMask;
            }
        }

        [Conditional("UNITY_URP")]
        private void SyncUniversalData()
        {
#if UNITY_URP
            foreach (var (data, additional) in SystemAPI
                .Query<RefRO<LightUniversalData>, SystemAPI.ManagedAPI.UnityEngineComponent<UniversalAdditionalLightData>>()
                .WithChangeFilter<LightUniversalData>())
            {
                var universal = additional.Value;
                var universalData = data.ValueRO;

                universal.usePipelineSettings = universalData.UsePipelineSettings;
                universal.softShadowQuality = universalData.SoftShadowQuality;

                universal.renderingLayers = universalData.RenderingLayers;
                universal.shadowRenderingLayers = universalData.RenderingLayers;

                universal.lightCookieSize = universalData.CookieSize;
                universal.lightCookieOffset = universalData.CookieOffset;
            }
#endif
        }

        [Conditional("UNITY_HDRP")]
        private void SyncHdData()
        {
#if UNITY_HDRP
            foreach (var (data, additional) in SystemAPI
                .Query<RefRO<LightHdData>, SystemAPI.ManagedAPI.UnityEngineComponent<HDAdditionalLightData>>()
                .WithChangeFilter<LightHdData>())
            {
                additional.Value.lightDimmer = data.ValueRO.LightDimmer;
                additional.Value.volumetricDimmer = data.ValueRO.VolumetricDimmer;
                additional.Value.affectDiffuse = data.ValueRO.AffectDiffuse;
                additional.Value.affectSpecular = data.ValueRO.AffectSpecular;
                additional.Value.fadeDistance = data.ValueRO.FadeDistance;
                additional.Value.shadowDimmer = data.ValueRO.ShadowDimmer;
                additional.Value.shadowFadeDistance = data.ValueRO.ShadowFadeDistance;
                additional.Value.affectsVolumetric = data.ValueRO.AffectsVolumetric;
            }
#endif
        }
    }
}
