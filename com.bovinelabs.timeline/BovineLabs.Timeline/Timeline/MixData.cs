// <copyright file="MixData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Mathematics;

    /// <summary>
    /// Holds accumulated mix values and weights for blending.
    /// </summary>
    /// <typeparam name="T">The value type being blended.</typeparam>
    public struct MixData<T>
        where T : unmanaged
    {
        /// <summary>The blend weights for each value slot.</summary>
        public float4 Weights;

        /// <summary>The primary value.</summary>
        public T Value1;

        /// <summary>The secondary value.</summary>
        public T Value2;

        /// <summary>The tertiary value.</summary>
        public T Value3;

        /// <summary>The quaternary value.</summary>
        public T Value4;

        /// <summary>Whether this mix should be applied additively.</summary>
        public bool Additive;
    }
}
