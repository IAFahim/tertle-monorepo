// <copyright file="MixUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Mathematics;

    public static class MixUtil
    {
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
