// <copyright file="ActionCreateBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Builders
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Collections;

    /// <summary>
    /// Builder for configuring entity creation actions with target assignment and destruction settings.
    /// </summary>
    public struct ActionCreateBuilder : IDisposable
    {
        private NativeList<ActionCreate> entitiesToCreate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionCreateBuilder"/> struct with the specified allocator.
        /// </summary>
        /// <param name="allocator">The allocator to use for internal collections.</param>
        public ActionCreateBuilder(Allocator allocator)
        {
            this.entitiesToCreate = new NativeList<ActionCreate>(allocator);
        }

        /// <summary>
        /// Releases all resources used by the ActionCreateBuilder.
        /// </summary>
        public void Dispose()
        {
            this.entitiesToCreate.Dispose();
        }

        /// <summary>
        /// Adds an entity creation action with the specified target and destruction behavior.
        /// </summary>
        /// <param name="create">The object ID of the entity to create.</param>
        /// <param name="target">The target assignment for the created entity.</param>
        /// <param name="destroyOnDisable">Whether to destroy the entity when the reaction is disabled.</param>
        public void WithCreate(ObjectId create, Target target, bool destroyOnDisable)
        {
            this.entitiesToCreate.Add(new ActionCreate { Target = target, Id = create, DestroyOnDisabled = destroyOnDisable });
        }

        /// <summary>
        /// Adds multiple entity creation actions to the builder.
        /// </summary>
        /// <param name="entities">The array of creation actions to add.</param>
        public void WithCreates(in NativeArray<ActionCreate> entities)
        {
            this.entitiesToCreate.AddRange(entities);
        }

        /// <summary>
        /// Applies the configured creation actions to the specified entity builder.
        /// </summary>
        /// <typeparam name="T">The type of entity command builder.</typeparam>
        /// <param name="builder">The entity builder to apply creation actions to.</param>
        /// <returns>True if any actions were applied; false if no actions were configured.</returns>
        public bool ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            if (this.entitiesToCreate.Length == 0)
            {
                return false;
            }

            var effectCreate = builder.AddBuffer<ActionCreate>();
            var anyDestroy = false;

            foreach (var create in this.entitiesToCreate)
            {
                if (create.DestroyOnDisabled)
                {
                    anyDestroy = true;
                }

                effectCreate.Add(create);
            }

            if (anyDestroy)
            {
                builder.AddBuffer<ActionCreated>();
            }

            return true;
        }
    }
}
