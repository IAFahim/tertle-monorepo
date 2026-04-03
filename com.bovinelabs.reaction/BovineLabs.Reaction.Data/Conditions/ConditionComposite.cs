// <copyright file="ConditionComposite.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using Unity.Entities;

    /// <summary>
    /// Component containing complex boolean logic definition for reaction conditions.
    /// When present, the system evaluates this composite logic instead of simple AND logic to set <see cref="ConditionAllActive"/>.
    /// Supports AND, OR, XOR, NOT operations with nested grouping and parentheses for advanced condition combinations.
    /// </summary>
    [WriteGroup(typeof(ConditionAllActive))]
    public struct ConditionComposite : IComponentData
    {
        /// <summary>
        /// Reference to the blob asset containing the composite logic definition.
        /// </summary>
        public BlobAssetReference<CompositeLogic> Logic;
    }
}
