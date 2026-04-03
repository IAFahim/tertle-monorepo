// <copyright file="GlobalCondition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using System;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    internal readonly struct ConditionGlobal : IBufferElementData, IEquatable<ConditionGlobal>
    {
        public readonly ushort Key;
        public readonly byte ConditionType;

        public ConditionGlobal(ushort key, byte conditionType)
        {
            this.Key = key;
            this.ConditionType = conditionType;
        }

        public bool Equals(ConditionGlobal other)
        {
            return this.Key == other.Key && this.ConditionType == other.ConditionType;
        }

        public override bool Equals(object? obj)
        {
            return obj is ConditionGlobal other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Key.GetHashCode() * 397) ^ this.ConditionType.GetHashCode();
            }
        }
    }
}
