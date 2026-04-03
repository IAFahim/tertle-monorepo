// <copyright file="MixerImplementationsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Runtime.Mixers
{
    using NUnit.Framework;
    using Unity.Mathematics;

    public class MixerImplementationsTests
    {
        [Test]
        public void FloatMixer_LerpAndAdd_MatchExpectedMath()
        {
            var mixer = new FloatMixer();

            Assert.AreEqual(math.lerp(2f, 6f, 0.25f), mixer.Lerp(2f, 6f, 0.25f));
            Assert.AreEqual(8f, mixer.Add(2f, 6f));
        }

        [Test]
        public void Float2Mixer_LerpAndAdd_MatchExpectedMath()
        {
            var mixer = new Float2Mixer();
            var a = new float2(1, 2);
            var b = new float2(3, 4);

            AssertFloat2Equal(math.lerp(a, b, 0.5f), mixer.Lerp(a, b, 0.5f));
            AssertFloat2Equal(a + b, mixer.Add(a, b));
        }

        [Test]
        public void Float3Mixer_LerpAndAdd_MatchExpectedMath()
        {
            var mixer = new Float3Mixer();
            var a = new float3(1, 2, 3);
            var b = new float3(4, 5, 6);

            AssertFloat3Equal(math.lerp(a, b, 0.5f), mixer.Lerp(a, b, 0.5f));
            AssertFloat3Equal(a + b, mixer.Add(a, b));
        }

        [Test]
        public void Float4Mixer_LerpAndAdd_MatchExpectedMath()
        {
            var mixer = new Float4Mixer();
            var a = new float4(1, 2, 3, 4);
            var b = new float4(5, 6, 7, 8);

            AssertFloat4Equal(math.lerp(a, b, 0.5f), mixer.Lerp(a, b, 0.5f));
            AssertFloat4Equal(a + b, mixer.Add(a, b));
        }

        [Test]
        public void QuaternionMixer_Lerp_UsesNormalizedInterpolation()
        {
            var mixer = new QuaternionMixer();
            var a = quaternion.identity;
            var b = quaternion.RotateX(math.radians(90f));

            var expected = math.nlerp(a, b, 0.25f);
            var actual = mixer.Lerp(a, b, 0.25f);

            Assert.GreaterOrEqual(math.abs(math.dot(expected.value, actual.value)), 0.9999f);
            Assert.AreEqual(1f, math.length(actual.value), 0.0001f);
        }

        [Test]
        public void QuaternionMixer_Add_UsesQuaternionMultiply()
        {
            var mixer = new QuaternionMixer();
            var a = quaternion.RotateY(math.radians(30f));
            var b = quaternion.RotateX(math.radians(45f));

            var expected = math.mul(a, b);
            var actual = mixer.Add(a, b);

            Assert.GreaterOrEqual(math.abs(math.dot(expected.value, actual.value)), 0.9999f);
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
