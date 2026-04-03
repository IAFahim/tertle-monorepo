// <copyright file="StatModifyType.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    /// <summary>
    /// Defines how a stat modifier affects the base stat value.
    /// </summary>
    public enum StatModifyType : byte
    {
        /// <summary> Direct addition to the base stat value. </summary>
        Added = 0,

        /// <summary> Percentage-based increase that stacks additively with other additive modifiers. </summary>
        Additive = 1,

        /// <summary> Percentage-based multiplier that compounds with other multiplicative modifiers. </summary>
        Multiplicative = 2,
    }
}
