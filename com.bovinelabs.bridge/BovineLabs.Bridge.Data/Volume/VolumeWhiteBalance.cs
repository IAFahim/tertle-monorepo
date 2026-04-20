// <copyright file="VolumeWhiteBalance.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;

    public struct VolumeWhiteBalance : IComponentData
    {
        public float Temperature;
        public float Tint;

        public bool Active;
        public bool TemperatureOverride;
        public bool TintOverride;
    }
}
#endif
