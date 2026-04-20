// <copyright file="VolumeSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine.Rendering;

    public struct VolumeSettings : IComponentData
    {
        public float Weight;
        public float Priority;
        public float BlendDistance;
        public bool IsGlobal;
        public UnityObjectRef<VolumeProfile> Profile;
    }
}
#endif
