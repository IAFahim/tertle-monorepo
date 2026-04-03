// <copyright file="ActionTagBuilder.cs" company="BovineLabs">
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
    /// Builder for configuring tag addition/removal actions on target entities.
    /// </summary>
    public struct ActionTagBuilder : IDisposable
    {
        private NativeList<ActionTag> tags;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionTagBuilder"/> struct with the specified allocator.
        /// </summary>
        /// <param name="allocator">The allocator to use for internal collections.</param>
        public ActionTagBuilder(Allocator allocator)
        {
            this.tags = new NativeList<ActionTag>(allocator);
        }

        /// <summary>
        /// Releases all resources used by the ActionTagBuilder.
        /// </summary>
        public void Dispose()
        {
            this.tags.Dispose();
        }

        /// <summary>
        /// Adds a tag addition/removal action for the specified target.
        /// </summary>
        /// <param name="target">The target entity to add/remove tags on.</param>
        /// <param name="tag">The stable type hash of the tag component to add/remove.</param>
        public void WithTag(Target target, ulong tag)
        {
            this.tags.Add(new ActionTag { Target = target, Value = tag });
        }

        /// <summary>
        /// Adds multiple tag actions to the builder.
        /// </summary>
        /// <param name="tags">The array of tag actions to add.</param>
        public void WithTags(NativeArray<ActionTag> tags)
        {
            this.tags.AddRange(tags);
        }

        /// <summary>
        /// Applies the configured tag actions to the specified entity builder.
        /// </summary>
        /// <typeparam name="T">The type of entity command builder.</typeparam>
        /// <param name="builder">The entity builder to apply tag actions to.</param>
        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            if (this.tags.Length == 0)
            {
                return;
            }

            var addTags = builder.AddBuffer<ActionTag>();

            foreach (var t in this.tags)
            {
                addTags.Add(t);
            }
        }
    }
}
