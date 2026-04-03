// <copyright file="Float2Mixer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Mathematics;

    /// <summary>
    /// Mixer implementation for <see cref="float2"/> values.
    /// </summary>
    public readonly struct Float2Mixer : IMixer<float2>
    {
        /// <inheritdoc />
        public float2 Lerp(in float2 a, in float2 b, in float s)
        {
            return math.lerp(a, b, s);
        }

        /// <inheritdoc />
        public float2 Add(in float2 a, in float2 b)
        {
            return a + b;
        }
    }
}
