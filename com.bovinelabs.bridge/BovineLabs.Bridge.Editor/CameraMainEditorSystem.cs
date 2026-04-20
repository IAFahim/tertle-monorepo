// <copyright file="CameraMainEditorSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Editor
{
    using System;
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Camera;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEditor;
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Cinemachine;
#endif

    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(BridgeReadSystemGroup))]
    public partial class CameraMainEditorSystem : SystemBase
    {
        private EntityArchetype archetype;
        private Entity localCameraEntity;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            Span<ComponentType> components = stackalloc ComponentType[5];
            components[0] = ComponentType.ReadWrite<CameraMain>();
            components[1] = ComponentType.ReadWrite<LocalTransform>();
            components[2] = ComponentType.ReadWrite<CameraBridge>();
#if UNITY_CINEMACHINE
            components[3] = ComponentType.ReadWrite<CMBrain>();
            components[4] = ComponentType.ReadWrite<CinemachineBrainBridge>();
#endif
            this.archetype = this.EntityManager.CreateArchetype(components);
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var cameraQuery = SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<CameraMain>().Build();

            var cameras = cameraQuery.CalculateEntityCount();
            if (cameras == 0)
            {
                this.localCameraEntity = this.EntityManager.CreateEntity(this.archetype);
                this.EntityManager.SetName(this.localCameraEntity, "Camera Main");
            }
            else if (cameras > 1)
            {
                if (this.localCameraEntity != Entity.Null)
                {
                    this.EntityManager.DestroyEntity(this.localCameraEntity);
                    this.localCameraEntity = Entity.Null;
                }
            }

            // check again after potential changes
            cameras = cameraQuery.CalculateEntityCount();

            if (cameras != 1)
            {
                // Use has more than 1 camera in scene
                return;
            }

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return;
            }

            var camera = sceneView.camera;
            if (camera == null)
            {
                return;
            }

            var entity = cameraQuery.GetSingletonEntity();

            this.EntityManager.SetComponentData(entity, new CameraBridge { Value = camera });
            var tr = camera.transform;
            this.EntityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(tr.position, tr.rotation));

#if UNITY_CINEMACHINE

            var brain = camera.GetComponent<CinemachineBrain>();
            var bridge = this.EntityManager.GetComponentData<CinemachineBrainBridge>(entity);

            if (brain == bridge.Value.Value)
            {
                return;
            }

            this.EntityManager.SetComponentData(entity, new CinemachineBrainBridge { Value = brain });

            if (brain != null)
            {
                this.EntityManager.SetComponentData(entity, new CMBrain
                {
                    IgnoreTimeScale = brain.IgnoreTimeScale,
                    UpdateMethod = brain.UpdateMethod,
                    BlendUpdateMethod = brain.BlendUpdateMethod,
                    DefaultBlend = brain.DefaultBlend,
                });
            }
#endif
        }
    }
}