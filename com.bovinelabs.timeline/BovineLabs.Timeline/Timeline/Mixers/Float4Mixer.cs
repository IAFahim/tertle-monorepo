// <copyright file="Float4Mixer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Mathematics;

    /// <summary>
    /// Mixer implementation for <see cref="float4"/> values.
    /// </summary>
    public readonly struct Float4Mixer : IMixer<float4>
    {
        /// <inheritdoc />
        public float4 Lerp(in float4 a, in float4 b, in float s)
        {
            return math.lerp(a, b, s);
        }

        /// <inheritdoc />
        public float4 Add(in float4 a, in float4 b)
        {
            return a + b;
        }
    }
}
