// <copyright file="Float4Mixer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Mathematics;

    public readonly struct Float4Mixer : IMixer<float4>
    {
        public float4 Lerp(in float4 a, in float4 b, in float s)
        {
            return math.lerp(a, b, s);
        }

        public float4 Add(in float4 a, in float4 b)
        {
            return a + b;
        }
    }
}
