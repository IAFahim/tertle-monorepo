// <copyright file="CinemachineSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Camera
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;
#if UNITY_PHYSICS
    using Unity.Physics;
#endif
    using Unity.Transforms;
    using UnityEngine;
#if UNITY_SPLINES
    using UnityEngine.Splines;
#endif

    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    public partial struct CinemachineSyncSystem : ISystem
    {
        static unsafe CinemachineSyncSystem()
        {
            Burst.Brain.Data = new BurstTrampoline(&BrainChangedPacked);
            Burst.CameraEnabled.Data = new BurstTrampoline(&CameraEnabledChangedPacked);
            Burst.Camera.Data = new BurstTrampoline(&CameraChangedPacked);
            Burst.CameraTargets.Data = new BurstTrampoline(&CameraTargetsChangedPacked);
            Burst.Follow.Data = new BurstTrampoline(&FollowChangedPacked);
            Burst.PositionComposer.Data = new BurstTrampoline(&PositionComposerChangedPacked);
            Burst.RotationComposer.Data = new BurstTrampoline(&RotationComposerChangedPacked);
#if UNITY_PHYSICS
            Burst.ThirdPersonFollow.Data = new BurstTrampoline(&ThirdPersonFollowChangedPacked);
#endif
#if UNITY_SPLINES
            Burst.SplineDolly.Data = new BurstTrampoline(&SplineDollyChangedPacked);
            Burst.SplineDollyTarget.Data = new BurstTrampoline(&SplineDollyTargetChangedPacked);
            Burst.SplineDollyLookAtTargets.Data = new BurstTrampoline(&SplineDollyLookAtTargetsChangedPacked);
#endif
            Burst.OrbitFollow.Data = new BurstTrampoline(&OrbitCameraChangedPacked);
            Burst.FreeLookModifier.Data = new BurstTrampoline(&FreeLookModifierChangedPacked);
            Burst.RotateWithFollowTarget.Data = new BurstTrampoline(&RotateWithFollowTargetChangedPacked);
            Burst.HardLockToTarget.Data = new BurstTrampoline(&HardLockToTargetChangedPacked);
            Burst.HardLookAt.Data = new BurstTrampoline(&HardLookAtChangedPacked);
            Burst.PanTilt.Data = new BurstTrampoline(&POVCameraChangedPacked);
            Burst.BasicMultiChannelPerlin.Data = new BurstTrampoline(&BasicMultiChannelPerlinChangedPacked);
            Burst.GroupFraming.Data = new BurstTrampoline(&GroupFramingChangedPacked);
            Burst.FollowZoom.Data = new BurstTrampoline(&FollowZoomChangedPacked);
            Burst.CameraOffset.Data = new BurstTrampoline(&CameraOffsetChangedPacked);
            Burst.Recomposer.Data = new BurstTrampoline(&RecomposerChangedPacked);
            Burst.VolumeSettingsFocus.Data = new BurstTrampoline(&VolumeSettingsFocusChangedPacked);
            Burst.VolumeSettings.Data = new BurstTrampoline(&VolumeSettingsChangedPacked);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.BrainChanged(ref state);
            this.CameraEnabledChanged(ref state);
            this.CameraChanged(ref state);
            this.CameraTargetsChanged(ref state);
            this.FollowChanged(ref state);
            this.PositionComposerChanged(ref state);
            this.ComposerChanged(ref state);
#if UNITY_PHYSICS
            this.ThirdPersonCameraChanged(ref state);
#endif
#if UNITY_SPLINES
            this.SplineDollyChanged(ref state);
            this.SplineDollyLookAtTargetsChanged(ref state);
#endif
            this.OrbitCameraChanged(ref state);
            this.FreeLookModifierChanged(ref state);
            this.RotateWithFollowTargetChanged(ref state);
            this.HardLockToTargetChanged(ref state);
            this.HardLookAtChanged(ref state);
            this.POVCameraChanged(ref state);
            this.BasicMultiChannelPerlinChanged(ref state);
            this.GroupFramingChanged(ref state);
            this.FollowZoomChanged(ref state);
            this.CameraOffsetChanged(ref state);
            this.RecomposerChanged(ref state);
            this.VolumeSettingsFocusChanged(ref state);
            this.VolumeSettingsChanged(ref state);
        }

        private void BrainChanged(ref SystemState state)
        {
            foreach (var (brainData, brain) in SystemAPI.Query<RefRO<CMBrain>, RefRO<CinemachineBrainBridge>>().WithChangeFilter<CMBrain>())
            {
                if (!brain.ValueRO.Value.IsValid())
                {
                    continue;
                }

                Burst.Brain.Data.Invoke(brain.ValueRO, brainData.ValueRO);
            }
        }

        private void CameraEnabledChanged(ref SystemState state)
        {
            foreach (var (enabled, cameraData, bridge) in SystemAPI
                .Query<EnabledRefRO<CMCameraEnabled>, RefRO<CMCamera>, RefRO<BridgeObject>>()
                .WithChangeFilter<CMCameraEnabled>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                Burst.CameraEnabled.Data.Invoke(bridge.ValueRO, enabled.ValueRO, cameraData.ValueRO);
            }
        }

        private void CameraChanged(ref SystemState state)
        {
            foreach (var (cameraData, bridge) in SystemAPI
                .Query<RefRO<CMCamera>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMCamera>())
            {
                Burst.Camera.Data.Invoke(bridge.ValueRO, cameraData.ValueRO);
            }
        }

        private void CameraTargetsChanged(ref SystemState state)
        {
            var localToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var bridgeObjects = SystemAPI.GetComponentLookup<BridgeObject>(true);

            foreach (var (cameraData, targetBridges, bridge) in SystemAPI
                .Query<RefRO<CMCamera>, RefRO<CMCameraTargetBridgeObjects>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                var targets = new CameraTargetState
                {
                    HasTrackingTarget = localToWorlds.HasComponent(cameraData.ValueRO.TrackingTarget),
                    HasLookAtTarget = localToWorlds.HasComponent(cameraData.ValueRO.LookAtTarget),
                };

                if (bridgeObjects.TryGetComponent(cameraData.ValueRO.TrackingTarget, out var trackingTargetSource))
                {
                    targets.TrackingTargetSource = trackingTargetSource;
                }

                if (bridgeObjects.TryGetComponent(targetBridges.ValueRO.TrackingTargetBridge, out var trackingTargetProxy))
                {
                    targets.TrackingTargetProxy = trackingTargetProxy;
                }

                if (bridgeObjects.TryGetComponent(cameraData.ValueRO.LookAtTarget, out var lookAtTargetSource))
                {
                    targets.LookAtTargetSource = lookAtTargetSource;
                }

                if (bridgeObjects.TryGetComponent(targetBridges.ValueRO.LookAtTargetBridge, out var lookAtTargetProxy))
                {
                    targets.LookAtTargetProxy = lookAtTargetProxy;
                }

                Burst.CameraTargets.Data.Invoke(bridge.ValueRO, cameraData.ValueRO, targets);
            }
        }

        private void FollowChanged(ref SystemState state)
        {
            foreach (var (followData, bridge) in SystemAPI
                .Query<RefRO<CMFollow>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMFollow>())
            {
                Burst.Follow.Data.Invoke(bridge.ValueRO, followData.ValueRO);
            }
        }

        private void PositionComposerChanged(ref SystemState state)
        {
            foreach (var (composerData, bridge) in SystemAPI
                .Query<RefRO<CMPositionComposer>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMPositionComposer>())
            {
                Burst.PositionComposer.Data.Invoke(bridge.ValueRO, composerData.ValueRO);
            }
        }

        private void ComposerChanged(ref SystemState state)
        {
            foreach (var (composerData, bridge) in SystemAPI
                .Query<RefRO<CMRotationComposer>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMRotationComposer>())
            {
                Burst.RotationComposer.Data.Invoke(bridge.ValueRO, composerData.ValueRO);
            }
        }

#if UNITY_PHYSICS
        private void ThirdPersonCameraChanged(ref SystemState state)
        {
            SystemAPI.TryGetSingletonEntity<PhysicsWorldSingleton>(out var physicsWorldEntity);
            var context = new ThirdPersonFollowContext
            {
                PhysicsWorldEntity = physicsWorldEntity,
                WorldSequenceNumber = state.WorldUnmanaged.SequenceNumber,
            };

            foreach (var (data, bridge) in SystemAPI
                .Query<RefRO<CMThirdPersonFollow>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMThirdPersonFollow>())
            {
                Burst.ThirdPersonFollow.Data.Invoke(bridge.ValueRO, data.ValueRO, context);
            }
        }
#endif

#if UNITY_SPLINES
        private void SplineDollyChanged(ref SystemState state)
        {
            foreach (var (data, bridge) in SystemAPI
                .Query<RefRO<CMSplineDolly>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMSplineDolly>())
            {
                Burst.SplineDolly.Data.Invoke(bridge.ValueRO, data.ValueRO);
            }

            var bridgeObjects = SystemAPI.GetComponentLookup<BridgeObject>(true);

            foreach (var (target, cameraBridge) in SystemAPI
                .Query<RefRO<CMSplineDollyTarget>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMSplineDollyTarget>())
            {
                bridgeObjects.TryGetComponent(target.ValueRO.Spline, out BridgeObject splineBridge);
                Burst.SplineDollyTarget.Data.Invoke(cameraBridge.ValueRO, splineBridge);
            }
        }

        private void SplineDollyLookAtTargetsChanged(ref SystemState state)
        {
            var localToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var bridgeObjects = SystemAPI.GetComponentLookup<BridgeObject>(true);

            foreach (var (data, targets, targetBridges, bridge) in SystemAPI
                .Query<RefRO<CMSplineDollyLookAtTargets>, DynamicBuffer<CMSplineDollyLookAtTarget>, DynamicBuffer<CMSplineDollyLookAtTargetBridge>,
                    RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMSplineDollyLookAtTargets, CMSplineDollyLookAtTarget>())
            {
                var arguments = new SplineDollyLookAtTargetsPayload
                {
                    Bridge = bridge.ValueRO,
                    Data = data.ValueRO,
                    Targets = targets,
                    TargetBridges = targetBridges,
                    LocalToWorlds = localToWorlds,
                    BridgeObjects = bridgeObjects,
                };

                Burst.SplineDollyLookAtTargets.Data.Invoke(ref arguments);
            }
        }
#endif

        private void OrbitCameraChanged(ref SystemState state)
        {
            foreach (var (orbitData, bridge) in SystemAPI
                .Query<RefRO<CMOrbitFollow>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMOrbitFollow>())
            {
                Burst.OrbitFollow.Data.Invoke(bridge.ValueRO, orbitData.ValueRO);
            }
        }

        private void FreeLookModifierChanged(ref SystemState state)
        {
            foreach (var (modifierData, buffer, bridge) in SystemAPI
                .Query<RefRO<CMFreeLookModifier>, DynamicBuffer<CMFreeLookModifierEntry>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMFreeLookModifier, CMFreeLookModifierEntry>())
            {
                Burst.FreeLookModifier.Data.Invoke(bridge.ValueRO, modifierData.ValueRO, buffer);
            }
        }

        private void RotateWithFollowTargetChanged(ref SystemState state)
        {
            foreach (var (rotateData, bridge) in SystemAPI
                .Query<RefRO<CMRotateWithFollowTarget>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMRotateWithFollowTarget>())
            {
                Burst.RotateWithFollowTarget.Data.Invoke(bridge.ValueRO, rotateData.ValueRO);
            }
        }

        private void HardLockToTargetChanged(ref SystemState state)
        {
            foreach (var (lockData, bridge) in SystemAPI
                .Query<RefRO<CMHardLockToTarget>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMHardLockToTarget>())
            {
                Burst.HardLockToTarget.Data.Invoke(bridge.ValueRO, lockData.ValueRO);
            }
        }

        private void HardLookAtChanged(ref SystemState state)
        {
            foreach (var (lookAtData, bridge) in SystemAPI
                .Query<RefRO<CMHardLookAt>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMHardLookAt>())
            {
                Burst.HardLookAt.Data.Invoke(bridge.ValueRO, lookAtData.ValueRO);
            }
        }

        private void POVCameraChanged(ref SystemState state)
        {
            foreach (var (panTiltData, bridge) in SystemAPI
                .Query<RefRO<CMPanTilt>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMPanTilt>())
            {
                Burst.PanTilt.Data.Invoke(bridge.ValueRO, panTiltData.ValueRO);
            }
        }

        private void BasicMultiChannelPerlinChanged(ref SystemState state)
        {
            foreach (var (noiseData, bridge) in SystemAPI
                .Query<RefRO<CMBasicMultiChannelPerlin>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMBasicMultiChannelPerlin>())
            {
                Burst.BasicMultiChannelPerlin.Data.Invoke(bridge.ValueRO, noiseData.ValueRO);
            }
        }

        private void GroupFramingChanged(ref SystemState state)
        {
            foreach (var (framingData, bridge) in SystemAPI
                .Query<RefRO<CMGroupFraming>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMGroupFraming>())
            {
                Burst.GroupFraming.Data.Invoke(bridge.ValueRO, framingData.ValueRO);
            }
        }

        private void FollowZoomChanged(ref SystemState state)
        {
            foreach (var (zoomData, bridge) in SystemAPI
                .Query<RefRO<CMFollowZoom>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMFollowZoom>())
            {
                Burst.FollowZoom.Data.Invoke(bridge.ValueRO, zoomData.ValueRO);
            }
        }

        private void CameraOffsetChanged(ref SystemState state)
        {
            foreach (var (offsetData, bridge) in SystemAPI
                .Query<RefRO<CMCameraOffset>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMCameraOffset>())
            {
                Burst.CameraOffset.Data.Invoke(bridge.ValueRO, offsetData.ValueRO);
            }
        }

        private void RecomposerChanged(ref SystemState state)
        {
            foreach (var (recomposerData, bridge) in SystemAPI
                .Query<RefRO<CMRecomposer>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMRecomposer>())
            {
                Burst.Recomposer.Data.Invoke(bridge.ValueRO, recomposerData.ValueRO);
            }
        }

        private void VolumeSettingsFocusChanged(ref SystemState state)
        {
            var localToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var bridgeObjects = SystemAPI.GetComponentLookup<BridgeObject>(true);

            foreach (var (volumeData, focusBridge, bridge) in SystemAPI
                .Query<RefRO<CMVolumeSettings>, RefRO<CMVolumeSettingsFocusBridge>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMVolumeSettings>())
            {
                var arguments = new VolumeSettingsFocusPayload
                {
                    Bridge = bridge.ValueRO,
                    Data = volumeData.ValueRO,
                    FocusBridge = focusBridge.ValueRO,
                    LocalToWorlds = localToWorlds,
                    BridgeObjects = bridgeObjects,
                };

                Burst.VolumeSettingsFocus.Data.Invoke(ref arguments);
            }
        }

        private void VolumeSettingsChanged(ref SystemState state)
        {
            foreach (var (volumeData, bridge) in SystemAPI
                .Query<RefRO<CMVolumeSettings>, RefRO<BridgeObject>>()
                .WithAll<CMCameraEnabled>()
                .WithChangeFilter<CMVolumeSettings>())
            {
                Burst.VolumeSettings.Data.Invoke(bridge.ValueRO, volumeData.ValueRO);
            }
        }

        private static unsafe void BrainChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<CinemachineBrainBridge, CMBrain>>(argumentsPtr, argumentsSize);
            ref readonly var brainBridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var brain = brainBridge.Value.Value;
            brain.IgnoreTimeScale = component.IgnoreTimeScale;
            brain.UpdateMethod = component.UpdateMethod;
            brain.BlendUpdateMethod = component.BlendUpdateMethod;
            brain.DefaultBlend = component.DefaultBlend;
        }

        private static unsafe void CameraEnabledChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedTriple<BridgeObject, bool, CMCamera>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var isEnabled = ref arguments.Second;
            ref readonly var cameraData = ref arguments.Third;
            var camera = bridge.Q<CinemachineCamera>();
            camera.enabled = isEnabled;
            camera.gameObject.name = cameraData.Name.Value.ToString();
        }

        private static unsafe void CameraChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMCamera>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var camera = bridge.Q<CinemachineCamera>();
            camera.gameObject.name = component.Name.Value.ToString();

            camera.Priority = component.Priority;
            camera.OutputChannel = component.OutputChannel;
            camera.BlendHint = component.BlendHint;

            var lens = camera.Lens;
            lens.FieldOfView = math.clamp(component.FieldOfView, 1, 179);
            lens.NearClipPlane = component.NearClipPlane;
            lens.FarClipPlane = component.FarClipPlane;
            lens.Dutch = component.Dutch;
            lens.ModeOverride = component.ModeOverride;
            lens.OrthographicSize = component.OrthographicSize;
            camera.Lens = lens;

            camera.StandbyUpdate = component.StandbyUpdate;
        }

        private static unsafe void CameraTargetsChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments =
                ref BurstTrampoline.ArgumentsFromPtr<BurstManagedTriple<BridgeObject, CMCamera, CameraTargetState>>(argumentsPtr, argumentsSize);

            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            ref readonly var targets = ref arguments.Third;
            var camera = bridge.Q<CinemachineCamera>();
            camera.Target.CustomLookAtTarget = component.CustomLookAtTarget;
            camera.Target.TrackingTarget = ResolveTargetTransform(targets.HasTrackingTarget, targets.TrackingTargetSource, targets.TrackingTargetProxy);
            camera.Target.LookAtTarget = component.CustomLookAtTarget
                ? ResolveTargetTransform(targets.HasLookAtTarget, targets.LookAtTargetSource, targets.LookAtTargetProxy)
                : null;
        }

        private static unsafe void FollowChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMFollow>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var follow = bridge.Q<CinemachineFollow>();
            follow.FollowOffset = component.FollowOffset;
            follow.TrackerSettings = component.TrackerSettings;
        }

        private static unsafe void PositionComposerChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMPositionComposer>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var composer = bridge.Q<CinemachinePositionComposer>();
            composer.CameraDistance = component.CameraDistance;
            composer.DeadZoneDepth = component.DeadZoneDepth;
            composer.Composition = component.Composition;
            composer.TargetOffset = component.TargetOffset;
            composer.Damping = component.Damping;
            composer.Lookahead = component.Lookahead;
            composer.CenterOnActivate = component.CenterOnActivate;
        }

        private static unsafe void RotationComposerChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMRotationComposer>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var composer = bridge.Q<CinemachineRotationComposer>();
            composer.TargetOffset = component.TargetOffset;
            composer.Lookahead = component.Lookahead;
            composer.Damping = component.Damping;
            composer.Composition = component.Composition;
            composer.CenterOnActivate = component.CenterOnActivate;
        }

