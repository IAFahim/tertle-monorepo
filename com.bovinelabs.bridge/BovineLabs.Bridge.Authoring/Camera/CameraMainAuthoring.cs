// <copyright file="CameraMainAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring.Camera
{
    using BovineLabs.Bridge.Data.Camera;
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
#endif
    using Unity.Entities;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class CameraMainAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Vector2 projectionCenterOffset;

        private class Baker : Baker<CameraMainAuthoring>
        {
            public override void Bake(CameraMainAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.Dynamic);

                var cts = new ComponentTypeSet(typeof(CameraMain), typeof(CameraBridge), typeof(CameraFrustumPlanes), typeof(CameraFrustumCorners),
                    typeof(CameraViewSpaceOffset));

                this.AddComponent(entity, cts);

                this.SetComponent(entity, new CameraViewSpaceOffset { ProjectionCenterOffset = authoring.projectionCenterOffset });

#if UNITY_CINEMACHINE
                this.AddComponent<CMBrain>(entity);
                this.AddComponent<CinemachineBrainBridge>(entity);
#endif
            }
        }
    }
}
