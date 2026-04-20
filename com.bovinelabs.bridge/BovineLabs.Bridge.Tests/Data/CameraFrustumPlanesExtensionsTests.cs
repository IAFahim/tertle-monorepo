// <copyright file="CameraFrustumPlanesExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data.Camera;
    using NUnit.Framework;
    using Unity.Mathematics;

    public class CameraFrustumPlanesExtensionsTests
    {
        [Test]
        public void Intersect_FullyInside_ReturnsIn()
        {
            var planes = CreateUnitCubeFrustum();
            var aabb = new AABB { Center = float3.zero, Extents = new float3(0.25f) };

            var result = planes.Intersect(aabb);

            Assert.AreEqual(IntersectResult.In, result);
            Assert.IsTrue(planes.AnyIntersect(aabb));
        }

        [Test]
        public void Intersect_FullyOutside_ReturnsOut()
        {
            var planes = CreateUnitCubeFrustum();
            var aabb = new AABB { Center = new float3(3f, 0f, 0f), Extents = new float3(0.5f) };

            var result = planes.Intersect(aabb);

            Assert.AreEqual(IntersectResult.Out, result);
            Assert.IsFalse(planes.AnyIntersect(aabb));
        }

        [Test]
        public void Intersect_PartialOverlap_ReturnsPartial()
        {
            var planes = CreateUnitCubeFrustum();
            var aabb = new AABB { Center = new float3(0.9f, 0f, 0f), Extents = new float3(0.2f) };

            var result = planes.Intersect(aabb);

            Assert.AreEqual(IntersectResult.Partial, result);
            Assert.IsTrue(planes.AnyIntersect(aabb));
        }

        [Test]
        public void GetNearCenter_ReturnsExpectedCenter()
        {
            var planes = CreateUnitCubeFrustum();

            var nearCenter = planes.GetNearCenter();

            Assert.That(nearCenter.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(nearCenter.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(nearCenter.z, Is.EqualTo(-1f).Within(0.0001f));
        }

        private static CameraFrustumPlanes CreateUnitCubeFrustum()
        {
            return new CameraFrustumPlanes
            {
                Left = new float4(1f, 0f, 0f, 1f),
                Right = new float4(-1f, 0f, 0f, 1f),
                Bottom = new float4(0f, 1f, 0f, 1f),
                Top = new float4(0f, -1f, 0f, 1f),
                Near = new float4(0f, 0f, 1f, 1f),
                Far = new float4(0f, 0f, -1f, 1f),
            };
        }
    }
}