#if UNITY_PHYSICS
        private static unsafe void ThirdPersonFollowChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments =
                ref BurstTrampoline.ArgumentsFromPtr<BurstManagedTriple<BridgeObject, CMThirdPersonFollow, ThirdPersonFollowContext>>(argumentsPtr,
                    argumentsSize);

            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            ref readonly var context = ref arguments.Third;
            var thirdPerson = bridge.Q<CinemachineThirdPersonFollowDots>();
            thirdPerson.Damping = component.Damping;
            thirdPerson.ShoulderOffset = component.ShoulderOffset;
            thirdPerson.VerticalArmLength = component.VerticalArmLength;
            thirdPerson.CameraSide = math.clamp(component.CameraSide, 0, 1);
            thirdPerson.CameraDistance = component.CameraDistance;
            thirdPerson.AvoidObstacles = component.AvoidObstacles;
            thirdPerson.World = context.PhysicsWorldEntity != Entity.Null ? FindWorld(context.WorldSequenceNumber) : null;
            thirdPerson.PhysicsWorldEntity = context.PhysicsWorldEntity;
        }
#endif

#if UNITY_SPLINES
        private static unsafe void SplineDollyChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMSplineDolly>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var dolly = bridge.Q<CinemachineSplineDolly>();

            ref var settings = ref dolly.SplineSettings;
            settings.Position = component.Position;
            settings.Units = component.PositionUnits;
            settings.InvalidateCache();

            dolly.SplineOffset = component.SplineOffset;
            dolly.CameraRotation = component.CameraRotation;

            var damping = dolly.Damping;
            damping.Enabled = component.Damping.Enabled;
            damping.Position = component.Damping.Position;
            damping.Angular = component.Damping.Angular;
            dolly.Damping = damping;

            var autoDolly = dolly.AutomaticDolly;
            autoDolly.Enabled = component.AutoDolly.Enabled;
            autoDolly.Method = CreateAutoDolly(component.AutoDolly);
            dolly.AutomaticDolly = autoDolly;
        }

        private static unsafe void SplineDollyTargetChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, BridgeObject>>(argumentsPtr, argumentsSize);
            ref readonly var cameraBridge = ref arguments.First;
            ref readonly var splineBridge = ref arguments.Second;
            var dolly = cameraBridge.Q<CinemachineSplineDolly>();
            dolly.Spline = splineBridge.Value.IsValid() ? splineBridge.Value.Value.GetComponent<SplineContainer>() : null;
        }

        private static unsafe void SplineDollyLookAtTargetsChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<SplineDollyLookAtTargetsPayload>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.Bridge;
            ref readonly var data = ref arguments.Data;
            ref readonly var targets = ref arguments.Targets;
            ref readonly var targetBridges = ref arguments.TargetBridges;
            ref var localToWorlds = ref arguments.LocalToWorlds;
            ref var bridgeObjects = ref arguments.BridgeObjects;

            var lookAt = bridge.Q<CinemachineSplineDollyLookAtTargets>();
            var lookAtTargets = lookAt.Targets;
            lookAtTargets.PathIndexUnit = data.PathIndexUnit;
            lookAtTargets.Clear();

            for (var index = 0; index < targets.Length; index++)
            {
                var entry = targets[index];
                Transform target = null;

                if (localToWorlds.HasComponent(entry.LookAt) && bridgeObjects.TryGetComponent(targetBridges[index].Value, out var targetBridge) &&
                    targetBridge.Value.IsValid())
                {
                    target = targetBridge.Value.Value.transform;
                }

                lookAtTargets.Add(new DataPoint<CinemachineSplineDollyLookAtTargets.Item>(entry.Position, new CinemachineSplineDollyLookAtTargets.Item
                    {
                        LookAt = target,
                        Offset = entry.Offset,
                        Easing = entry.Easing,
                    }));
            }
        }
