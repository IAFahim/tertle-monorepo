// <copyright file="LightDataExtended.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Light
{
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Extended data for <see cref="UnityEngine.Light"/> properties that rarely change at runtime.
    /// </summary>
    public struct LightDataExtended : IComponentData
    {
        public LightType Type;

        public float Range;
        public float SpotAngle;
        public float InnerSpotAngle;
        public float2 CookieSize;

        public UnityObjectRef<Texture> Cookie;

        public float BounceIntensity;

        public LightShadows Shadows;
        public float ShadowStrength;
        public float ShadowBias;
        public float ShadowNormalBias;
        public float ShadowNearPlane;

        public LightRenderMode RenderMode;
        public int CullingMask;
        public int RenderingLayerMask;
    }
}
