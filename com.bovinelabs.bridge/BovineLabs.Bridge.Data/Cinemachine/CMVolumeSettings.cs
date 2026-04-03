// <copyright file="CMVolumeSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using UnityEngine.Rendering;

    public struct CMVolumeSettings : IComponentData
    {
        public float Weight;
        public CinemachineVolumeSettings.FocusTrackingMode FocusTracking;
        public Entity FocusTarget;
        public float FocusOffset;
        public UnityObjectRef<VolumeProfile> Profile;
    }
}
#endif