// <copyright file="CMCameraOffset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CMCameraOffset : IComponentData
    {
        public float3 Offset;
        public CinemachineCore.Stage ApplyAfter;
        public bool PreserveComposition;
    }
}
#endif