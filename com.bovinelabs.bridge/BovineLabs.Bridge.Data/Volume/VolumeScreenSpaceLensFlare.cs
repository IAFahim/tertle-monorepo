// <copyright file="VolumeScreenSpaceLensFlare.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    public struct VolumeScreenSpaceLensFlare : IComponentData
    {
        public float Intensity;
        public Color TintColor;
        public int BloomMip;
        public float FirstFlareIntensity;
        public float SecondaryFlareIntensity;
        public float WarpedFlareIntensity;
        public Vector2 WarpedFlareScale;
        public int Samples;
        public float SampleDimmer;
        public float VignetteEffect;
        public float StartingPosition;
        public float Scale;
        public float StreaksIntensity;
        public float StreaksLength;
        public float StreaksOrientation;
        public float StreaksThreshold;
        public ScreenSpaceLensFlareResolution Resolution;
        public float ChromaticAbberationIntensity;

        public bool Active;
        public bool IntensityOverride;
        public bool TintColorOverride;
        public bool BloomMipOverride;
        public bool FirstFlareIntensityOverride;
        public bool SecondaryFlareIntensityOverride;
        public bool WarpedFlareIntensityOverride;
        public bool WarpedFlareScaleOverride;
        public bool SamplesOverride;
        public bool SampleDimmerOverride;
        public bool VignetteEffectOverride;
        public bool StartingPositionOverride;
        public bool ScaleOverride;
        public bool StreaksIntensityOverride;
        public bool StreaksLengthOverride;
        public bool StreaksOrientationOverride;
        public bool StreaksThresholdOverride;
        public bool ResolutionOverride;
        public bool ChromaticAbberationIntensityOverride;
    }
}
#endif
