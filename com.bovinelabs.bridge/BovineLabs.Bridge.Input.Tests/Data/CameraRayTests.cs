// <copyright file="CameraRayTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Tests.Data
{
    using BovineLabs.Bridge.Input;
    using NUnit.Framework;
    using Unity.Mathematics;
    using UnityEngine;

    public class CameraRayTests
    {
        [Test]
        public void ImplicitFromUnityRay_CopiesOriginAndDirection()
        {
            var ray = new Ray(new Vector3(1f, 2f, 3f), new Vector3(0f, 2f, 0f));

            CameraRay cameraRay = ray;

            Assert.AreEqual((float3)ray.origin, cameraRay.Origin);
            Assert.AreEqual((float3)ray.direction, cameraRay.Displacement);
        }

        [Test]
        public void ImplicitToUnityRay_NormalizesDirection()
        {
            var cameraRay = new CameraRay
            {
                Origin = new float3(4f, 5f, 6f),
                Displacement = new float3(0f, 3f, 0f),
            };

            Ray ray = cameraRay;

            Assert.AreEqual((Vector3)cameraRay.Origin, ray.origin);
            Assert.That(ray.direction.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(ray.direction.y, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ray.direction.z, Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
