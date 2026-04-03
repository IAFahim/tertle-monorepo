// <copyright file="CMFollow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine.TargetTracking;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CMFollow : IComponentData
    {
        public float3 FollowOffset;
        public TrackerSettings TrackerSettings;
    }
}
#endif