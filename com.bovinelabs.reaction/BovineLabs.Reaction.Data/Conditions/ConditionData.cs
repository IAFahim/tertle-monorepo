// <copyright file="ConditionData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using BovineLabs.Reaction.Data.Core;

    /// <summary>
    /// Configuration data for a single condition in a reaction system, defining its type, target, operation, and behavior.
    /// </summary>
    public struct ConditionData
    {
        public ushort Key;

        public byte ConditionType;
        public bool IsEvent;
        public Target Target;
        public ConditionFeature Feature;

        public Equality Operation;
        public bool CustomComparison;
        public bool DestroyOnTargetDestroyed;
        public bool CancelActive;

        public ValueIndex ValueIndex;
    }
}