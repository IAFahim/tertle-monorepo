// <copyright file="StatAuthoringUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using System;
    using BovineLabs.Essence.Data;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Static utility class providing helper methods for converting between authoring and runtime stat representations.
    /// Handles the conversion of authoring stat modification types to runtime types and value encoding for the stat system.
    /// </summary>
    public static class StatAuthoringUtil
    {
        public static StatModifyType GetModifier(StatAuthoringType modifyType)
        {
            return modifyType switch
            {
                StatAuthoringType.Added => StatModifyType.Added,
                StatAuthoringType.Subtracted => StatModifyType.Added,
                StatAuthoringType.Increased => StatModifyType.Additive,
                StatAuthoringType.Reduced => StatModifyType.Additive,
                StatAuthoringType.More => StatModifyType.Multiplicative,
                StatAuthoringType.Less => StatModifyType.Multiplicative,
                _ => throw new ArgumentOutOfRangeException(nameof(modifyType)),
            };
        }

        public static uint GetValueRaw(StatAuthoringType modifierType, float value)
        {
            // Select correct union but also convert subtracted/reduced/less into negative values
            switch (modifierType)
            {
                case StatAuthoringType.Added:
                {
                    var s = (int)value;
                    return UnsafeUtility.As<int, uint>(ref s);
                }

                case StatAuthoringType.Subtracted:
                {
                    var s = (int)-value;
                    return UnsafeUtility.As<int, uint>(ref s);
                }

                case StatAuthoringType.Increased:
                case StatAuthoringType.More:
                {
                    return UnsafeUtility.As<float, uint>(ref value);
                }

                case StatAuthoringType.Reduced:
                case StatAuthoringType.Less:
                {
                    var neg = -value;
                    return UnsafeUtility.As<float, uint>(ref neg);
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(modifierType));
            }
        }
    }
}
