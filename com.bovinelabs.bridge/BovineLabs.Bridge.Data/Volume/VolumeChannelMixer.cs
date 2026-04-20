// <copyright file="VolumeChannelMixer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;

    public struct VolumeChannelMixer : IComponentData
    {
        public float RedOutRedIn;
        public float RedOutGreenIn;
        public float RedOutBlueIn;
        public float GreenOutRedIn;
        public float GreenOutGreenIn;
        public float GreenOutBlueIn;
        public float BlueOutRedIn;
        public float BlueOutGreenIn;
        public float BlueOutBlueIn;

        public bool Active;
        public bool RedOutRedInOverride;
        public bool RedOutGreenInOverride;
        public bool RedOutBlueInOverride;
        public bool GreenOutRedInOverride;
        public bool GreenOutGreenInOverride;
        public bool GreenOutBlueInOverride;
        public bool BlueOutRedInOverride;
        public bool BlueOutGreenInOverride;
        public bool BlueOutBlueInOverride;
    }
}
#endif
