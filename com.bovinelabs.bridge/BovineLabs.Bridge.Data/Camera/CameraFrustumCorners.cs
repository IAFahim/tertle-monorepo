// <copyright file="CameraFrustumCorners.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Camera
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CameraFrustumCorners : IComponentData
    {
        public float3x4 NearPlane;
        public float3x4 FarPlane;
    }
}
