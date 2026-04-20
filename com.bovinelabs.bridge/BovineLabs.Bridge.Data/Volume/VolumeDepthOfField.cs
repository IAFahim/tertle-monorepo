// <copyright file="VolumeDepthOfField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine.Rendering.Universal;

    public struct VolumeDepthOfField : IComponentData
    {
        public DepthOfFieldMode Mode;
        public float GaussianStart;
        public float GaussianEnd;
        public float GaussianMaxRadius;
        public bool HighQualitySampling;
        public float FocusDistance;
        public float Aperture;
        public float FocalLength;
        public int BladeCount;
        public float BladeCurvature;
        public float BladeRotation;

        public bool Active;
        public bool ModeOverride;
        public bool GaussianStartOverride;
        public bool GaussianEndOverride;
        public bool GaussianMaxRadiusOverride;
        public bool HighQualitySamplingOverride;
        public bool FocusDistanceOverride;
        public bool ApertureOverride;
        public bool FocalLengthOverride;
        public bool BladeCountOverride;
        public bool BladeCurvatureOverride;
        public bool BladeRotationOverride;
    }
}
#endif
