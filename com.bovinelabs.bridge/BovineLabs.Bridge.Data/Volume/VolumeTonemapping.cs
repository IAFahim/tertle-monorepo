// <copyright file="VolumeTonemapping.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine.Rendering.Universal;

    public struct VolumeTonemapping : IComponentData
    {
        public TonemappingMode Mode;
        public NeutralRangeReductionMode NeutralHDRRangeReductionMode;
        public HDRACESPreset AcesPreset;
        public float HueShiftAmount;
        public bool DetectPaperWhite;
        public float PaperWhite;
        public bool DetectBrightnessLimits;
        public float MinNits;
        public float MaxNits;

        public bool Active;
        public bool ModeOverride;
        public bool NeutralHDRRangeReductionModeOverride;
        public bool AcesPresetOverride;
        public bool HueShiftAmountOverride;
        public bool DetectPaperWhiteOverride;
        public bool PaperWhiteOverride;
        public bool DetectBrightnessLimitsOverride;
        public bool MinNitsOverride;
        public bool MaxNitsOverride;
    }
}
#endif
