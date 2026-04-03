// <copyright file="ConditionCancelActive.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    /// <summary>
    /// Component data tracking which conditions should cancel active reactions when triggered.
    /// </summary>
    public struct ConditionCancelActive : IComponentData
    {
        public BitArray32 Value;
    }
}