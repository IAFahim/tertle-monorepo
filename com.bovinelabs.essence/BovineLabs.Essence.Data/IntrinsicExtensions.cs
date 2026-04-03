// <copyright file="IntrinsicExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Iterators;
    using Unity.Entities;

    public static partial class IntrinsicExtensions
    {
        /// <summary>
        /// Gets the intrinsic value for the specified key from the buffer.
        /// </summary>
        /// <param name="buffer">The intrinsic buffer to search in.</param>
        /// <param name="key">The key identifying the intrinsic value to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The intrinsic value for the specified key, or the default value if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetValue(this in DynamicBuffer<Intrinsic> buffer, IntrinsicKey key, int defaultValue = 0)
        {
            return buffer.AsMap().GetValue(key, defaultValue);
        }

        /// <summary>
        /// Gets the intrinsic value for the specified key from the hash map.
        /// </summary>
        /// <param name="buffer">The intrinsic hash map to search in.</param>
        /// <param name="key">The key identifying the intrinsic value to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The intrinsic value for the specified key, or the default value if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetValue(this in DynamicHashMap<IntrinsicKey, int> buffer, IntrinsicKey key, int defaultValue = 0)
        {
            return buffer.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
