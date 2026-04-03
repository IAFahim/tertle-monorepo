// <copyright file="InputBounds.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.Input
{
    using Unity.Mathematics;
    using Unity.NetCode;

    public struct InputBounds : IInputComponentData
    {
        [GhostField(Quantization = 100)]
        public float3 Min;

        [GhostField(Quantization = 100)]
        public float3 Max;

        public float3 Center => (this.Max + this.Min) / 2f;

        public MinMaxAABB AABB => new()
        {
            Min = this.Min,
            Max = this.Max,
        };
    }
}
