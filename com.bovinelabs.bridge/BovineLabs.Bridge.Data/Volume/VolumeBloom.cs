// <copyright file="VolumeBloom.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    public struct VolumeBloom : IComponentData
    {
        public float Threshold;
        public float Intensity;
        public float Scatter;
        public float Clamp;
        public Color Tint;
        public bool HighQualityFiltering;
        public BloomFilterMode Filter;
        public BloomDownscaleMode Downscale;
        public int MaxIterations;
        public UnityObjectRef<Texture> DirtTexture;
        public float DirtIntensity;

        public bool Active;
        public bool ThresholdOverride;
        public bool IntensityOverride;
        public bool ScatterOverride;
        public bool ClampOverride;
        public bool TintOverride;
        public bool HighQualityFilteringOverride;
        public bool FilterOverride;
        public bool DownscaleOverride;
        public bool MaxIterationsOverride;
        public bool DirtTextureOverride;
        public bool DirtIntensityOverride;
    }
}
#endif
