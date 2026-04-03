// <copyright file="QuaternionMixer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Mathematics;

    /// <summary>
    /// Mixer implementation for <see cref="quaternion"/> values.
    /// </summary>
    public readonly struct QuaternionMixer : IMixer<quaternion>
    {
        /// <inheritdoc />
        public quaternion Lerp(in quaternion a, in quaternion b, in float s)
        {
            return math.nlerp(a, b, s);
        }

        /// <inheritdoc />
        public quaternion Add(in quaternion a, in quaternion b)
        {
            return math.mul(a, b);
        }
    }
}
