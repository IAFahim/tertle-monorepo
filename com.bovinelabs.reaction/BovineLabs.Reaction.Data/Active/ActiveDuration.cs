// <copyright file="ActiveDuration.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Component defining the total duration a reaction should remain active once triggered.
    /// </summary>
    public struct ActiveDuration : IComponentData
    {
        public float Value;
    }
}
