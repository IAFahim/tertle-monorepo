// <copyright file="VolumeChromaticAberration.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;

    public struct VolumeChromaticAberration : IComponentData
    {
        public float Intensity;

        public bool Active;
        public bool IntensityOverride;
    }
}
#endif
