// <copyright file="ActionEnableableBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Builders
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Collections;

    /// <summary>
    /// Builder for configuring component enable/disable actions on target entities.
    /// </summary>
    public struct ActionEnableableBuilder : IDisposable
    {
        private NativeList<ActionEnableable> enableables;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionEnableableBuilder"/> struct with the specified allocator.
        /// </summary>
        /// <param name="allocator">The allocator to use for internal collections.</param>
        public ActionEnableableBuilder(Allocator allocator)
        {
            this.enableables = new NativeList<ActionEnableable>(allocator);
        }

        /// <summary>
        /// Releases all resources used by the ActionEnableableBuilder.
        /// </summary>
        public void Dispose()
        {
            this.enableables.Dispose();
        }

        /// <summary>
        /// Adds a component enable/disable action for the specified target.
        /// </summary>
        /// <param name="target">The target entity to enable/disable components on.</param>
        /// <param name="componentToEnable">The stable type hash of the component to enable/disable.</param>
        public void WithEnableable(Target target, ulong componentToEnable)
        {
            this.enableables.Add(new ActionEnableable { Target = target, Value = componentToEnable });
        }

        /// <summary>
        /// Adds multiple component enable/disable actions to the builder.
        /// </summary>
        /// <param name="componentsToEnable">The array of enableable actions to add.</param>
        public void WithEnableables(NativeArray<ActionEnableable> componentsToEnable)
        {
            this.enableables.AddRange(componentsToEnable);
        }

        /// <summary>
        /// Applies the configured enableable actions to the specified entity builder.
        /// </summary>
        /// <typeparam name="T">The type of entity command builder.</typeparam>
        /// <param name="builder">The entity builder to apply enableable actions to.</param>
        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            if (this.enableables.Length == 0)
            {
                return;
            }

            builder.AddBuffer<ActionEnableable>().AddRange(this.enableables.AsArray());
        }
    }
}
