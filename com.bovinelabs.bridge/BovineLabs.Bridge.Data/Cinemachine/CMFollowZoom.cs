// <copyright file="CMFollowZoom.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CMFollowZoom : IComponentData
    {
        public float Width;
        public float Damping;
        public float2 FovRange;
    }
}
#endif