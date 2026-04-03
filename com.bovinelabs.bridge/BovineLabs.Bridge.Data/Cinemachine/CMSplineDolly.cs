// <copyright file="CMSplineDolly.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine.Splines;

    public struct CMSplineDolly : IComponentData
    {
        public Entity Spline;
        public float Position;
        public PathIndexUnit PositionUnits;
        public float3 SplineOffset;
        public CinemachineSplineDolly.RotationMode CameraRotation;
        public CMSplineDollyDamping Damping;
        public CMSplineAutoDolly AutoDolly;
    }

    public struct CMSplineDollyDamping
    {
        public bool Enabled;
        public float3 Position;
        public float Angular;
    }

    public struct CMSplineAutoDolly
    {
        public bool Enabled;
        public CMSplineAutoDollyType Type;
        public float FixedSpeed;
        public float PositionOffset;
        public int SearchResolution;
        public int SearchIteration;
    }

    public enum CMSplineAutoDollyType : byte
    {
        None,
        FixedSpeed,
        NearestPointToTarget,
    }
}
#endif