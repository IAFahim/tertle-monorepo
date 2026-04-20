// <copyright file="CameraViewSpaceOffset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Camera
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CameraViewSpaceOffset : IComponentData
    {
        /// <summary>
        /// Offset of the projection center as a fraction of the half-frustum size at the near plane.
        /// (1, 0) shifts by one half-width.
        /// </summary>
        public float2 ProjectionCenterOffset;
    }
}
