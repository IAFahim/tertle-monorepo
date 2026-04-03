// <copyright file="JobHelpers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using BovineLabs.Timeline.Data;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Helper methods for accumulating and blending animated values.
    /// </summary>
    public static class JobHelpers
    {
        /// <summary>
        /// Writes the unblended value for a track into the blend data.
        /// </summary>
        /// <typeparam name="T">The value type being animated.</typeparam>
        /// <typeparam name="TC">The animated component type.</typeparam>
        /// <param name="binding">The track binding that identifies the target entity.</param>
        /// <param name="animatedComponent">The animated component holding the default value.</param>
        /// <param name="blendData">The blend data map to populate.</param>
        public static void AnimateUnblend<T, TC>(
            in TrackBinding binding, ref TC animatedComponent, NativeParallelHashMap<Entity, MixData<T>>.ParallelWriter blendData)
            where T : unmanaged
            where TC : unmanaged, IAnimatedComponent<T>
        {
            var v = animatedComponent.Value;

            var mixData = new MixData<T>
            {
                Value1 = v,
                Weights = new float4(1, 0, 0, 0),
            };

            blendData.TryAdd(binding.Value, mixData);
        }

        /// <summary>
        /// Accumulates a weighted clip value into the blend data for a track.
        /// </summary>
        /// <typeparam name="T">The value type being animated.</typeparam>
        /// <typeparam name="TC">The animated component type.</typeparam>
        /// <param name="binding">The track binding that identifies the target entity.</param>
        /// <param name="animatedComponent">The animated component holding the clip value.</param>
        /// <param name="c3">The clip weight to apply.</param>
        /// <param name="blendData">The blend data map to update.</param>
        public static void AccumulateWeighted<T, TC>(
            in TrackBinding binding, ref TC animatedComponent, in ClipWeight c3, NativeParallelHashMap<Entity, MixData<T>> blendData)
            where T : unmanaged
            where TC : unmanaged, IAnimatedComponent<T>
        {
            var v = animatedComponent.Value;

            if (!blendData.TryGetValue(binding.Value, out var data))
            {
                data.Weights = float4.zero;
            }

            if (c3.Value > data.Weights.x)
            {
                data.Weights = data.Weights.xxyz;
                data.Weights.x = c3.Value;
                data.Value4 = data.Value3;
                data.Value3 = data.Value2;
                data.Value2 = data.Value1;
                data.Value1 = v;
            }
            else if (c3.Value > data.Weights.y)
            {
                data.Weights = data.Weights.xxyz;
                data.Weights.y = c3.Value;
                data.Value4 = data.Value3;
                data.Value3 = data.Value2;
                data.Value2 = v;
            }
            else if (c3.Value > data.Weights.z)
            {
                data.Weights = data.Weights.xyyz;
                data.Weights.z = c3.Value;
                data.Value4 = data.Value3;
                data.Value3 = v;
            }
            else if (c3.Value > data.Weights.w)
            {
                data.Weights.w = c3.Value;
                data.Value4 = v;
            }

            blendData[binding.Value] = data;
        }

        /// <summary>
        /// Blends accumulated values into a final result.
        /// </summary>
        /// <typeparam name="T">The value type being blended.</typeparam>
        /// <typeparam name="TMixer">The mixer implementation used for blending.</typeparam>
        /// <param name="values">The accumulated mix data.</param>
        /// <param name="defaultValue">The default value to use when weights do not sum to one.</param>
        /// <param name="mixer">The mixer used for interpolation and additive blending.</param>
        /// <returns>The blended value.</returns>
        public static T Blend<T, TMixer>(ref MixData<T> values, in T defaultValue, TMixer mixer = default)
            where T : unmanaged
            where TMixer : unmanaged, IMixer<T>
        {
            var result = defaultValue;

            if (values.Weights.x > math.EPSILON)
            {
                var totalWeight = math.dot(values.Weights, new float4(1));
                if (totalWeight < 1 && !values.Additive)
                {
                    if (values.Weights.y <= math.EPSILON)
                    {
                        values.Weights.y = 1 - totalWeight;
                        values.Value2 = defaultValue;
                    }
                    else if (values.Weights.z <= math.EPSILON)
                    {
                        values.Weights.z = 1 - totalWeight;
                        values.Value3 = defaultValue;
                    }
                    else if (values.Weights.w <= math.EPSILON)
                    {
                        values.Weights.w = 1 - totalWeight;
                        values.Value4 = defaultValue;
                    }

                    totalWeight = 1;
                }

                var weights = values.Weights * math.rcp(totalWeight);
                if (weights.y <= math.EPSILON)
                {
                    result = values.Value1;
                }
                else if (weights.z <= math.EPSILON)
                {
                    result = mixer.Lerp(values.Value1, values.Value2, weights.y);
                }
                else
                {
                    var w = weights.x + weights.y;
                    var a = mixer.Lerp(values.Value1, values.Value2, weights.y / w);
                    var b = mixer.Lerp(values.Value3, values.Value4, weights.w / (1 - w));
                    result = mixer.Lerp(b, a, w);
                }

                if (values.Additive)
                {
                    result = mixer.Add(defaultValue, result);
                }
            }

            return result;
        }
    }
}
