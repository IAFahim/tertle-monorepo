// <copyright file="StatsBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data.Builders
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// A builder utility for constructing stat configurations and applying them to entities.
    /// </summary>
    public struct StatsBuilder : IDisposable
    {
        private NativeList<StatModifier> defaultValues;
        private bool canBeModified;
        private bool writeEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatsBuilder"/> struct.
        /// </summary>
        /// <param name="allocator">The allocator to use for internal collections.</param>
        public StatsBuilder(Allocator allocator)
        {
            this.canBeModified = true;
            this.writeEvents = false;
            this.defaultValues = new NativeList<StatModifier>(allocator);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.defaultValues.Dispose();
        }

        /// <summary>
        /// Adds a default stat modifier to the builder.
        /// </summary>
        /// <param name="value">The default stat modifier to add.</param>
        public void WithDefault(StatModifier value)
        {
            this.defaultValues.Add(value);
        }

        /// <summary>
        /// Adds multiple default stat modifiers to the builder.
        /// </summary>
        /// <param name="values">The array of default stat modifiers to add.</param>
        public void WithDefaults(NativeArray<StatModifier> values)
        {
            this.defaultValues.AddRange(values);
        }

        /// <summary>
        /// Sets whether the stats can be modified at runtime.
        /// </summary>
        /// <param name="value">True if stats can be modified, false otherwise.</param>
        public void WithCanBeModified(bool value)
        {
            this.canBeModified = value;
        }

        /// <summary>
        /// Sets whether stat change events should be written when stats are modified.
        /// </summary>
        /// <param name="value">True if stat change events should be written, false otherwise.</param>
        public void WithWriteEvents(bool value)
        {
            this.writeEvents = value;
        }

        /// <summary>
        /// Applies the configured stat settings to an entity builder.
        /// </summary>
        /// <typeparam name="T">The type of entity builder that implements IEntityCommands.</typeparam>
        /// <param name="builder">The entity builder to apply the stat configuration to.</param>
        public unsafe void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            var calc = new StatModifierCalculator(Allocator.Temp);
            foreach (var stat in this.defaultValues)
            {
                calc.Add(stat);
            }

#if UNITY_NETCODE
            builder.AddBuffer<StatGhost>();
#endif

            var stats = builder.AddBuffer<Stat>().Initialize().AsMap();
            calc.ApplyTo(ref stats);

            if (this.writeEvents)
            {
                builder.AddComponent<StatConditionDirty>();
            }

            if (this.canBeModified)
            {
                builder.AddBuffer<StatModifiers>();
                builder.AddComponent<StatChanged>();
                builder.SetComponentEnabled<StatChanged>(false);

                var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var statMeta = ref blobBuilder.ConstructRoot<StatDefaults.Data>();

                var defaultArray = blobBuilder.Allocate(ref statMeta.Default, this.defaultValues.Length);
                UnsafeUtility.MemCpy(defaultArray.GetUnsafePtr(), this.defaultValues.GetUnsafePtr(), sizeof(StatModifier) * this.defaultValues.Length);

                var blobReference = blobBuilder.CreateBlobAssetReference<StatDefaults.Data>(Allocator.Persistent);
                builder.AddBlobAsset(ref blobReference, out _);
                builder.AddComponent(new StatDefaults { Value = blobReference });
            }
        }
    }
}
