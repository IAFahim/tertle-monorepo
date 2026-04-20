// <copyright file="CinemachineBrainBridge.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    public struct CinemachineBrainBridge : IComponentData
    {
        public UnityObjectRef<CinemachineBrain> Value;
    }
}
#endif