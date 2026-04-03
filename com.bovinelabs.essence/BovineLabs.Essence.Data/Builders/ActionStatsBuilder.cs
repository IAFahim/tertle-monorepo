// <copyright file="ActionStatsBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data.Builders
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Essence.Data.Actions;
    using Unity.Collections;

    /// <summary>
    /// A builder utility for constructing action stat configurations and applying them to entities.
    /// </summary>
    public struct ActionStatsBuilder : IDisposable
    {
        private NativeList<ActionStat> stats;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionStatsBuilder"/> struct.
        /// </summary>
        /// <param name="allocator">The allocator to use for internal collections.</param>
        public ActionStatsBuilder(Allocator allocator)
        {
            this.stats = new NativeList<ActionStat>(allocator);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.stats.Dispose();
        }

        /// <summary>
        /// Adds a stat action to the builder.
        /// </summary>
        /// <param name="statToAdd">The stat action to add.</param>
        public void WithStat(in ActionStat statToAdd)
        {
            this.stats.Add(statToAdd);
        }

        /// <summary>
        /// Adds multiple stat actions to the builder.
        /// </summary>
        /// <param name="statsToAdd">The array of stat actions to add.</param>
        public void WithStats(in NativeArray<ActionStat> statsToAdd)
        {
            this.stats.AddRange(statsToAdd);
        }

        /// <summary>
        /// Applies the configured action stat settings to an entity builder.
        /// </summary>
        /// <typeparam name="T">The type of entity builder that implements IEntityCommands.</typeparam>
        /// <param name="builder">The entity builder to apply the action stat configuration to.</param>
        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            if (this.stats.Length == 0)
            {
                return;
            }

            var statEffects = builder.AddBuffer<ActionStat>();
            statEffects.AddRange(this.stats.AsArray());
        }
    }
}
