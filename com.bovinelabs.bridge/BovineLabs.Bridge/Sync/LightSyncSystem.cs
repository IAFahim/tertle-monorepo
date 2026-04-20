// <copyright file="LightSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Lighting;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Rendering;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using LightData = BovineLabs.Bridge.Data.Lighting.LightData;
#if UNITY_HDRP
    using UnityEngine.Rendering.HighDefinition;
#endif

    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    public partial struct LightSyncSystem : ISystem
    {
        static unsafe LightSyncSystem()
        {
            Burst.LightEnabled.Data = new BurstTrampoline(&LightEnabledChangedPacked);
            Burst.LightBakingOutputData.Data = new BurstTrampoline(&LightBakingOutputDataChangedPacked);
            Burst.LightData.Data = new BurstTrampoline(&LightDataChangedPacked);
            Burst.LightDataExtended.Data = new BurstTrampoline(&LightDataExtendedChangedPacked);
#if UNITY_URP
            Burst.LightUniversalData.Data = new BurstTrampoline(&LightUniversalDataChangedPacked);
#endif
#if UNITY_HDRP
            Burst.LightHdData.Data = new BurstTrampoline(&LightHdDataChangedPacked);
#endif
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (enabled, bridge) in SystemAPI
                .Query<EnabledRefRO<LightEnabled>, RefRO<BridgeObject>>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .WithChangeFilter<LightEnabled>())
            {
                Burst.LightEnabled.Data.Invoke(bridge.ValueRO, enabled.ValueRO);
            }

            foreach (var (data, bridge) in SystemAPI.Query<RefRO<LightBakingOutputData>, RefRO<BridgeObject>>().WithChangeFilter<LightBakingOutputData>())
            {
                Burst.LightBakingOutputData.Data.Invoke(bridge.ValueRO, data.ValueRO);
            }

            foreach (var (data, bridge) in SystemAPI.Query<RefRO<LightData>, RefRO<BridgeObject>>().WithChangeFilter<LightData>())
            {
                Burst.LightData.Data.Invoke(bridge.ValueRO, data.ValueRO);
            }

            foreach (var (data, bridge) in SystemAPI.Query<RefRO<LightDataExtended>, RefRO<BridgeObject>>().WithChangeFilter<LightDataExtended>())
            {
                Burst.LightDataExtended.Data.Invoke(bridge.ValueRO, data.ValueRO);
            }

#if UNITY_URP
            foreach (var (data, bridge) in SystemAPI.Query<RefRO<LightUniversalData>, RefRO<BridgeObject>>().WithChangeFilter<LightUniversalData>())
            {
                Burst.LightUniversalData.Data.Invoke(bridge.ValueRO, data.ValueRO);
            }
#endif
#if UNITY_HDRP
            foreach (var (data, bridge) in SystemAPI.Query<RefRO<LightHdData>, RefRO<BridgeObject>>().WithChangeFilter<LightHdData>())
            {
                Burst.LightHdData.Data.Invoke(bridge.ValueRO, data.ValueRO);
            }
#endif
        }

        private static unsafe void LightEnabledChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, bool>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var isEnabled = ref arguments.Second;
            bridge.Q<Light>().enabled = isEnabled;
        }

        private static unsafe void LightBakingOutputDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, LightBakingOutputData>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            // replaces HybridLightBakingDataSystem
            var light = bridge.Q<Light>();
            light.bakingOutput = component.Value;
        }

        private static unsafe void LightDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, LightData>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var light = bridge.Q<Light>();
            light.color = component.Color;
            light.intensity = component.Intensity;
            light.colorTemperature = component.ColorTemperature;
        }

        private static unsafe void LightDataExtendedChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, LightDataExtended>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var light = bridge.Q<Light>();
            light.type = component.Type;

            light.range = component.Range;
            light.spotAngle = component.SpotAngle;
            light.innerSpotAngle = component.InnerSpotAngle;
            light.cookieSize2D = component.CookieSize;
            light.cookie = component.Cookie.Value;

            light.bounceIntensity = component.BounceIntensity;

            light.shadows = component.Shadows;
            light.shadowStrength = component.ShadowStrength;
            light.shadowBias = component.ShadowBias;
            light.shadowNormalBias = component.ShadowNormalBias;
            light.shadowNearPlane = component.ShadowNearPlane;

            light.renderMode = component.RenderMode;
            light.cullingMask = component.CullingMask;
            light.renderingLayerMask = component.RenderingLayerMask;
        }

#if UNITY_URP
        private static unsafe void LightUniversalDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, LightUniversalData>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var additional = bridge.Q<UniversalAdditionalLightData>();
            additional.usePipelineSettings = component.UsePipelineSettings;
            additional.softShadowQuality = component.SoftShadowQuality;
            additional.renderingLayers = component.RenderingLayers;
            additional.shadowRenderingLayers = component.RenderingLayers;
            additional.lightCookieSize = component.CookieSize;
            additional.lightCookieOffset = component.CookieOffset;
        }
#endif

#if UNITY_HDRP
        private static unsafe void LightHdDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, LightHdData>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var additional = bridge.Q<HDAdditionalLightData>();
            additional.lightDimmer = component.LightDimmer;
            additional.volumetricDimmer = component.VolumetricDimmer;
            additional.affectDiffuse = component.AffectDiffuse;
            additional.affectSpecular = component.AffectSpecular;
            additional.fadeDistance = component.FadeDistance;
            additional.shadowDimmer = component.ShadowDimmer;
            additional.shadowFadeDistance = component.ShadowFadeDistance;
            additional.affectsVolumetric = component.AffectsVolumetric;
        }
#endif

        private static class Burst
        {
            public static readonly SharedStatic<BurstTrampoline> LightEnabled =
                SharedStatic<BurstTrampoline>.GetOrCreate<LightSyncSystem, LightEnabled>();

            public static readonly SharedStatic<BurstTrampoline> LightBakingOutputData =
                SharedStatic<BurstTrampoline>.GetOrCreate<LightSyncSystem, LightBakingOutputData>();

            public static readonly SharedStatic<BurstTrampoline> LightData =
                SharedStatic<BurstTrampoline>.GetOrCreate<LightSyncSystem, LightData>();

            public static readonly SharedStatic<BurstTrampoline> LightDataExtended =
                SharedStatic<BurstTrampoline>.GetOrCreate<LightSyncSystem, LightDataExtended>();

#if UNITY_URP
            public static readonly SharedStatic<BurstTrampoline> LightUniversalData =
                SharedStatic<BurstTrampoline>.GetOrCreate<LightSyncSystem, LightUniversalData>();
#endif

#if UNITY_HDRP
            public static readonly SharedStatic<BurstTrampoline> LightHdData =
                SharedStatic<BurstTrampoline>.GetOrCreate<LightSyncSystem, LightHdData>();
#endif
        }
    }
}
