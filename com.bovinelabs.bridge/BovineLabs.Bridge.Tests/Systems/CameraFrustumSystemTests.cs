// <copyright file="CameraFrustumSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Camera;
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using UnityEngine;

    public class CameraFrustumSystemTests : ECSTestsFixture
    {
        private CameraFrustumSystem system;
        private GameObject cameraObject;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystemManaged<CameraFrustumSystem>();
        }

        public override void TearDown()
        {
            if (this.cameraObject != null)
            {
                Object.DestroyImmediate(this.cameraObject);
            }

            base.TearDown();
        }

        [Test]
        public void Update_WithValidCamera_WritesPlanesAndCorners()
        {
            this.cameraObject = new GameObject("CameraFrustumSystemTests_Camera", typeof(Camera));
            var camera = this.cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(2f, 1f, -4f);

            var entity = this.Manager.CreateEntity(typeof(CameraFrustumPlanes), typeof(CameraFrustumCorners), typeof(CameraBridge));
            this.Manager.SetComponentData(entity, new CameraBridge { Value = camera });

            this.system.Update();

            var planes = this.Manager.GetComponentData<CameraFrustumPlanes>(entity);
            var corners = this.Manager.GetComponentData<CameraFrustumCorners>(entity);

            Assert.IsFalse(planes.IsDefault);
            Assert.AreNotEqual(0f, corners.NearPlane.c0.x + corners.NearPlane.c0.y + corners.NearPlane.c0.z);
        }

        [Test]
        public void Update_WithNullCamera_DoesNotWriteFrustumData()
        {
            var entity = this.Manager.CreateEntity(typeof(CameraFrustumPlanes), typeof(CameraFrustumCorners), typeof(CameraBridge));
            this.Manager.SetComponentData(entity, new CameraBridge());

            this.system.Update();

            var planes = this.Manager.GetComponentData<CameraFrustumPlanes>(entity);
            Assert.IsTrue(planes.IsDefault);
        }
    }
}
