// <copyright file="VolumeMotionBlur.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine.Rendering.Universal;

    public struct VolumeMotionBlur : IComponentData
    {
        public MotionBlurMode Mode;
        public MotionBlurQuality Quality;
        public float Intensity;
        public float Clamp;

        public bool Active;
        public bool ModeOverride;
        public bool QualityOverride;
        public bool IntensityOverride;
        public bool ClampOverride;
    }
}
#endif
