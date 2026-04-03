// <copyright file="FloatMixer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Mathematics;

    public readonly struct FloatMixer : IMixer<float>
    {
        public float Lerp(in float a, in float b, in float s)
        {
            return math.lerp(a, b, s);
        }

        public float Add(in float a, in float b)
        {
            return a + b;
        }
    }
}
