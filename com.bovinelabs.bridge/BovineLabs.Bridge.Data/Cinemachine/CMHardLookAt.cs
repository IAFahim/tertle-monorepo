// <copyright file="CMHardLookAt.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CMHardLookAt : IComponentData
    {
        public float3 LookAtOffset;
    }
}
#endif