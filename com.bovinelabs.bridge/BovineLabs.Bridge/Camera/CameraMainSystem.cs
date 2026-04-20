// <copyright file="CameraMainSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Camera
{
    using System;
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Core;
    using BovineLabs.Core.Assertions;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Cinemachine;
#endif

    [UpdateInGroup(typeof(BridgeReadSystemGroup))]
    public partial class CameraMainSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var cameraQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<LocalTransform, CameraBridge>()
                .WithAll<CameraMain>()
#if UNITY_CINEMACHINE
                .WithAllRW<CinemachineBrainBridge>()
                .WithAllRW<CMBrain>()
#endif
                .Build();

            if (cameraQuery.IsEmptyIgnoreFilter)
            {
                // User hasn't setup an entity, create our own
#if UNITY_CINEMACHINE
                Span<ComponentType> components = stackalloc ComponentType[7];
#else
                Span<ComponentType> components = stackalloc ComponentType[5];
#endif
                components[0] = ComponentType.ReadWrite<CameraMain>();
                components[1] = ComponentType.ReadWrite<LocalTransform>();
                components[2] = ComponentType.ReadWrite<CameraFrustumPlanes>();
                components[3] = ComponentType.ReadWrite<CameraFrustumCorners>();
                components[4] = ComponentType.ReadWrite<CameraBridge>();
#if UNITY_CINEMACHINE
                components[5] = ComponentType.ReadWrite<CMBrain>();
                components[6] = ComponentType.ReadWrite<CinemachineBrainBridge>();
#endif
                var e = this.EntityManager.CreateEntity(components);
                this.EntityManager.SetName(e, "Camera Main");
            }

            cameraQuery.CompleteDependency();

            ref var cameraBridge = ref cameraQuery.GetSingletonRW<CameraBridge>().ValueRW;

            if (!cameraBridge.Value.IsValid())
            {
                cameraBridge.Value.Value = Camera.main;
                if (cameraBridge.Value.Value == null)
                {
                    SystemAPI.GetSingleton<BLLogger>().LogError("No main camera found");
                    return;
                }

#if UNITY_CINEMACHINE
                ref var brainBridge = ref cameraQuery.GetSingletonRW<CinemachineBrainBridge>().ValueRW;
                Check.Assume(!brainBridge.Value.IsValid(), "Camera invalid but brain not");

                var cinemachineBrain = cameraBridge.Value.Value.GetComponent<CinemachineBrain>();
                if (cinemachineBrain == null)
                {
                    SystemAPI.GetSingleton<BLLogger>().LogError("No CinemachineBrain found on camera");
                    return;
                }

                brainBridge.Value.Value = cinemachineBrain;
                ref var brain = ref cameraQuery.GetSingletonRW<CMBrain>().ValueRW;
                brain.IgnoreTimeScale = cinemachineBrain.IgnoreTimeScale;
                brain.UpdateMethod = cinemachineBrain.UpdateMethod;
                brain.BlendUpdateMethod = cinemachineBrain.BlendUpdateMethod;
                brain.DefaultBlend = cinemachineBrain.DefaultBlend;
#endif
            }

            var tr = cameraBridge.Value.Value.transform;
            cameraQuery.GetSingletonRW<LocalTransform>().ValueRW = LocalTransform.FromPositionRotation(tr.position, tr.rotation);
        }
    }
}
