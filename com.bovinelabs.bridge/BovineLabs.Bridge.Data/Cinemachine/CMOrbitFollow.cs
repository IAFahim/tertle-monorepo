// <copyright file="CMOrbitFollow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Cinemachine.TargetTracking;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CMOrbitFollow : IComponentData
    {
        public TrackerSettings TrackerSettings;
        public CinemachineOrbitalFollow.OrbitStyles OrbitStyle;
        public float Radius;
        public Cinemachine3OrbitRig.Settings Orbits;
        public InputAxis HorizontalAxis;
        public InputAxis VerticalAxis;
        public InputAxis RadialAxis;
        public float3 TargetOffset;
        public CinemachineOrbitalFollow.ReferenceFrames RecenteringTarget;
    }
}
#endif