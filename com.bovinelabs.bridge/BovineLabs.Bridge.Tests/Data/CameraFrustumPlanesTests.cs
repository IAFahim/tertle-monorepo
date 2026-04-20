// <copyright file="CameraFrustumPlanesTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data.Camera;
    using NUnit.Framework;
    using Unity.Mathematics;

    public class CameraFrustumPlanesTests
    {
        [Test]
        public void Indexer_ReadWrite_AllSixPlanes()
        {
            var planes = default(CameraFrustumPlanes);

            planes[0] = new float4(1, 2, 3, 4);
            planes[1] = new float4(5, 6, 7, 8);
            planes[2] = new float4(9, 10, 11, 12);
            planes[3] = new float4(13, 14, 15, 16);
            planes[4] = new float4(17, 18, 19, 20);
            planes[5] = new float4(21, 22, 23, 24);

            Assert.AreEqual(new float4(1, 2, 3, 4), planes.Left);
            Assert.AreEqual(new float4(5, 6, 7, 8), planes.Right);
            Assert.AreEqual(new float4(9, 10, 11, 12), planes.Bottom);
            Assert.AreEqual(new float4(13, 14, 15, 16), planes.Top);
            Assert.AreEqual(new float4(17, 18, 19, 20), planes.Near);
            Assert.AreEqual(new float4(21, 22, 23, 24), planes.Far);
        }

        [Test]
        public void IsDefault_ReturnsExpectedValue()
        {
            var planes = default(CameraFrustumPlanes);
            Assert.IsTrue(planes.IsDefault);

            planes.Left = new float4(1, 0, 0, 1);
            Assert.IsFalse(planes.IsDefault);
        }

        [Test]
        public void Equals_AndHashCode_MatchForEqualValues()
        {
            var a = new CameraFrustumPlanes
            {
                Left = new float4(1, 0, 0, 1),
                Right = new float4(-1, 0, 0, 1),
                Bottom = new float4(0, 1, 0, 1),
                Top = new float4(0, -1, 0, 1),
                Near = new float4(0, 0, 1, 1),
                Far = new float4(0, 0, -1, 1),
            };

            var b = a;

            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }
    }
}
