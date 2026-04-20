// <copyright file="VolumeColorAdjustments.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;

    public struct VolumeColorAdjustments : IComponentData
    {
        public float PostExposure;
        public float Contrast;
        public Color ColorFilter;
        public float HueShift;
        public float Saturation;

        public bool Active;
        public bool PostExposureOverride;
        public bool ContrastOverride;
        public bool ColorFilterOverride;
        public bool HueShiftOverride;
        public bool SaturationOverride;
    }
}
#endif
