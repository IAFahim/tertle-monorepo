// <copyright file="CMThirdPersonFollow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && UNITY_PHYSICS
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CMThirdPersonFollow : IComponentData
    {
        public float3 Damping;
        public float3 ShoulderOffset;
        public float VerticalArmLength;
        public float CameraSide;
        public float CameraDistance;
        public CinemachineThirdPersonFollowDots.ObstacleSettings AvoidObstacles;
    }
}
#endif