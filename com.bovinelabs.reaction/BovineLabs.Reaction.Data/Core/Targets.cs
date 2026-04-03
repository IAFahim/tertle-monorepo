// <copyright file="Targets.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Core
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core;
    using Unity.Entities;
    using Unity.Properties;
    using UnityEngine;

    /// <summary>
    /// Component storing target entity references (Owner, Source, Target) for reaction processing.
    /// </summary>
    public struct Targets : IComponentData
    {
        public Entity Owner;
        public Entity Source;
        public Entity Target;

        /// <summary>
        /// Creates a copy of this Targets struct with updated source and target entities.
        /// </summary>
        /// <param name="source">The new source entity.</param>
        /// <param name="target">The new target entity (uses current target if default).</param>
        /// <returns>A new Targets struct with the specified values.</returns>
        public readonly Targets Copy(Entity source, Entity target = default)
        {
            var targets = default(Targets);
            targets.Owner = this.Owner;
            targets.Source = source;
            targets.Target = target == default ? this.Target : target;
            return targets;
        }

        /// <summary>
        /// Gets the entity reference for the specified target type.
        /// </summary>
        /// <param name="target">The target type to resolve.</param>
        /// <param name="self">The current entity (used for Self and Custom targets).</param>
        /// <param name="targetsCustoms">Lookup for custom target components.</param>
        /// <returns>The resolved entity for the target type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Entity Get(in Target target, in Entity self, in ComponentLookup<TargetsCustom> targetsCustoms)
        {
            return target switch
            {
                Core.Target.None => Entity.Null,
                Core.Target.Target => this.Target,
                Core.Target.Owner => this.Owner,
                Core.Target.Source => this.Source,
                Core.Target.Self => self,
                Core.Target.Custom0 => GetCustom(0, self, targetsCustoms),
                Core.Target.Custom1 => GetCustom(1, self, targetsCustoms),
                _ => Entity.Null,
            };
        }

        private static Entity GetCustom(in int index, in Entity self, ComponentLookup<TargetsCustom> targetsCustoms)
        {
            if (!targetsCustoms.TryGetComponent(self, out var target))
            {
                Debug.LogError($"Trying to get custom targets on {self.ToFixedString()} but doesn't have TargetsCustom component");
                return Entity.Null;
            }

            return index == 0 ? target.Target0 : target.Target1;
        }
    }
}
