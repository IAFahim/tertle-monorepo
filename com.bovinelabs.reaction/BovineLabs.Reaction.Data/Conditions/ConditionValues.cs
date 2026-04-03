// <copyright file="ConditionValues.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using Unity.Entities;

    /// <summary>
    /// Buffer element storing condition values for recording and comparison during reaction processing.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct ConditionValues : IBufferElementData
    {
        public int Value;
    }
}
