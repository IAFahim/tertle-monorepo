// <copyright file="ValueIndex.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Union struct representing either a specific value index or a min/max value indices range for condition comparisons.
    /// Values index into the <see cref="ConditionComparisonValue"/> buffer.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct ValueIndex
    {
        [FieldOffset(0)]
        public byte Value;

        [FieldOffset(0)]
        public byte Min;

        [FieldOffset(1)]
        public byte Max;
    }
}
