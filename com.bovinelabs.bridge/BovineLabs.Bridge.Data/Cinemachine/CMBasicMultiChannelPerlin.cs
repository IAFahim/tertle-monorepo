// <copyright file="CMBasicMultiChannelPerlin.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using UnityEngine;

    public struct CMBasicMultiChannelPerlin : IComponentData
    {
        public Vector3 PivotOffset;
        public float AmplitudeGain;
        public float FrequencyGain;
        public UnityObjectRef<NoiseSettings> NoiseProfile;
    }
}
#endif