#endif

        private static unsafe void OrbitCameraChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMOrbitFollow>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var orbit = bridge.Q<CinemachineOrbitalFollow>();
            orbit.TargetOffset = component.TargetOffset;
            orbit.TrackerSettings = component.TrackerSettings;
            orbit.OrbitStyle = component.OrbitStyle;
            orbit.Radius = component.Radius;
            orbit.Orbits = component.Orbits;
            orbit.HorizontalAxis = component.HorizontalAxis;
            orbit.VerticalAxis = component.VerticalAxis;
            orbit.RadialAxis = component.RadialAxis;
            orbit.RecenteringTarget = component.RecenteringTarget;
        }

        private static unsafe void FreeLookModifierChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments =
                ref BurstTrampoline.ArgumentsFromPtr<BurstManagedTriple<BridgeObject, CMFreeLookModifier, DynamicBuffer<CMFreeLookModifierEntry>>>(argumentsPtr,
                    argumentsSize);

            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            ref readonly var buffer = ref arguments.Third;

            var modifier = bridge.Q<CinemachineFreeLookModifier>();
            modifier.Easing = component.Easing;

            var managedModifiers = modifier.Modifiers;
            if (managedModifiers == null)
            {
                managedModifiers = new System.Collections.Generic.List<CinemachineFreeLookModifier.Modifier>(buffer.Length);
            }
            else
            {
                managedModifiers.Clear();
                if (managedModifiers.Capacity < buffer.Length)
                {
                    managedModifiers.Capacity = buffer.Length;
                }
            }

            var owner = modifier.ComponentOwner;
            foreach (var entry in buffer)
            {
                var newModifier = CreateModifier(entry);
                if (newModifier == null)
                {
                    continue;
                }

                managedModifiers.Add(newModifier);
                if (owner != null)
                {
                    newModifier.RefreshCache(owner);
                }
            }

            modifier.Modifiers = managedModifiers;
        }

        private static unsafe void RotateWithFollowTargetChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments =
                ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMRotateWithFollowTarget>>(argumentsPtr, argumentsSize);

            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            bridge.Q<CinemachineRotateWithFollowTarget>().Damping = component.Damping;
        }

        private static unsafe void HardLockToTargetChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMHardLockToTarget>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            bridge.Q<CinemachineHardLockToTarget>().Damping = component.Damping;
        }

        private static unsafe void HardLookAtChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMHardLookAt>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            bridge.Q<CinemachineHardLookAt>().LookAtOffset = new Vector3(component.LookAtOffset.x, component.LookAtOffset.y, component.LookAtOffset.z);
        }

        private static unsafe void POVCameraChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMPanTilt>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var panTilt = bridge.Q<CinemachinePanTilt>();
            panTilt.ReferenceFrame = component.ReferenceFrame;
            panTilt.RecenterTarget = component.RecenterTarget;
            panTilt.PanAxis = component.PanAxis;
            panTilt.TiltAxis = component.TiltAxis;
        }

        private static unsafe void BasicMultiChannelPerlinChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments =
                ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMBasicMultiChannelPerlin>>(argumentsPtr, argumentsSize);

            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var noise = bridge.Q<CinemachineBasicMultiChannelPerlin>();
            noise.PivotOffset = component.PivotOffset;
            noise.AmplitudeGain = component.AmplitudeGain;
            noise.FrequencyGain = component.FrequencyGain;
            noise.NoiseProfile = component.NoiseProfile.Value;
        }

        private static unsafe void GroupFramingChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMGroupFraming>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var framing = bridge.Q<CinemachineGroupFraming>();
            framing.FramingMode = component.FramingMode;
            framing.FramingSize = component.FramingSize;
            framing.CenterOffset = new Vector2(component.CenterOffset.x, component.CenterOffset.y);
            framing.Damping = component.Damping;
            framing.SizeAdjustment = component.SizeAdjustment;
            framing.LateralAdjustment = component.LateralAdjustment;
            framing.FovRange = new Vector2(component.FovRange.x, component.FovRange.y);
            framing.DollyRange = new Vector2(component.DollyRange.x, component.DollyRange.y);
            framing.OrthoSizeRange = new Vector2(component.OrthoSizeRange.x, component.OrthoSizeRange.y);
        }

        private static unsafe void FollowZoomChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMFollowZoom>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var zoom = bridge.Q<CinemachineFollowZoom>();
            zoom.Width = component.Width;
            zoom.Damping = component.Damping;
            zoom.FovRange = new Vector2(component.FovRange.x, component.FovRange.y);
        }

        private static unsafe void CameraOffsetChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMCameraOffset>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var offset = bridge.Q<CinemachineCameraOffset>();
            offset.Offset = new Vector3(component.Offset.x, component.Offset.y, component.Offset.z);
            offset.ApplyAfter = component.ApplyAfter;
            offset.PreserveComposition = component.PreserveComposition;
        }

        private static unsafe void RecomposerChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMRecomposer>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var recomposer = bridge.Q<CinemachineRecomposer>();
            recomposer.ApplyAfter = component.ApplyAfter;
            recomposer.Tilt = component.Tilt;
            recomposer.Pan = component.Pan;
            recomposer.Dutch = component.Dutch;
            recomposer.ZoomScale = component.ZoomScale;
            recomposer.FollowAttachment = component.FollowAttachment;
            recomposer.LookAtAttachment = component.LookAtAttachment;
        }

        private static unsafe void VolumeSettingsFocusChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<VolumeSettingsFocusPayload>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.Bridge;
            ref readonly var component = ref arguments.Data;
            ref readonly var focusBridge = ref arguments.FocusBridge;
            ref var localToWorlds = ref arguments.LocalToWorlds;
            ref var bridgeObjects = ref arguments.BridgeObjects;

            Transform focusTarget = null;

            if (localToWorlds.HasComponent(component.FocusTarget) &&
                bridgeObjects.TryGetComponent(focusBridge.Value, out var focusTargetBridge) && focusTargetBridge.Value.IsValid())
            {
                focusTarget = focusTargetBridge.Value.Value.transform;
            }

            bridge.Q<CinemachineVolumeSettings>().FocusTarget = focusTarget;
        }

        private static unsafe void VolumeSettingsChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, CMVolumeSettings>>(argumentsPtr, argumentsSize);
            ref readonly var bridge = ref arguments.First;
            ref readonly var component = ref arguments.Second;
            var volumeSettings = bridge.Q<CinemachineVolumeSettings>();
            volumeSettings.Profile = component.Profile.Value;
            volumeSettings.Weight = component.Weight;
            volumeSettings.FocusTracking = component.FocusTracking;
            volumeSettings.FocusOffset = component.FocusOffset;
        }

        private static Transform ResolveTargetTransform(bool hasTarget, in BridgeObject sourceTarget, in BridgeObject proxyTarget)
        {
            if (!hasTarget)
            {
                return null;
            }

            if (sourceTarget.Value.IsValid())
            {
                return sourceTarget.Value.Value.transform;
            }

            return proxyTarget.Value.IsValid() ? proxyTarget.Value.Value.transform : null;
        }

