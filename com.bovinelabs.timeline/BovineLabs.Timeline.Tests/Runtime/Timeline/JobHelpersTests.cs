// <copyright file="JobHelpersTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Runtime.Timeline
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Tests.TestDoubles;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    public class JobHelpersTests
    {
        [Test]
        public void AnimateUnblend_InsertsDefaultValueAndWeight()
        {
            var blendData = new NativeParallelHashMap<Entity, MixData<float>>(1, Allocator.TempJob);
            try
            {
                var target = new Entity { Index = 1, Version = 1 };
                var binding = new TrackBinding { Value = target };
                var animated = new TestAnimatedFloatComponent { CurrentValue = 42f };

                JobHelpers.AnimateUnblend(binding, ref animated, blendData.AsParallelWriter());

                Assert.IsTrue(blendData.TryGetValue(target, out var mix));
                Assert.AreEqual(42f, mix.Value1, 0.0001f);
                AssertFloat4Equal(new float4(1, 0, 0, 0), mix.Weights);
            }
            finally
            {
                blendData.Dispose();
            }
        }

        [Test]
        public void AccumulateWeighted_FirstInsert_AddsEntryWhenMissing()
        {
            var blendData = new NativeParallelHashMap<Entity, MixData<float>>(1, Allocator.TempJob);
            try
            {
                var target = new Entity { Index = 2, Version = 1 };
                var binding = new TrackBinding { Value = target };
                var animated = new TestAnimatedFloatComponent { CurrentValue = 5f };

                JobHelpers.AccumulateWeighted(
                    binding,
                    ref animated,
                    new ClipWeight { Value = 0.25f },
                    blendData);

                Assert.IsTrue(blendData.TryGetValue(target, out var mix));
                Assert.AreEqual(5f, mix.Value1, 0.0001f);
                Assert.AreEqual(0.25f, mix.Weights.x, 0.0001f);
                Assert.AreEqual(0f, mix.Weights.y, 0.0001f);
            }
            finally
            {
                blendData.Dispose();
            }
        }

        [Test]
        public void AccumulateWeighted_NewHighestWeight_ShiftsExistingEntriesDown()
        {
            var blendData = new NativeParallelHashMap<Entity, MixData<float>>(1, Allocator.TempJob);
            try
            {
                var target = new Entity { Index = 3, Version = 1 };
                var binding = new TrackBinding { Value = target };
                blendData[target] = new MixData<float>
                {
                    Weights = new float4(0.7f, 0.5f, 0.3f, 0.1f),
                    Value1 = 7f,
                    Value2 = 5f,
                    Value3 = 3f,
                    Value4 = 1f,
                };

                var animated = new TestAnimatedFloatComponent { CurrentValue = 9f };
                JobHelpers.AccumulateWeighted(
                    binding,
                    ref animated,
                    new ClipWeight { Value = 0.8f },
                    blendData);

                var mix = blendData[target];
                AssertFloat4Equal(new float4(0.8f, 0.7f, 0.5f, 0.3f), mix.Weights);
                Assert.AreEqual(9f, mix.Value1, 0.0001f);
                Assert.AreEqual(7f, mix.Value2, 0.0001f);
                Assert.AreEqual(5f, mix.Value3, 0.0001f);
                Assert.AreEqual(3f, mix.Value4, 0.0001f);
            }
            finally
            {
                blendData.Dispose();
            }
        }

        [Test]
        public void AccumulateWeighted_InsertsIntoSecondSlot_WhenWeightIsSecondHighest()
        {
            var blendData = new NativeParallelHashMap<Entity, MixData<float>>(1, Allocator.TempJob);
            try
            {
                var target = new Entity { Index = 4, Version = 1 };
                var binding = new TrackBinding { Value = target };
                blendData[target] = new MixData<float>
                {
                    Weights = new float4(0.9f, 0.5f, 0.3f, 0.1f),
                    Value1 = 9f,
                    Value2 = 5f,
                    Value3 = 3f,
                    Value4 = 1f,
                };

                var animated = new TestAnimatedFloatComponent { CurrentValue = 6f };
                JobHelpers.AccumulateWeighted(
                    binding,
                    ref animated,
                    new ClipWeight { Value = 0.6f },
                    blendData);

                var mix = blendData[target];
                AssertFloat4Equal(new float4(0.9f, 0.6f, 0.5f, 0.3f), mix.Weights);
                Assert.AreEqual(9f, mix.Value1, 0.0001f);
                Assert.AreEqual(6f, mix.Value2, 0.0001f);
                Assert.AreEqual(5f, mix.Value3, 0.0001f);
                Assert.AreEqual(3f, mix.Value4, 0.0001f);
            }
            finally
            {
                blendData.Dispose();
            }
        }

        [Test]
        public void AccumulateWeighted_InsertsIntoThirdSlot_WhenWeightIsThirdHighest()
        {
            var blendData = new NativeParallelHashMap<Entity, MixData<float>>(1, Allocator.TempJob);
            try
            {
                var target = new Entity { Index = 5, Version = 1 };
                var binding = new TrackBinding { Value = target };
                blendData[target] = new MixData<float>
                {
                    Weights = new float4(0.9f, 0.6f, 0.3f, 0.1f),
                    Value1 = 9f,
                    Value2 = 6f,
                    Value3 = 3f,
                    Value4 = 1f,
                };

                var animated = new TestAnimatedFloatComponent { CurrentValue = 4f };
                JobHelpers.AccumulateWeighted(
                    binding,
                    ref animated,
                    new ClipWeight { Value = 0.4f },
                    blendData);

                var mix = blendData[target];
                AssertFloat4Equal(new float4(0.9f, 0.6f, 0.4f, 0.3f), mix.Weights);
                Assert.AreEqual(9f, mix.Value1, 0.0001f);
                Assert.AreEqual(6f, mix.Value2, 0.0001f);
                Assert.AreEqual(4f, mix.Value3, 0.0001f);
                Assert.AreEqual(3f, mix.Value4, 0.0001f);
            }
            finally
            {
                blendData.Dispose();
            }
        }

        [Test]
        public void AccumulateWeighted_InsertsIntoFourthSlot_WhenWeightIsFourthHighest()
        {
            var blendData = new NativeParallelHashMap<Entity, MixData<float>>(1, Allocator.TempJob);
            try
            {
                var target = new Entity { Index = 6, Version = 1 };
                var binding = new TrackBinding { Value = target };
                blendData[target] = new MixData<float>
                {
                    Weights = new float4(0.9f, 0.6f, 0.4f, 0.1f),
                    Value1 = 9f,
                    Value2 = 6f,
                    Value3 = 4f,
                    Value4 = 1f,
                };

                var animated = new TestAnimatedFloatComponent { CurrentValue = 2f };
                JobHelpers.AccumulateWeighted(
                    binding,
                    ref animated,
                    new ClipWeight { Value = 0.2f },
                    blendData);

                var mix = blendData[target];
                AssertFloat4Equal(new float4(0.9f, 0.6f, 0.4f, 0.2f), mix.Weights);
                Assert.AreEqual(2f, mix.Value4, 0.0001f);
            }
            finally
            {
                blendData.Dispose();
            }
        }

        [Test]
        public void Blend_NoWeightsAboveEpsilon_ReturnsDefault()
        {
            var values = new MixData<float> { Weights = float4.zero };

            var result = JobHelpers.Blend<float, FloatMixer>(ref values, 3f);

            Assert.AreEqual(3f, result, 0.0001f);
        }

        [Test]
        public void Blend_OneValuePath_ReturnsValue1()
        {
            var values = new MixData<float>
            {
                Weights = new float4(1f, 0f, 0f, 0f),
                Value1 = 8f,
            };

            var result = JobHelpers.Blend<float, FloatMixer>(ref values, 3f);

            Assert.AreEqual(8f, result, 0.0001f);
        }

        [Test]
        public void Blend_TwoValuePath_UsesMixerLerpWithNormalizedWeight()
        {
            var values = new MixData<float>
            {
                Weights = new float4(0.25f, 0.75f, 0f, 0f),
                Value1 = 10f,
                Value2 = 20f,
            };

            var result = JobHelpers.Blend<float, FloatMixer>(ref values, 0f);

            Assert.AreEqual(math.lerp(10f, 20f, 0.75f), result, 0.0001f);
        }

        [Test]
        public void Blend_FourValuePath_UsesTwoStepBlendBranch()
        {
            var values = new MixData<float>
            {
                Weights = new float4(0.4f, 0.3f, 0.2f, 0.1f),
                Value1 = 10f,
                Value2 = 20f,
                Value3 = 30f,
                Value4 = 40f,
            };

            var result = JobHelpers.Blend<float, FloatMixer>(ref values, 0f);

            var weights = values.Weights;
            var w = weights.x + weights.y;
            var a = math.lerp(values.Value1, values.Value2, weights.y / w);
            var b = math.lerp(values.Value3, values.Value4, weights.w / (1f - w));
            var expected = math.lerp(b, a, w);

            Assert.AreEqual(expected, result, 0.0001f);
        }

        [Test]
        public void Blend_NonAdditiveUnderweight_FillsDefaultAndNormalizes()
        {
            var values = new MixData<float>
            {
                Weights = new float4(0.5f, 0f, 0f, 0f),
                Value1 = 8f,
            };

            var result = JobHelpers.Blend<float, FloatMixer>(ref values, 2f);

            Assert.AreEqual(5f, result, 0.0001f);
            Assert.AreEqual(0.5f, values.Weights.y, 0.0001f);
            Assert.AreEqual(2f, values.Value2, 0.0001f);
        }

        [Test]
        public void Blend_AdditivePath_AddsDefaultToResult()
        {
            var values = new MixData<float>
            {
                Weights = new float4(1f, 0f, 0f, 0f),
                Value1 = 4f,
                Additive = true,
            };

            var result = JobHelpers.Blend<float, FloatMixer>(ref values, 3f);

            Assert.AreEqual(7f, result, 0.0001f);
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
