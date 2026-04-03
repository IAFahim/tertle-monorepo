// <copyright file="CinemachineTrackingBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Authoring.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Entities;

    internal class CinemachineTrackingBaker : Baker<CinemachineTrackingAuthoring>
    {
        public override void Bake(CinemachineTrackingAuthoring authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Dynamic);
            this.AddComponentObject(entity, authoring);
            this.AddComponent<CinemachineTracking>(entity);
        }
    }
}
#endif