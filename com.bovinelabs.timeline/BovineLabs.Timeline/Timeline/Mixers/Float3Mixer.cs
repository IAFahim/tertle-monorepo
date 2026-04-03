// <copyright file="Float3Mixer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Mathematics;

    public readonly struct Float3Mixer : IMixer<float3>
    {
        public float3 Lerp(in float3 a, in float3 b, in float s)
        {
            return math.lerp(a, b, s);
        }

        public float3 Add(in float3 a, in float3 b)
        {
            return a + b;
        }
    }
}
