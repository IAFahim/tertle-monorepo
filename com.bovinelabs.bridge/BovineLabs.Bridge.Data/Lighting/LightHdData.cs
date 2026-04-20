// <copyright file="LightHdData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP

namespace BovineLabs.Bridge.Data.Lighting
{
    using Unity.Entities;

    /// <summary>
    /// Optional data that maps to <c>UnityEngine.Rendering.HighDefinition.HDAdditionalLightData</c>.
    /// </summary>
    public struct LightHdData : IComponentData
    {
        public float LightDimmer;
        public float VolumetricDimmer;
        public bool AffectDiffuse;
        public bool AffectSpecular;
        public float FadeDistance;
        public float ShadowDimmer;
        public float ShadowFadeDistance;
        public bool AffectsVolumetric;
    }
}

#endif
