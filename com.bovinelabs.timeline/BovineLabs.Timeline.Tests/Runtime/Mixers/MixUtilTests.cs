// <copyright file="MixUtilTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Runtime.Mixers
{
    using NUnit.Framework;
    using Unity.Mathematics;
    using UnityEngine.UIElements;

    public class MixUtilTests
    {
        [Test]
        public void LerpFloat_HandlesAllOverrideBranches()
        {
            Assert.AreEqual(10f, MixUtil.LerpFloat(10f, 20f, 0.25f, false, false));
            Assert.AreEqual(20f, MixUtil.LerpFloat(10f, 20f, 0.25f, false, true));
            Assert.AreEqual(10f, MixUtil.LerpFloat(10f, 20f, 0.25f, true, false));
            Assert.AreEqual(math.lerp(10f, 20f, 0.25f), MixUtil.LerpFloat(10f, 20f, 0.25f, true, true));
        }

        [Test]
        public void LerpFloat2_HandlesAllOverrideBranches()
        {
            var a = new float2(1, 2);
            var b = new float2(3, 4);

            AssertFloat2Equal(a, MixUtil.LerpFloat2(a, b, 0.25f, false, false));
            AssertFloat2Equal(b, MixUtil.LerpFloat2(a, b, 0.25f, false, true));
            AssertFloat2Equal(a, MixUtil.LerpFloat2(a, b, 0.25f, true, false));
            AssertFloat2Equal(math.lerp(a, b, 0.25f), MixUtil.LerpFloat2(a, b, 0.25f, true, true));
        }

        [Test]
        public void LerpFloat3_HandlesAllOverrideBranches()
        {
            var a = new float3(1, 2, 3);
            var b = new float3(4, 5, 6);

            AssertFloat3Equal(a, MixUtil.LerpFloat3(a, b, 0.5f, false, false));
            AssertFloat3Equal(b, MixUtil.LerpFloat3(a, b, 0.5f, false, true));
            AssertFloat3Equal(a, MixUtil.LerpFloat3(a, b, 0.5f, true, false));
            AssertFloat3Equal(math.lerp(a, b, 0.5f), MixUtil.LerpFloat3(a, b, 0.5f, true, true));
        }

        [Test]
        public void LerpFloat4_HandlesAllOverrideBranches()
        {
            var a = new float4(1, 2, 3, 4);
            var b = new float4(5, 6, 7, 8);

            AssertFloat4Equal(a, MixUtil.LerpFloat4(a, b, 0.75f, false, false));
            AssertFloat4Equal(b, MixUtil.LerpFloat4(a, b, 0.75f, false, true));
            AssertFloat4Equal(a, MixUtil.LerpFloat4(a, b, 0.75f, true, false));
            AssertFloat4Equal(math.lerp(a, b, 0.75f), MixUtil.LerpFloat4(a, b, 0.75f, true, true));
        }

        [Test]
        public void AddFloat_HandlesAllSelectionBranches()
        {
            Assert.AreEqual(0f, MixUtil.AddFloat(2f, 3f, false, false));
            Assert.AreEqual(2f, MixUtil.AddFloat(2f, 3f, true, false));
            Assert.AreEqual(3f, MixUtil.AddFloat(2f, 3f, false, true));
            Assert.AreEqual(5f, MixUtil.AddFloat(2f, 3f, true, true));
        }

        [Test]
        public void AddFloat2_HandlesAllSelectionBranches()
        {
            var a = new float2(1, 2);
            var b = new float2(3, 4);

            AssertFloat2Equal(float2.zero, MixUtil.AddFloat2(a, b, false, false));
            AssertFloat2Equal(a, MixUtil.AddFloat2(a, b, true, false));
            AssertFloat2Equal(b, MixUtil.AddFloat2(a, b, false, true));
            AssertFloat2Equal(a + b, MixUtil.AddFloat2(a, b, true, true));
        }

        [Test]
        public void AddFloat3_HandlesAllSelectionBranches()
        {
            var a = new float3(1, 2, 3);
            var b = new float3(4, 5, 6);

            AssertFloat3Equal(float3.zero, MixUtil.AddFloat3(a, b, false, false));
            AssertFloat3Equal(a, MixUtil.AddFloat3(a, b, true, false));
            AssertFloat3Equal(b, MixUtil.AddFloat3(a, b, false, true));
            AssertFloat3Equal(a + b, MixUtil.AddFloat3(a, b, true, true));
        }

        [Test]
        public void AddFloat4_HandlesAllSelectionBranches()
        {
            var a = new float4(1, 2, 3, 4);
            var b = new float4(5, 6, 7, 8);

            AssertFloat4Equal(float4.zero, MixUtil.AddFloat4(a, b, false, false));
            AssertFloat4Equal(a, MixUtil.AddFloat4(a, b, true, false));
            AssertFloat4Equal(b, MixUtil.AddFloat4(a, b, false, true));
            AssertFloat4Equal(a + b, MixUtil.AddFloat4(a, b, true, true));
        }

        [Test]
        public void SelectBool_HandlesAllOverrideBranches()
        {
            Assert.IsTrue(MixUtil.SelectBool(true, false, 0.25f, false, false));
            Assert.IsFalse(MixUtil.SelectBool(true, false, 0.25f, false, true));
            Assert.IsTrue(MixUtil.SelectBool(true, false, 0.25f, true, false));
            Assert.IsTrue(MixUtil.SelectBool(true, false, 0.49f, true, true));
            Assert.IsFalse(MixUtil.SelectBool(true, false, 0.5f, true, true));
        }

        private static void AssertFloat2Equal(float2 expected, float2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
        }

        private static void AssertFloat3Equal(float3 expected, float3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }

        private static void AssertFloat4Equal(float4 expected, float4 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
            Assert.AreEqual(expected.w, actual.w, 0.0001f);
        }
    }
}
