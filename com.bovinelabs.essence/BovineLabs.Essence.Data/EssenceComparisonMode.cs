// <copyright file="EssenceComparisonMode.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using System.Runtime.InteropServices;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    [StructLayout(LayoutKind.Explicit)]
    public struct EssenceComparisonMode : IBufferElementData, IEnableableComponent
    {
        [FieldOffset(0)]
        public byte Index;

        [FieldOffset(1)]
        public byte ConditionIndex;

        [FieldOffset(2)]
        public bool IsStat;

        [FieldOffset(4)]
        public IntrinsicKey Intrinsic;

        [FieldOffset(4)]
        public StatKey Stat;
    }
}
