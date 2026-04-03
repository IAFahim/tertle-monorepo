// <copyright file="LightUniversalData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP

namespace BovineLabs.Bridge.Data.Light
{
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// Optional data that maps to <c>UnityEngine.Rendering.Universal.UniversalAdditionalLightData</c>.
    /// </summary>
    public struct LightUniversalData : IComponentData
    {
        public bool UsePipelineSettings;

        public SoftShadowQuality SoftShadowQuality;

        public RenderingLayerMask RenderingLayers;

        public float2 CookieSize;
        public float2 CookieOffset;
    }
}

#endif
