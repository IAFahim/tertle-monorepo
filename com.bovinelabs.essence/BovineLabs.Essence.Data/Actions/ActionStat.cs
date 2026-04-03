// <copyright file="ActionStat.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data.Actions
{
    using System.Runtime.InteropServices;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Defines the type of value calculation for stat actions.
    /// </summary>
    public enum StatValueType : byte
    {
        /// <summary> Fixed value set from authoring. </summary>
        Fixed = 0,

        /// <summary> Remaps a condition value linearly. This condition must exist with ConditionFeature.Value set. </summary>
        Linear = 1,

        /// <summary> Random picks a value within a range on creation. </summary>
        Range = 2,
    }

    /// <summary>
    /// An action that modifies a stat value on an entity.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [InternalBufferCapacity(1)]
    public struct ActionStat : IBufferElementData, IActionWithTarget
    {
        [FieldOffset(0)]
        public StatKey Type;

        [FieldOffset(2)]
        public StatModifyType ModifyType;

        [FieldOffset(3)]
        public StatValueType ValueType;

        [FieldOffset(4)]
        public Target Target;

        [FieldOffset(8)]
        public ValueUnion Fixed;

        [FieldOffset(8)]
        public RangeData Range;

        [FieldOffset(8)]
        public LinearData Linear;

        Target IActionWithTarget.Target => this.Target;

        /// <summary>
        /// A union type for storing different value formats in the same memory space.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct ValueUnion
        {
            [FieldOffset(0)]
            public uint Raw;

            [FieldOffset(0)]
            public int Int;

            [FieldOffset(0)]
            public float Float;
        }

        /// <summary>
        /// Data for range-based stat value calculations.
        /// </summary>
        public struct RangeData
        {
            public ValueUnion Value; // Set on effect setup
            public ValueUnion Min;
            public ValueUnion Max;
        }

        /// <summary>
        /// Data for linear interpolation stat value calculations.
        /// </summary>
        public struct LinearData
        {
            public byte Index;
            public int FromMin;
            public int FromMax;
            public ValueUnion ToMin;
            public ValueUnion ToMax;
        }
    }
}
