// <copyright file="ConditionComparisonValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct ConditionComparisonValue : IBufferElementData
    {
        public int Value;
    }
}