#if UNITY_PHYSICS
        private static World FindWorld(ulong worldSequenceNumber)
        {
            foreach (var world in World.All)
            {
                if (world.SequenceNumber == worldSequenceNumber)
                {
                    return world;
                }
            }

            return null;
        }
#endif

#if UNITY_SPLINES
        private static SplineAutoDolly.ISplineAutoDolly CreateAutoDolly(in CMSplineAutoDolly autoDolly)
        {
            switch (autoDolly.Type)
            {
                case CMSplineAutoDollyType.FixedSpeed:
                    return new SplineAutoDolly.FixedSpeed
                    {
                        Speed = autoDolly.FixedSpeed,
                    };
                case CMSplineAutoDollyType.NearestPointToTarget:
                    return new SplineAutoDolly.NearestPointToTarget
                    {
                        PositionOffset = autoDolly.PositionOffset,
                        SearchResolution = math.max(1, autoDolly.SearchResolution),
                        SearchIteration = math.max(1, autoDolly.SearchIteration),
                    };
                default:
                    return null;
            }
        }
#endif

        private static CinemachineFreeLookModifier.Modifier CreateModifier(in CMFreeLookModifierEntry entry)
        {
            switch (entry.Type)
            {
                case CMFreeLookModifierType.Tilt:
                    return new CinemachineFreeLookModifier.TiltModifier
                    {
                        Tilt = new CinemachineFreeLookModifier.TopBottomRigs<float>
                        {
                            Top = entry.TiltTop,
                            Bottom = entry.TiltBottom,
                        },
                    };
                case CMFreeLookModifierType.Lens:
                    return new CinemachineFreeLookModifier.LensModifier
                    {
                        Top = ToLensSettings(entry.LensTop),
                        Bottom = ToLensSettings(entry.LensBottom),
                    };
                case CMFreeLookModifierType.PositionDamping:
                    return new CinemachineFreeLookModifier.PositionDampingModifier
                    {
                        Damping = new CinemachineFreeLookModifier.TopBottomRigs<Vector3>
                        {
                            Top = new Vector3(entry.PositionDampingTop.x, entry.PositionDampingTop.y, entry.PositionDampingTop.z),
                            Bottom = new Vector3(entry.PositionDampingBottom.x, entry.PositionDampingBottom.y, entry.PositionDampingBottom.z),
                        },
                    };
                case CMFreeLookModifierType.Composition:
                    return new CinemachineFreeLookModifier.CompositionModifier
                    {
                        Composition = new CinemachineFreeLookModifier.TopBottomRigs<ScreenComposerSettings>
                        {
                            Top = entry.CompositionTop,
                            Bottom = entry.CompositionBottom,
                        },
                    };
                case CMFreeLookModifierType.Distance:
                    return new CinemachineFreeLookModifier.DistanceModifier
                    {
                        Distance = new CinemachineFreeLookModifier.TopBottomRigs<float>
                        {
                            Top = entry.DistanceTop,
                            Bottom = entry.DistanceBottom,
                        },
                    };
                case CMFreeLookModifierType.Noise:
                    return new CinemachineFreeLookModifier.NoiseModifier
                    {
                        Noise = new CinemachineFreeLookModifier.TopBottomRigs<CinemachineFreeLookModifier.NoiseModifier.NoiseSettings>
                        {
                            Top = new CinemachineFreeLookModifier.NoiseModifier.NoiseSettings
                            {
                                Amplitude = entry.NoiseTop.Amplitude,
                                Frequency = entry.NoiseTop.Frequency,
                            },
                            Bottom = new CinemachineFreeLookModifier.NoiseModifier.NoiseSettings
                            {
                                Amplitude = entry.NoiseBottom.Amplitude,
                                Frequency = entry.NoiseBottom.Frequency,
                            },
                        },
                    };
                default:
                    return null;
            }
        }

        private static LensSettings ToLensSettings(in CMFreeLookModifierLensSettings source)
        {
            var lens = LensSettings.Default;
            lens.FieldOfView = source.FieldOfView;
            lens.OrthographicSize = source.OrthographicSize;
            lens.NearClipPlane = source.NearClipPlane;
            lens.FarClipPlane = source.FarClipPlane;
            lens.Dutch = source.Dutch;
            lens.ModeOverride = source.ModeOverride;
            lens.PhysicalProperties = new LensSettings.PhysicalSettings
            {
                GateFit = source.PhysicalProperties.GateFit,
                SensorSize = new Vector2(source.PhysicalProperties.SensorSize.x, source.PhysicalProperties.SensorSize.y),
                LensShift = new Vector2(source.PhysicalProperties.LensShift.x, source.PhysicalProperties.LensShift.y),
                FocusDistance = source.PhysicalProperties.FocusDistance,
                Iso = source.PhysicalProperties.Iso,
                ShutterSpeed = source.PhysicalProperties.ShutterSpeed,
                Aperture = source.PhysicalProperties.Aperture,
                BladeCount = source.PhysicalProperties.BladeCount,
                Curvature = new Vector2(source.PhysicalProperties.Curvature.x, source.PhysicalProperties.Curvature.y),
                BarrelClipping = source.PhysicalProperties.BarrelClipping,
                Anamorphism = source.PhysicalProperties.Anamorphism,
            };

            return lens;
        }

        private struct CameraTargetState
        {
            public bool HasTrackingTarget;
            public bool HasLookAtTarget;
            public BridgeObject TrackingTargetSource;
            public BridgeObject TrackingTargetProxy;
            public BridgeObject LookAtTargetSource;
            public BridgeObject LookAtTargetProxy;
        }

