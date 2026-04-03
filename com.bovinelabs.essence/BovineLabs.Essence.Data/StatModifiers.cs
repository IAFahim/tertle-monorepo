// <copyright file="StatModifiers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using Unity.Entities;
    using Unity.NetCode;

    /// <summary>
    /// A buffer component that stores stat modifiers applied to an entity.
    /// </summary>
    [InternalBufferCapacity(0)]
    [GhostComponent(PrefabType=GhostPrefabType.Server)]
    public struct StatModifiers : IBufferElementData
    {
        public Entity SourceEntity;
        public StatModifier Value;
    }
}
