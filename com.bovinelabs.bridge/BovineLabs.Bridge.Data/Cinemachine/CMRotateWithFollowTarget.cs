// <copyright file="CMRotateWithFollowTarget.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Entities;

    public struct CMRotateWithFollowTarget : IComponentData
    {
        public float Damping;
    }
}
#endif