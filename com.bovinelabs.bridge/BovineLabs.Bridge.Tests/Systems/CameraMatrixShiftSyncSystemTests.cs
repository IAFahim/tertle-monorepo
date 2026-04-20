// <copyright file="CameraMatrixShiftSyncSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Camera;
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    public class CameraMatrixShiftSyncSystemTests : ECSTestsFixture
    {
        private SystemHandle system;
        private GameObject cameraObject;
        private GameObject otherCameraObject;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<CameraMatrixShiftSyncSystem>();
        }

        public override void TearDown()
        {
            if (this.cameraObject != null)
            {
                Object.DestroyImmediate(this.cameraObject);
            }

            if (this.otherCameraObject != null)
            {
                Object.DestroyImmediate(this.otherCameraObject);
            }

            base.TearDown();
        }

        [Test]
        public void Update_WhenOffsetChanges_AppliesAndResetsProjectionMatrix()
        {
            this.cameraObject = new GameObject("CameraMatrixShiftSyncSystemTests_Camera", typeof(Camera));
            var camera = this.cameraObject.GetComponent<Camera>();
            var original = camera.projectionMatrix;

            var entity = this.Manager.CreateEntity(typeof(CameraBridge), typeof(CameraViewSpaceOffset));
            this.Manager.SetComponentData(entity, new CameraBridge { Value = camera });
            this.Manager.SetComponentData(entity, new CameraViewSpaceOffset { ProjectionCenterOffset = new float2(0.2f, -0.1f) });

            this.system.Update(this.WorldUnmanaged);
            var shifted = camera.projectionMatrix;
            Assert.IsFalse(this.AreEqual(original, shifted));

            this.Manager.SetComponentData(entity, new CameraViewSpaceOffset { ProjectionCenterOffset = float2.zero });
            this.system.Update(this.WorldUnmanaged);

            Assert.IsTrue(this.AreEqual(original, camera.projectionMatrix));
        }

        [Test]
        public void Update_WhenEntryHasNullCamera_DoesNotAbortOtherEntries()
        {
            this.cameraObject = new GameObject("CameraMatrixShiftSyncSystemTests_Camera", typeof(Camera));
            this.otherCameraObject = new GameObject("CameraMatrixShiftSyncSystemTests_OtherCamera", typeof(Camera));
            var camera = this.otherCameraObject.GetComponent<Camera>();
            var original = camera.projectionMatrix;

            var invalidEntity = this.Manager.CreateEntity(typeof(CameraBridge), typeof(CameraViewSpaceOffset));
            this.Manager.SetComponentData(invalidEntity, new CameraBridge());
            this.Manager.SetComponentData(invalidEntity, new CameraViewSpaceOffset { ProjectionCenterOffset = new float2(0.1f, 0.1f) });

            var validEntity = this.Manager.CreateEntity(typeof(CameraBridge), typeof(CameraViewSpaceOffset));
            this.Manager.SetComponentData(validEntity, new CameraBridge { Value = camera });
            this.Manager.SetComponentData(validEntity, new CameraViewSpaceOffset { ProjectionCenterOffset = new float2(-0.15f, 0.05f) });

            Assert.DoesNotThrow(() => this.system.Update(this.WorldUnmanaged));

            Assert.IsFalse(this.AreEqual(original, camera.projectionMatrix));
        }

        private bool AreEqual(Matrix4x4 a, Matrix4x4 b)
        {
            const float eps = 0.0001f;
            return Mathf.Abs(a.m00 - b.m00) < eps &&
                Mathf.Abs(a.m02 - b.m02) < eps &&
                Mathf.Abs(a.m11 - b.m11) < eps &&
                Mathf.Abs(a.m12 - b.m12) < eps;
        }
    }
}
