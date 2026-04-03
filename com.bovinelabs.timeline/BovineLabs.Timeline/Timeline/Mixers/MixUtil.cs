// <copyright file="MixUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Mathematics;

    /// <summary>
    /// Utility methods for mixing primitive and vector types with override flags.
    /// </summary>
    public static class MixUtil
    {
        /// <summary>
        /// Interpolates between two float values when overrides are enabled.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="s">The interpolation factor.</param>
        /// <param name="overrideA">Whether to use the first value.</param>
        /// <param name="overrideB">Whether to use the second value.</param>
        /// <returns>The selected or interpolated value.</returns>
        public static float LerpFloat(float a, float b, float s, bool overrideA, bool overrideB)
        {
            if (!overrideA && !overrideB)
            {
                return a;
            }

            if (!overrideA)
            {
                return b;
            }

            if (!overrideB)
            {
                return a;
            }

            return math.lerp(a, b, s);
        }

        /// <summary>
        /// Interpolates between two float2 values when overrides are enabled.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="s">The interpolation factor.</param>
        /// <param name="overrideA">Whether to use the first value.</param>
        /// <param name="overrideB">Whether to use the second value.</param>
        /// <returns>The selected or interpolated value.</returns>
        public static float2 LerpFloat2(in float2 a, in float2 b, float s, bool overrideA, bool overrideB)
        {
            if (!overrideA && !overrideB)
            {
                return a;
            }

            if (!overrideA)
            {
                return b;
            }

            if (!overrideB)
            {
                return a;
            }

            return math.lerp(a, b, s);
        }

        /// <summary>
        /// Interpolates between two float3 values when overrides are enabled.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="s">The interpolation factor.</param>
        /// <param name="overrideA">Whether to use the first value.</param>
        /// <param name="overrideB">Whether to use the second value.</param>
        /// <returns>The selected or interpolated value.</returns>
        public static float3 LerpFloat3(in float3 a, in float3 b, float s, bool overrideA, bool overrideB)
        {
            if (!overrideA && !overrideB)
            {
                return a;
            }

            if (!overrideA)
            {
                return b;
            }

            if (!overrideB)
            {
                return a;
            }

            return math.lerp(a, b, s);
        }

        /// <summary>
        /// Interpolates between two float4 values when overrides are enabled.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="s">The interpolation factor.</param>
        /// <param name="overrideA">Whether to use the first value.</param>
        /// <param name="overrideB">Whether to use the second value.</param>
        /// <returns>The selected or interpolated value.</returns>
        public static float4 LerpFloat4(in float4 a, in float4 b, float s, bool overrideA, bool overrideB)
        {
            if (!overrideA && !overrideB)
            {
                return a;
            }

            if (!overrideA)
            {
                return b;
            }

            if (!overrideB)
            {
                return a;
            }

            return math.lerp(a, b, s);
        }

        /// <summary>
        /// Adds two float values based on override flags.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="overrideA">Whether to include the first value.</param>
        /// <param name="overrideB">Whether to include the second value.</param>
        /// <returns>The summed value.</returns>
        public static float AddFloat(float a, float b, bool overrideA, bool overrideB)
        {
            var value = 0f;
            if (overrideA)
            {
                value += a;
            }

            if (overrideB)
            {
                value += b;
            }

            return value;
        }

        /// <summary>
        /// Adds two float2 values based on override flags.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="overrideA">Whether to include the first value.</param>
        /// <param name="overrideB">Whether to include the second value.</param>
        /// <returns>The summed value.</returns>
        public static float2 AddFloat2(float2 a, float2 b, bool overrideA, bool overrideB)
        {
            var value = float2.zero;
            if (overrideA)
            {
                value += a;
            }

            if (overrideB)
            {
                value += b;
            }

            return value;
        }

        /// <summary>
        /// Adds two float3 values based on override flags.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="overrideA">Whether to include the first value.</param>
        /// <param name="overrideB">Whether to include the second value.</param>
        /// <returns>The summed value.</returns>
        public static float3 AddFloat3(float3 a, float3 b, bool overrideA, bool overrideB)
        {
            var value = float3.zero;
            if (overrideA)
            {
                value += a;
            }

            if (overrideB)
            {
                value += b;
            }

            return value;
        }

        /// <summary>
        /// Adds two float4 values based on override flags.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="overrideA">Whether to include the first value.</param>
        /// <param name="overrideB">Whether to include the second value.</param>
        /// <returns>The summed value.</returns>
        public static float4 AddFloat4(float4 a, float4 b, bool overrideA, bool overrideB)
        {
            var value = float4.zero;
            if (overrideA)
            {
                value += a;
            }

            if (overrideB)
            {
                value += b;
            }

            return value;
        }

        /// <summary>
        /// Selects a boolean value based on override flags and a blend factor.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="s">The blend factor used when both overrides are enabled.</param>
        /// <param name="overrideA">Whether to consider the first value.</param>
        /// <param name="overrideB">Whether to consider the second value.</param>
        /// <returns>The selected value.</returns>
        public static bool SelectBool(in bool a, in bool b, in float s, in bool overrideA, in bool overrideB)
        {
            if (!overrideA && !overrideB)
            {
                return a;
            }

            if (!overrideA)
            {
                return b;
            }

            if (!overrideB)
            {
                return a;
            }

            return s >= 0.5f ? b : a;
        }
    }
}
