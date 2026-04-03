// <copyright file="EntityComponentKey.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actions
{
    using System;
    using Unity.Entities;

    public readonly struct EntityComponentKey : IEquatable<EntityComponentKey>
    {
        public readonly Entity Entity;
        public readonly ComponentType Component;

        public EntityComponentKey(Entity entity, ComponentType component)
        {
            this.Entity = entity;
            this.Component = component;
        }

        /// <inheritdoc/>
        public bool Equals(EntityComponentKey other)
        {
            return this.Entity.Equals(other.Entity) && this.Component == other.Component;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return this.Entity.GetHashCode() * 397 ^ this.Component.GetHashCode();
            }
        }
    }
}
