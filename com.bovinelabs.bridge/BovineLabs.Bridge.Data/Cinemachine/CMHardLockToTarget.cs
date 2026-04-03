// <copyright file="CMHardLockToTarget.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Entities;

    public struct CMHardLockToTarget : IComponentData
    {
        public float Damping;
    }
}
#endif