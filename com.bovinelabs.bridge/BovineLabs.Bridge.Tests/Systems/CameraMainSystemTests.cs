// <copyright file="CameraMainSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Systems
{
    using System.Collections.Generic;
    using BovineLabs.Bridge.Camera;
    using BovineLabs.Bridge.Data.Camera;
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
#endif
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEngine;
#if UNITY_CINEMACHINE
    using Unity.Cinemachine;
#endif

    public class CameraMainSystemTests : ECSTestsFixture
    {
        private CameraMainSystem system;
        private GameObject cameraObject;
        private List<GameObject> previousMainCameras;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystemManaged<CameraMainSystem>();

            this.previousMainCameras = RetagMainCamerasToUntagged();
            this.cameraObject = new GameObject("CameraMainSystemTests_MainCamera", typeof(Camera));
#if UNITY_CINEMACHINE
            this.cameraObject.AddComponent<CinemachineBrain>();
#endif
            this.cameraObject.tag = "MainCamera";
        }

        public override void TearDown()
        {
            if (this.cameraObject != null)
            {
                Object.DestroyImmediate(this.cameraObject);
            }

            RestoreMainCameraTags(this.previousMainCameras);
            base.TearDown();
        }

        [Test]
        public void Update_WithoutMainEntity_CreatesAndAssignsMainCameraEntity()
        {
            this.cameraObject.transform.position = new Vector3(3f, 2f, 1f);

            this.system.Update();

            var query = this.Manager.CreateEntityQuery(typeof(CameraMain), typeof(CameraBridge), typeof(LocalTransform));
            Assert.AreEqual(1, query.CalculateEntityCount());

            var entity = query.GetSingletonEntity();
            var bridge = this.Manager.GetComponentData<CameraBridge>(entity);
            var transform = this.Manager.GetComponentData<LocalTransform>(entity);

            Assert.AreEqual(this.cameraObject.GetComponent<Camera>(), bridge.Value.Value);
            Assert.That(transform.Position.x, Is.EqualTo(3f).Within(0.001f));
            Assert.That(transform.Position.y, Is.EqualTo(2f).Within(0.001f));
            Assert.That(transform.Position.z, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Update_WithExistingEntity_RefreshesTransformFromCamera()
        {
            this.cameraObject.transform.position = new Vector3(9f, 8f, 7f);

            var entity = this.Manager.CreateEntity(
                typeof(CameraMain),
                typeof(CameraBridge),
                typeof(LocalTransform),
                typeof(CameraFrustumPlanes),
                typeof(CameraFrustumCorners));
#if UNITY_CINEMACHINE
            this.Manager.AddComponent<CMBrain>(entity);
            this.Manager.AddComponent<CinemachineBrainBridge>(entity);
#endif
            this.Manager.SetComponentData(entity, new CameraBridge());
            this.Manager.SetComponentData(entity, LocalTransform.Identity);

            this.system.Update();

            var bridge = this.Manager.GetComponentData<CameraBridge>(entity);
            var transform = this.Manager.GetComponentData<LocalTransform>(entity);

            Assert.AreEqual(this.cameraObject.GetComponent<Camera>(), bridge.Value.Value);
            Assert.That(transform.Position.x, Is.EqualTo(9f).Within(0.001f));
            Assert.That(transform.Position.y, Is.EqualTo(8f).Within(0.001f));
            Assert.That(transform.Position.z, Is.EqualTo(7f).Within(0.001f));
        }

        private static List<GameObject> RetagMainCamerasToUntagged()
        {
            var previousMainCameras = new List<GameObject>();
            var cameras = Camera.allCameras;
            foreach (var c in cameras)
            {
                var go = c.gameObject;
                if (!go.CompareTag("MainCamera"))
                {
                    continue;
                }

                previousMainCameras.Add(go);
                go.tag = "Untagged";
            }

            return previousMainCameras;
        }

        private static void RestoreMainCameraTags(IEnumerable<GameObject> cameras)
        {
            foreach (var go in cameras)
            {
                if (go != null)
                {
                    go.tag = "MainCamera";
                }
            }
        }
    }
}
