// <copyright file="Detour.Common.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;

    public static partial class Detour
    {
        /// <summary>Gets the opposite side index for a tile side value.</summary>
        /// <param name="side">The tile side index.</param>
        /// <returns>The opposite tile side index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte OppositeTile(byte side)
        {
            return (byte)((side + 4) & 0x7);
        }

        /// <summary>Swaps the values of two variables.</summary>
        /// <typeparam name="T">The type of the values being swapped.</typeparam>
        /// <param name="a">The first value to swap.</param>
        /// <param name="b">The second value to swap.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Swap<T>(ref T a, ref T b)
        {
            (a, b) = (b, a);
        }

        /// <summary>Rounds the value up to the next power of two.</summary>
        /// <param name="value">The value to round.</param>
        /// <returns>The next power of two, or zero if the input is zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint NextPowerOfTwo(uint value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;
            return value;
        }

        /// <summary>Returns floor(log2(value)) for an unsigned integer.</summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>The integer base-2 logarithm.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint IntegerLog2(uint value)
        {
            var result = (value > 0xffff) ? 16u : 0u;
            value >>= (int)result;

            var shift = (value > 0xff) ? 8u : 0u;
            value >>= (int)shift;
            result |= shift;

            shift = (value > 0xf) ? 4u : 0u;
            value >>= (int)shift;
            result |= shift;

            shift = (value > 0x3) ? 2u : 0u;
            value >>= (int)shift;
            result |= shift;

            result |= value >> 1;
            return result;
        }

        /// <summary>Determines if two axis-aligned bounding boxes overlap (quantized version).</summary>
        /// <param name="amin">The minimum bounds of the first box.</param>
        /// <param name="amax">The maximum bounds of the first box.</param>
        /// <param name="bmin">The minimum bounds of the second box.</param>
        /// <param name="bmax">The maximum bounds of the second box.</param>
        /// <returns>True if the bounds overlap; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool OverlapQuantBounds(ushort3 amin, ushort3 amax, ushort3 bmin, ushort3 bmax)
        {
            return amin.x <= bmax.x && amax.x >= bmin.x && amin.y <= bmax.y && amax.y >= bmin.y && amin.z <= bmax.z && amax.z >= bmin.z;
        }

        /// <summary>Determines if two axis-aligned bounding boxes overlap.</summary>
        /// <param name="amin">The minimum bounds of the first box.</param>
        /// <param name="amax">The maximum bounds of the first box.</param>
        /// <param name="bmin">The minimum bounds of the second box.</param>
        /// <param name="bmax">The maximum bounds of the second box.</param>
        /// <returns>True if the bounds overlap; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool OverlapBounds(in float3 amin, in float3 amax, in float3 bmin, in float3 bmax)
        {
            return !(amin.x > bmax.x) && !(amax.x < bmin.x) && !(amin.y > bmax.y) && !(amax.y < bmin.y) && !(amin.z > bmax.z) && !(amax.z < bmin.z);
        }

        /// <summary>
        /// Derives the y-axis height of the closest point on the triangle from the specified reference point.
        /// </summary>
        /// <param name="p">The reference point from which to test.</param>
        /// <param name="a">Vertex A of triangle ABC.</param>
        /// <param name="b">Vertex B of triangle ABC.</param>
        /// <param name="c">Vertex C of triangle ABC.</param>
        /// <param name="h">The resulting height.</param>
        /// <returns>True if the point projects onto the triangle.</returns>
        internal static bool ClosestHeightPointTriangle(in float3 p, in float3 a, in float3 b, in float3 c, out float h)
        {
            const float eps = 1e-6f;
            h = 0;

            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            // Compute scaled barycentric coordinates
            var denom = (v0.x * v1.z) - (v0.z * v1.x);
            if (math.abs(denom) < eps)
            {
                return false;
            }

            var u = (v1.z * v2.x) - (v1.x * v2.z);
            var v = (v0.x * v2.z) - (v0.z * v2.x);

            if (denom < 0)
            {
                denom = -denom;
                u = -u;
                v = -v;
            }

            // If point lies inside the triangle, return interpolated y-coord
            if (u >= 0.0f && v >= 0.0f && (u + v) <= denom)
            {
                h = a.y + (((v0.y * u) + (v1.y * v)) / denom);
                return true;
            }

            return false;
        }
    }
}
