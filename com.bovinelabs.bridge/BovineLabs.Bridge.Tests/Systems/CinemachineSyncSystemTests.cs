// <copyright file="CinemachineSyncSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Tests.Systems
{
    using System.Collections.Generic;
    using BovineLabs.Bridge;
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Bridge.Data.Lighting;
#if UNITY_SPLINES
    using BovineLabs.Bridge.Data.Spline;
#endif
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Cinemachine;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;
    using UnityEngine.Rendering;
    using CinemachineSyncSystem = BovineLabs.Bridge.Camera.CinemachineSyncSystem;
    using CinemachineTargetBridgeSyncSystem = BovineLabs.Bridge.Camera.CinemachineTargetBridgeSyncSystem;
#if UNITY_SPLINES
    using UnityEngine.Splines;
#endif

    public class CinemachineSyncSystemTests : ECSTestsFixture
    {
        private readonly List<BlobAssetReference<BlobString>> nameBlobs = new();
        private BridgeCompanionSystem bridgeCompanionSystem;
        private SystemHandle bridgeTransformSyncSystem;
        private SystemHandle bridgeTypeSystem;
        private SystemHandle syncSystem;
        private SystemHandle targetBridgeSyncSystem;

        public override void Setup()
        {
            base.Setup();
            this.bridgeTypeSystem = this.World.CreateSystem<BridgeTypeSystem>();
            this.bridgeCompanionSystem = this.World.CreateSystemManaged<BridgeCompanionSystem>();
            this.targetBridgeSyncSystem = this.World.CreateSystem<CinemachineTargetBridgeSyncSystem>();
            this.bridgeTransformSyncSystem = this.World.CreateSystem<BridgeTransformSyncSystem>();
            this.syncSystem = this.World.CreateSystem<CinemachineSyncSystem>();
        }

        public override void TearDown()
        {
            base.TearDown();

            foreach (var blob in this.nameBlobs)
            {
                if (blob.IsCreated)
                {
                    blob.Dispose();
                }
            }

            this.nameBlobs.Clear();
        }

        [Test]
        public void Update_EmptyWorld_DoesNotThrow()
        {
            Assert.DoesNotThrow(this.UpdateSystems);
        }

        [Test]
        public void Update_CameraEnabledChanged_TogglesManagedCameraEnabled()
        {
            var entity = this.Manager.CreateEntity(typeof(CMCameraEnabled), typeof(CMCamera), typeof(CMCameraTargetBridgeObjects));
            this.Manager.SetComponentEnabled<CMCameraEnabled>(entity, false);
            this.Manager.SetComponentData(entity, new CMCamera { Name = this.CreateNameBlob("Blend Camera") });
            this.Manager.SetComponentData(entity, this.CreateTargetBridgeObjects());

            this.UpdateSystems();

            var camera = this.Manager.GetComponentData<BridgeObject>(entity).Q<CinemachineCamera>();
            Assert.IsFalse(camera.enabled);
            Assert.AreEqual("Blend Camera", camera.gameObject.name);
        }

        [Test]
        public void Update_CameraAndFollowComponents_CreateRuntimeBridgeObjectAndMapRepresentativeValues()
        {
            var trackingTarget = this.Manager.CreateEntity(typeof(LocalToWorld));
            this.Manager.SetComponentData(trackingTarget, new LocalToWorld { Value = float4x4.Translate(new float3(4f, 5f, 6f)) });

            var entity = this.Manager.CreateEntity(typeof(CMCameraEnabled), typeof(CMCamera), typeof(CMCameraTargetBridgeObjects), typeof(CMFollow));
            this.Manager.SetComponentData(entity, this.CreateTargetBridgeObjects());

            this.Manager.SetComponentData(entity, new CMCamera
            {
                Name = this.CreateNameBlob("Gameplay Camera"),
                TrackingTarget = trackingTarget,
                CustomLookAtTarget = false,
                Priority = (PrioritySettings)11,
                BlendHint = CinemachineCore.BlendHints.SphericalPosition,
                FieldOfView = 200f,
                NearClipPlane = 0.5f,
                FarClipPlane = 500f,
                Dutch = 12f,
            });
            this.Manager.SetComponentData(entity, new CMFollow
            {
                FollowOffset = new float3(1f, 2f, 3f),
            });

            this.UpdateSystems();

            var bridgeObject = this.Manager.GetComponentData<BridgeObject>(entity);
            var targetBridges = this.Manager.GetComponentData<CMCameraTargetBridgeObjects>(entity);
            var trackingBridge = this.Manager.GetComponentData<BridgeObject>(targetBridges.TrackingTargetBridge);
            var camera = bridgeObject.Q<CinemachineCamera>();
            var follow = bridgeObject.Q<CinemachineFollow>();
            Assert.AreEqual(UnityComponentType.Cinemachine, bridgeObject.Type.Types);
            Assert.AreEqual(CMCameraRuntimeType.Camera | CMCameraRuntimeType.Follow, bridgeObject.Type.Cinemachine);
            Assert.AreSame(camera.gameObject, follow.gameObject);
            Assert.IsTrue(this.Manager.HasComponent<BridgeObject>(targetBridges.TrackingTargetBridge));
            Assert.That(trackingBridge.Value.Value.transform.position.x, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(trackingBridge.Value.Value.transform.position.y, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(trackingBridge.Value.Value.transform.position.z, Is.EqualTo(6f).Within(0.0001f));
            Assert.AreSame(trackingBridge.Value.Value.transform, camera.Target.TrackingTarget);

            Assert.AreEqual(11, camera.Priority.Value);
            Assert.AreEqual(CinemachineCore.BlendHints.SphericalPosition, camera.BlendHint);
            Assert.AreEqual("Gameplay Camera", camera.gameObject.name);
            Assert.That(camera.Lens.FieldOfView, Is.EqualTo(179f).Within(0.0001f));
            Assert.That(camera.Lens.NearClipPlane, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(camera.Lens.FarClipPlane, Is.EqualTo(500f).Within(0.0001f));
            Assert.That(camera.Lens.Dutch, Is.EqualTo(12f).Within(0.0001f));

            Assert.AreEqual(new Vector3(1f, 2f, 3f), follow.FollowOffset);
        }

        [Test]
        public void Update_BrainChanged_DoesNotAbortWhenAnEntryIsInvalid()
        {
            var go = new GameObject("CinemachineSyncSystemTests_Brain", typeof(CinemachineBrain));

            try
            {
                var validBrain = go.GetComponent<CinemachineBrain>();

                var invalidEntity = this.Manager.CreateEntity(typeof(CMBrain), typeof(CinemachineBrainBridge));
                this.Manager.SetComponentData(invalidEntity, new CMBrain
                {
                    IgnoreTimeScale = false,
                    UpdateMethod = (CinemachineBrain.UpdateMethods)0,
                    BlendUpdateMethod = (CinemachineBrain.BrainUpdateMethods)0,
                    DefaultBlend = new CMBlendDefinition { Style = CinemachineBlendDefinition.Styles.EaseInOut, Time = 0.2f },
                });

                var validEntity = this.Manager.CreateEntity(typeof(CMBrain), typeof(CinemachineBrainBridge));
                this.Manager.SetComponentData(validEntity, new CinemachineBrainBridge { Value = validBrain });
                this.Manager.SetComponentData(validEntity, new CMBrain
                {
                    IgnoreTimeScale = true,
                    UpdateMethod = (CinemachineBrain.UpdateMethods)1,
                    BlendUpdateMethod = (CinemachineBrain.BrainUpdateMethods)1,
                    DefaultBlend = new CMBlendDefinition { Style = CinemachineBlendDefinition.Styles.HardIn, Time = 1.2f },
                });

                this.UpdateSystems();

                Assert.IsTrue(validBrain.IgnoreTimeScale);
                Assert.AreEqual((CinemachineBrain.UpdateMethods)1, validBrain.UpdateMethod);
                Assert.AreEqual((CinemachineBrain.BrainUpdateMethods)1, validBrain.BlendUpdateMethod);
                Assert.AreEqual(CinemachineBlendDefinition.Styles.HardIn, validBrain.DefaultBlend.Style);
                Assert.That(validBrain.DefaultBlend.Time, Is.EqualTo(1.2f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Update_WithCustomLookAtTarget_SyncsTrackingAndLookAtTransforms()
        {
            var trackingTarget = this.Manager.CreateEntity(typeof(LocalToWorld));
            var lookAtTarget = this.Manager.CreateEntity(typeof(LocalToWorld));
            this.Manager.SetComponentData(trackingTarget, new LocalToWorld { Value = float4x4.Translate(new float3(1f, 2f, 3f)) });
            this.Manager.SetComponentData(lookAtTarget, new LocalToWorld { Value = float4x4.Translate(new float3(4f, 5f, 6f)) });

            var entity = this.Manager.CreateEntity(typeof(CMCameraEnabled), typeof(CMCamera), typeof(CMCameraTargetBridgeObjects));
            this.Manager.SetComponentData(entity, this.CreateTargetBridgeObjects());
            this.Manager.SetComponentData(entity, new CMCamera
            {
                Name = this.CreateNameBlob("LookAt Camera"),
                TrackingTarget = trackingTarget,
                LookAtTarget = lookAtTarget,
                CustomLookAtTarget = true,
            });

            this.UpdateSystems();

            var targetBridges = this.Manager.GetComponentData<CMCameraTargetBridgeObjects>(entity);
            var trackingBridge = this.Manager.GetComponentData<BridgeObject>(targetBridges.TrackingTargetBridge);
            var lookAtBridge = this.Manager.GetComponentData<BridgeObject>(targetBridges.LookAtTargetBridge);
            var camera = this.Manager.GetComponentData<BridgeObject>(entity).Q<CinemachineCamera>();
            Assert.IsTrue(camera.Target.CustomLookAtTarget);
            Assert.AreSame(trackingBridge.Value.Value.transform, camera.Target.TrackingTarget);
            Assert.AreSame(lookAtBridge.Value.Value.transform, camera.Target.LookAtTarget);
            Assert.That(trackingBridge.Value.Value.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(lookAtBridge.Value.Value.transform.position, Is.EqualTo(new Vector3(4f, 5f, 6f)));
        }

        [Test]
        public void Update_WithBridgedTrackingTarget_UsesSourceBridgeObjectTransform()
        {
            var trackingTarget = this.Manager.CreateEntity(typeof(LocalToWorld), typeof(LightData));
            this.Manager.SetComponentData(trackingTarget, new LocalToWorld { Value = float4x4.Translate(new float3(9f, 8f, 7f)) });
            this.Manager.SetComponentData(trackingTarget, new LightData { Intensity = 2f });

            var entity = this.Manager.CreateEntity(typeof(CMCameraEnabled), typeof(CMCamera), typeof(CMCameraTargetBridgeObjects));
            this.Manager.SetComponentData(entity, this.CreateTargetBridgeObjects());
            this.Manager.SetComponentData(entity, new CMCamera
            {
                Name = this.CreateNameBlob("Bridge Target Camera"),
                TrackingTarget = trackingTarget,
            });

            this.UpdateSystems();

            var targetBridges = this.Manager.GetComponentData<CMCameraTargetBridgeObjects>(entity);
            var sourceBridge = this.Manager.GetComponentData<BridgeObject>(trackingTarget);
            var proxyBridge = this.Manager.GetComponentData<BridgeObject>(targetBridges.TrackingTargetBridge);
            var camera = this.Manager.GetComponentData<BridgeObject>(entity).Q<CinemachineCamera>();
            Assert.AreSame(sourceBridge.Value.Value.transform, camera.Target.TrackingTarget);
            Assert.AreNotSame(proxyBridge.Value.Value.transform, camera.Target.TrackingTarget);
            Assert.That(sourceBridge.Value.Value.transform.position, Is.EqualTo(new Vector3(9f, 8f, 7f)));
        }

        [Test]
        public void Update_VolumeSettingsChanged_SyncsFocusTargetTransform()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            try
            {
                var focusTarget = this.Manager.CreateEntity(typeof(LocalToWorld));
                var focusTargetBridge = this.CreateHelperTargetBridge();
                this.Manager.SetComponentData(focusTarget, new LocalToWorld { Value = float4x4.Translate(new float3(7f, 8f, 9f)) });

                var entity = this.Manager.CreateEntity(
                    typeof(CMCameraEnabled),
                    typeof(CMCamera),
                    typeof(CMCameraTargetBridgeObjects),
                    typeof(CMVolumeSettings),
                    typeof(CMVolumeSettingsFocusBridge));
                this.Manager.SetComponentData(entity, new CMCamera { Name = this.CreateNameBlob("Volume Camera") });
                this.Manager.SetComponentData(entity, this.CreateTargetBridgeObjects());
                this.Manager.SetComponentData(entity, new CMVolumeSettingsFocusBridge { Value = focusTargetBridge });

                this.Manager.SetComponentData(entity, new CMVolumeSettings
                {
                    Weight = 0.75f,
                    FocusTracking = CinemachineVolumeSettings.FocusTrackingMode.LookAtTarget,
                    FocusTarget = focusTarget,
                    FocusOffset = 1.5f,
                    Profile = profile,
                });

                this.UpdateSystems();

                var bridge = this.Manager.GetComponentData<BridgeObject>(focusTargetBridge);
                var settings = this.Manager.GetComponentData<BridgeObject>(entity).Q<CinemachineVolumeSettings>();
                Assert.That(bridge.Value.Value.transform.position, Is.EqualTo(new Vector3(7f, 8f, 9f)));
                Assert.AreEqual(profile, settings.Profile);
                Assert.That(settings.Weight, Is.EqualTo(0.75f).Within(0.0001f));
                Assert.AreEqual(CinemachineVolumeSettings.FocusTrackingMode.LookAtTarget, settings.FocusTracking);
                Assert.That(settings.FocusOffset, Is.EqualTo(1.5f).Within(0.0001f));
                Assert.AreSame(bridge.Value.Value.transform, settings.FocusTarget);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void Update_FreeLookModifierEntries_RebuildsModifiersAndSkipsInvalidType()
        {
            var entity = this.Manager.CreateEntity(typeof(CMCameraEnabled), typeof(CMCamera), typeof(CMCameraTargetBridgeObjects), typeof(CMFreeLookModifier));
            this.Manager.SetComponentData(entity, new CMCamera { Name = this.CreateNameBlob("Modifier Camera") });
            this.Manager.SetComponentData(entity, this.CreateTargetBridgeObjects());
            this.Manager.SetComponentData(entity, new CMFreeLookModifier { Easing = 0.25f });

            var buffer = this.Manager.AddBuffer<CMFreeLookModifierEntry>(entity);
            buffer.Add(new CMFreeLookModifierEntry
            {
                Type = CMFreeLookModifierType.Tilt,
                TiltTop = 15f,
                TiltBottom = -10f,
            });
            buffer.Add(new CMFreeLookModifierEntry
            {
                Type = (CMFreeLookModifierType)99,
            });

            this.UpdateSystems();

            var freeLookModifier = this.Manager.GetComponentData<BridgeObject>(entity).Q<CinemachineFreeLookModifier>();
            Assert.That(freeLookModifier.Easing, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.IsNotNull(freeLookModifier.Modifiers);
            Assert.AreEqual(1, freeLookModifier.Modifiers.Count);
            Assert.IsInstanceOf<CinemachineFreeLookModifier.TiltModifier>(freeLookModifier.Modifiers[0]);
        }

#if UNITY_PHYSICS
        [Test]
        public void Update_ThirdPersonFollowChanged_SyncsBridgeComponentValues()
        {
            var entity = this.Manager.CreateEntity(typeof(CMCameraEnabled), typeof(CMCamera), typeof(CMCameraTargetBridgeObjects), typeof(CMThirdPersonFollow));
            this.Manager.SetComponentData(entity, new CMCamera { Name = this.CreateNameBlob("Third Person Camera") });
            this.Manager.SetComponentData(entity, this.CreateTargetBridgeObjects());
            this.Manager.SetComponentData(entity, new CMThirdPersonFollow
            {
                Damping = new float3(1f, 2f, 3f),
                ShoulderOffset = new float3(4f, 5f, 6f),
                VerticalArmLength = 7f,
                CameraSide = 2f,
                CameraDistance = 8f,
                AvoidObstacles = new CinemachineThirdPersonFollowDots.ObstacleSettings
                {
                    Enabled = true,
                    CameraRadius = 0.25f,
                    DampingIntoCollision = 0.5f,
                    DampingFromCollision = 0.75f,
                },
            });

            this.UpdateSystems();

            var thirdPerson = this.Manager.GetComponentData<BridgeObject>(entity).Q<CinemachineThirdPersonFollowDots>();
            Assert.AreEqual(new Vector3(1f, 2f, 3f), thirdPerson.Damping);
            Assert.AreEqual(new Vector3(4f, 5f, 6f), thirdPerson.ShoulderOffset);
            Assert.That(thirdPerson.VerticalArmLength, Is.EqualTo(7f).Within(0.0001f));
            Assert.That(thirdPerson.CameraSide, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(thirdPerson.CameraDistance, Is.EqualTo(8f).Within(0.0001f));
            Assert.IsTrue(thirdPerson.AvoidObstacles.Enabled);
            Assert.That(thirdPerson.AvoidObstacles.CameraRadius, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(thirdPerson.AvoidObstacles.DampingIntoCollision, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(thirdPerson.AvoidObstacles.DampingFromCollision, Is.EqualTo(0.75f).Within(0.0001f));
        }
#endif

#if UNITY_SPLINES
        [Test]
        public void Update_SplineDollyChanged_SyncsBridgeComponentValues()
        {
            var splineEntity = this.Manager.CreateEntity(typeof(AddSplineBridge));

            var entity = this.Manager.CreateEntity(
                typeof(CMCameraEnabled),
                typeof(CMCamera),
                typeof(CMCameraTargetBridgeObjects),
                typeof(CMSplineDolly),
                typeof(CMSplineDollyTarget));
            this.Manager.SetComponentData(entity, new CMCamera { Name = this.CreateNameBlob("Spline Camera") });
            this.Manager.SetComponentData(entity, this.CreateTargetBridgeObjects());
            this.Manager.SetComponentData(entity, new CMSplineDolly
            {
                Position = 3.5f,
                PositionUnits = PathIndexUnit.Distance,
                SplineOffset = new float3(1f, 2f, 3f),
                CameraRotation = CinemachineSplineDolly.RotationMode.FollowTargetNoRoll,
                Damping = new CMSplineDollyDamping
                {
                    Enabled = true,
                    Position = new float3(4f, 5f, 6f),
                    Angular = 7f,
                },
                AutoDolly = new CMSplineAutoDolly
                {
                    Enabled = true,
                    Type = CMSplineAutoDollyType.FixedSpeed,
                    FixedSpeed = 2.5f,
                },
            });
            this.Manager.SetComponentData(entity, new CMSplineDollyTarget { Spline = splineEntity });

            this.UpdateSystems();

            var splineBridge = this.Manager.GetComponentData<BridgeObject>(splineEntity);
            var dolly = this.Manager.GetComponentData<BridgeObject>(entity).Q<CinemachineSplineDolly>();
            Assert.AreSame(splineBridge.Q<SplineContainer>(), dolly.Spline);
            Assert.That(dolly.SplineSettings.Position, Is.EqualTo(3.5f).Within(0.0001f));
            Assert.AreEqual(PathIndexUnit.Distance, dolly.SplineSettings.Units);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), dolly.SplineOffset);
            Assert.AreEqual(CinemachineSplineDolly.RotationMode.FollowTargetNoRoll, dolly.CameraRotation);
            Assert.IsTrue(dolly.Damping.Enabled);
            Assert.AreEqual(new Vector3(4f, 5f, 6f), dolly.Damping.Position);
            Assert.That(dolly.Damping.Angular, Is.EqualTo(7f).Within(0.0001f));
            Assert.IsTrue(dolly.AutomaticDolly.Enabled);
            Assert.IsInstanceOf<SplineAutoDolly.FixedSpeed>(dolly.AutomaticDolly.Method);
            Assert.That(((SplineAutoDolly.FixedSpeed)dolly.AutomaticDolly.Method).Speed, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void Update_SplineDollyLookAtTargetsChanged_SyncsBridgeComponentValues()
        {
            var firstLookAt = this.Manager.CreateEntity(typeof(LocalToWorld));
            var secondLookAt = this.Manager.CreateEntity(typeof(LocalToWorld));
            this.Manager.SetComponentData(firstLookAt, new LocalToWorld { Value = float4x4.Translate(new float3(1f, 2f, 3f)) });
            this.Manager.SetComponentData(secondLookAt, new LocalToWorld { Value = float4x4.Translate(new float3(4f, 5f, 6f)) });

            var entity = this.Manager.CreateEntity(
                typeof(CMCameraEnabled),
                typeof(CMCamera),
                typeof(CMCameraTargetBridgeObjects),
                typeof(CMSplineDollyLookAtTargets));
            this.Manager.SetComponentData(entity, new CMCamera { Name = this.CreateNameBlob("Spline LookAt Camera") });
            this.Manager.SetComponentData(entity, this.CreateTargetBridgeObjects());
            this.Manager.SetComponentData(entity, new CMSplineDollyLookAtTargets { PathIndexUnit = PathIndexUnit.Distance });

            var firstTargetBridge = this.CreateHelperTargetBridge();
            var secondTargetBridge = this.CreateHelperTargetBridge();
            this.Manager.AddBuffer<CMSplineDollyLookAtTarget>(entity);
            this.Manager.AddBuffer<CMSplineDollyLookAtTargetBridge>(entity);
            var targets = this.Manager.GetBuffer<CMSplineDollyLookAtTarget>(entity);
            var targetBridges = this.Manager.GetBuffer<CMSplineDollyLookAtTargetBridge>(entity);

            targets.Add(new CMSplineDollyLookAtTarget
            {
                Position = 2f,
                LookAt = firstLookAt,
                Offset = new float3(10f, 11f, 12f),
                Easing = 0.25f,
            });
            targetBridges.Add(new CMSplineDollyLookAtTargetBridge { Value = firstTargetBridge });

            targets.Add(new CMSplineDollyLookAtTarget
            {
                Position = 7f,
                LookAt = secondLookAt,
                Offset = new float3(13f, 14f, 15f),
                Easing = 0.75f,
            });
            targetBridges.Add(new CMSplineDollyLookAtTargetBridge { Value = secondTargetBridge });

            this.UpdateSystems();

            var firstBridge = this.Manager.GetComponentData<BridgeObject>(firstTargetBridge);
            var secondBridge = this.Manager.GetComponentData<BridgeObject>(secondTargetBridge);
            var lookAtTargets = this.Manager.GetComponentData<BridgeObject>(entity).Q<CinemachineSplineDollyLookAtTargets>().Targets;

            Assert.AreEqual(PathIndexUnit.Distance, lookAtTargets.PathIndexUnit);
            Assert.AreEqual(2, lookAtTargets.Count);

            Assert.That(lookAtTargets[0].Index, Is.EqualTo(2f).Within(0.0001f));
            Assert.AreSame(firstBridge.Value.Value.transform, lookAtTargets[0].Value.LookAt);
            Assert.AreEqual(new Vector3(10f, 11f, 12f), lookAtTargets[0].Value.Offset);
            Assert.That(lookAtTargets[0].Value.Easing, Is.EqualTo(0.25f).Within(0.0001f));

            Assert.That(lookAtTargets[1].Index, Is.EqualTo(7f).Within(0.0001f));
            Assert.AreSame(secondBridge.Value.Value.transform, lookAtTargets[1].Value.LookAt);
            Assert.AreEqual(new Vector3(13f, 14f, 15f), lookAtTargets[1].Value.Offset);
            Assert.That(lookAtTargets[1].Value.Easing, Is.EqualTo(0.75f).Within(0.0001f));
        }
#endif

        private void UpdateSystems()
        {
            this.bridgeTypeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
            this.bridgeCompanionSystem.Update();
            this.targetBridgeSyncSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
            this.bridgeTransformSyncSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
            this.syncSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }

        private CMCameraTargetBridgeObjects CreateTargetBridgeObjects()
        {
            var trackingTargetBridge = this.CreateHelperTargetBridge();
            var lookAtTargetBridge = this.CreateHelperTargetBridge();

            return new CMCameraTargetBridgeObjects
            {
                TrackingTargetBridge = trackingTargetBridge,
                LookAtTargetBridge = lookAtTargetBridge,
            };
        }

        private Entity CreateHelperTargetBridge()
        {
            var targetBridge = this.Manager.CreateEntity(typeof(LocalToWorld), typeof(CMCameraTargetBridgeObject));
            this.Manager.SetComponentData(targetBridge, new LocalToWorld { Value = float4x4.identity });
            return targetBridge;
        }

        private BlobAssetReference<BlobString> CreateNameBlob(string value)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<BlobString>();
            builder.AllocateString(ref root, value);
            var blob = builder.CreateBlobAssetReference<BlobString>(Allocator.Persistent);
            this.nameBlobs.Add(blob);
            return blob;
        }
    }
}
#endif