#if UNITY_PHYSICS
        private struct ThirdPersonFollowContext
        {
            public Entity PhysicsWorldEntity;
            public ulong WorldSequenceNumber;
        }
#endif

        private struct VolumeSettingsFocusPayload
        {
            public BridgeObject Bridge;
            public CMVolumeSettings Data;
            public CMVolumeSettingsFocusBridge FocusBridge;
            public ComponentLookup<LocalToWorld> LocalToWorlds;
            public ComponentLookup<BridgeObject> BridgeObjects;
        }

#if UNITY_SPLINES
        private struct SplineDollyLookAtTargetsPayload
        {
            public BridgeObject Bridge;
            public CMSplineDollyLookAtTargets Data;
            public DynamicBuffer<CMSplineDollyLookAtTarget> Targets;
            public DynamicBuffer<CMSplineDollyLookAtTargetBridge> TargetBridges;
            public ComponentLookup<LocalToWorld> LocalToWorlds;
            public ComponentLookup<BridgeObject> BridgeObjects;
        }
#endif

        private static class Burst
        {
            public static readonly SharedStatic<BurstTrampoline> Brain = SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMBrain>();

            public static readonly SharedStatic<BurstTrampoline> CameraEnabled =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMCameraEnabled>();

            public static readonly SharedStatic<BurstTrampoline> Camera = SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMCamera>();

            public static readonly SharedStatic<BurstTrampoline> CameraTargets =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMCameraTargetBridgeObjects>();

            public static readonly SharedStatic<BurstTrampoline> Follow = SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMFollow>();

            public static readonly SharedStatic<BurstTrampoline> PositionComposer =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMPositionComposer>();

            public static readonly SharedStatic<BurstTrampoline> RotationComposer =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMRotationComposer>();

#if UNITY_PHYSICS
            public static readonly SharedStatic<BurstTrampoline> ThirdPersonFollow =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMThirdPersonFollow>();
#endif

#if UNITY_SPLINES
            public static readonly SharedStatic<BurstTrampoline> SplineDolly =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMSplineDolly>();

            public static readonly SharedStatic<BurstTrampoline> SplineDollyTarget =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMSplineDollyTarget>();

            public static readonly SharedStatic<BurstTrampoline> SplineDollyLookAtTargets =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMSplineDollyLookAtTargets>();
#endif

            public static readonly SharedStatic<BurstTrampoline> OrbitFollow =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMOrbitFollow>();

            public static readonly SharedStatic<BurstTrampoline> FreeLookModifier =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMFreeLookModifier>();

            public static readonly SharedStatic<BurstTrampoline> RotateWithFollowTarget =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMRotateWithFollowTarget>();

            public static readonly SharedStatic<BurstTrampoline> HardLockToTarget =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMHardLockToTarget>();

            public static readonly SharedStatic<BurstTrampoline> HardLookAt = SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMHardLookAt>();

            public static readonly SharedStatic<BurstTrampoline> PanTilt = SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMPanTilt>();

            public static readonly SharedStatic<BurstTrampoline> BasicMultiChannelPerlin =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMBasicMultiChannelPerlin>();

            public static readonly SharedStatic<BurstTrampoline> GroupFraming =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMGroupFraming>();

            public static readonly SharedStatic<BurstTrampoline> FollowZoom = SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMFollowZoom>();

            public static readonly SharedStatic<BurstTrampoline> CameraOffset =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMCameraOffset>();

            public static readonly SharedStatic<BurstTrampoline> Recomposer = SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMRecomposer>();

            public static readonly SharedStatic<BurstTrampoline> VolumeSettingsFocus =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMVolumeSettingsFocusBridge>();

            public static readonly SharedStatic<BurstTrampoline> VolumeSettings =
                SharedStatic<BurstTrampoline>.GetOrCreate<CinemachineSyncSystem, CMVolumeSettings>();
        }
    }
}
#endif
