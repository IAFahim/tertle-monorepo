// <copyright file="ActionIntrinsicBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data.Builders
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Essence.Data.Actions;
    using Unity.Collections;

    /// <summary>
    /// A builder utility for constructing action intrinsic configurations and applying them to entities.
    /// </summary>
    public struct ActionIntrinsicBuilder : IDisposable
    {
        private NativeList<ActionIntrinsic> intrinsics;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionIntrinsicBuilder"/> struct.
        /// </summary>
        /// <param name="allocator">The allocator to use for internal collections.</param>
        public ActionIntrinsicBuilder(Allocator allocator)
        {
            this.intrinsics = new NativeList<ActionIntrinsic>(allocator);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.intrinsics.Dispose();
        }

        /// <summary>
        /// Adds an intrinsic action to the builder.
        /// </summary>
        /// <param name="intrinsicToAdd">The intrinsic action to add.</param>
        public void WithIntrinsic(in ActionIntrinsic intrinsicToAdd)
        {
            this.intrinsics.Add(intrinsicToAdd);
        }

        /// <summary>
        /// Adds multiple intrinsic actions to the builder.
        /// </summary>
        /// <param name="intrinsicsToAdd">The array of intrinsic actions to add.</param>
        public void WithIntrinsics(in NativeArray<ActionIntrinsic> intrinsicsToAdd)
        {
            this.intrinsics.AddRange(intrinsicsToAdd);
        }

        /// <summary>
        /// Applies the configured action intrinsic settings to an entity builder.
        /// </summary>
        /// <typeparam name="T">The type of entity builder that implements IEntityCommands.</typeparam>
        /// <param name="builder">The entity builder to apply the action intrinsic configuration to.</param>
        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            if (this.intrinsics.Length == 0)
            {
                return;
            }

            var statEffects = builder.AddBuffer<ActionIntrinsic>();
            statEffects.AddRange(this.intrinsics.AsArray());
        }
    }
}
