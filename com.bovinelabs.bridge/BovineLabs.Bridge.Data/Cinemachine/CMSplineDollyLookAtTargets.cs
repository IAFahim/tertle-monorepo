// <copyright file="CMSplineDollyLookAtTargets.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && UNITY_SPLINES
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine.Splines;

    public struct CMSplineDollyLookAtTargets : IComponentData
    {
        public PathIndexUnit PathIndexUnit;
    }

    public struct CMSplineDollyLookAtTarget : IBufferElementData
    {
        public float Position;
        public Entity LookAt;
        public float3 Offset;
        public float Easing;
    }

    public struct CMSplineDollyLookAtTargetBridge : IBufferElementData
    {
        public Entity Value;
    }
}
#endif
