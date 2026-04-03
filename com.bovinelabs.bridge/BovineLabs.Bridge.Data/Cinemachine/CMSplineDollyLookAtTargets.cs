// <copyright file="CMSplineDollyLookAtTargets.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
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
}
#endif