// <copyright file="ConditionReset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    /// <summary>
    /// Component containing a bitmask of event-based conditions that require resetting every frame for proper state management.
    /// </summary>
    public struct ConditionReset : IComponentData
    {
        public BitArray32 Value;
    }
}
