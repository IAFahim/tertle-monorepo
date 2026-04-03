// <copyright file="StatExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Iterators;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary> Extension methods for <see cref="DynamicBuffer{Stat}" />. </summary>
    public static unsafe partial class StatExtensions
    {
        /// <summary>
        /// Gets the stat value for the specified key from the buffer.
        /// </summary>
        /// <param name="buffer">The stat buffer to search in.</param>
        /// <param name="key">The key identifying the stat to retrieve.</param>
        /// <param name="defaultAdded">The default added value to use if the key is not found.</param>
        /// <returns>The stat value for the specified key, or a default value if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StatValue Get(this in DynamicBuffer<Stat> buffer, StatKey key, short defaultAdded = 0)
        {
            return buffer.AsMap().Get(key, defaultAdded);
        }

        /// <summary>
        /// Gets the calculated stat value for the specified key from the buffer.
        /// </summary>
        /// <param name="buffer">The stat buffer to search in.</param>
        /// <param name="key">The key identifying the stat to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The calculated stat value for the specified key, or the default value if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetValue(this in DynamicBuffer<Stat> buffer, StatKey key, int defaultValue = 0)
        {
            return buffer.AsMap().GetValue(key, defaultValue);
        }

        /// <summary>
        /// Gets the calculated stat value as a float for the specified key from the buffer.
        /// </summary>
        /// <param name="buffer">The stat buffer to search in.</param>
        /// <param name="key">The key identifying the stat to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The calculated stat value as a float for the specified key, or the default value if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetValueFloat(this in DynamicBuffer<Stat> buffer, StatKey key, float defaultValue = 0)
        {
            return buffer.AsMap().GetValueFloat(key, defaultValue);
        }

        /// <summary>
        /// Gets the stat value for the specified key from the hash map.
        /// </summary>
        /// <param name="buffer">The stat hash map to search in.</param>
        /// <param name="key">The key identifying the stat to retrieve.</param>
        /// <param name="defaultAdded">The default added value to use if the key is not found.</param>
        /// <returns>The stat value for the specified key, or a default value if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StatValue Get(this in DynamicHashMap<StatKey, StatValue> buffer, StatKey key, short defaultAdded = 0)
        {
            if (buffer.TryGetValue(key, out var stat))
            {
                return stat;
            }

            var sv = StatValue.Default;
            sv.Added = defaultAdded;
            return sv;
        }

        /// <summary>
        /// Gets the calculated stat value for the specified key from the hash map.
        /// </summary>
        /// <param name="buffer">The stat hash map to search in.</param>
        /// <param name="key">The key identifying the stat to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The calculated stat value for the specified key, or the default value if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetValue(this in DynamicHashMap<StatKey, StatValue> buffer, StatKey key, int defaultValue = 0)
        {
            return buffer.TryGetValue(key, out var stat) ? stat.Value : defaultValue;
        }

        /// <summary>
        /// Gets the calculated stat value for the specified key from the hash map and floors it to an integer.
        /// </summary>
        /// <param name="buffer">The stat hash map to search in.</param>
        /// <param name="key">The key identifying the stat to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The calculated stat value floored to an integer for the specified key, or the default value if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetValueFloor(this in DynamicHashMap<StatKey, StatValue> buffer, StatKey key, int defaultValue = 0)
        {
            return (int)math.floor(buffer.GetValue(key, defaultValue));
        }

        /// <summary>
        /// Gets the calculated stat value as a float for the specified key from the hash map.
        /// </summary>
        /// <param name="buffer">The stat hash map to search in.</param>
        /// <param name="key">The key identifying the stat to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The calculated stat value as a float for the specified key, or the default value if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetValueFloat(this in DynamicHashMap<StatKey, StatValue> buffer, StatKey key, float defaultValue = 0)
        {
            return buffer.TryGetValue(key, out var stat) ? stat.ValueFloat : defaultValue;
        }

        /// <summary> Reads three consecutive stats and returns them as a float 3 so that float3(key, key+1, key+2). </summary>
        /// <param name="stats"> The stat buffer. </param>
        /// <param name="key"> The first index. </param>
        /// <returns> The 3 stats in a float3. </returns>
        public static ref readonly float3 Read3(this DynamicBuffer<Stat> stats, int key)
        {
            CheckInRange(stats, key);

            var ptr = (float*)stats.GetUnsafeReadOnlyPtr();
            ptr += key;

            return ref UnsafeUtility.AsRef<float3>(ptr);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckInRange(DynamicBuffer<Stat> stats, int key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (key + 3 > stats.Length)
            {
                throw new ArgumentOutOfRangeException($"{key} + {UnsafeUtility.SizeOf<float3>()} > {stats.Length}");
            }
#endif
        }
    }
}
