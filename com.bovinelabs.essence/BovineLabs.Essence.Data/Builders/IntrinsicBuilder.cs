// <copyright file="IntrinsicBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data.Builders
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using Unity.Collections;

    /// <summary>
    /// A builder utility for constructing intrinsic configurations and applying them to entities.
    /// </summary>
    public struct IntrinsicBuilder : IDisposable
    {
        private NativeList<Default> defaultValues;
        private bool writeEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntrinsicBuilder"/> struct.
        /// </summary>
        /// <param name="allocator">The allocator to use for internal collections.</param>
        public IntrinsicBuilder(Allocator allocator)
        {
            this.writeEvents = false;
            this.defaultValues = new NativeList<Default>(allocator);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.defaultValues.Dispose();
        }

        /// <summary>
        /// Adds a default intrinsic value to the builder.
        /// </summary>
        /// <param name="value">The default intrinsic value to add.</param>
        public void WithDefault(Default value)
        {
            this.defaultValues.Add(value);
        }

        /// <summary>
        /// Adds multiple default intrinsic values to the builder.
        /// </summary>
        /// <param name="values">The array of default intrinsic values to add.</param>
        public void WithDefaults(NativeArray<Default> values)
        {
            this.defaultValues.AddRange(values);
        }

        /// <summary>
        /// Sets whether intrinsic change events should be written when intrinsics are modified.
        /// </summary>
        /// <param name="value">True if intrinsic change events should be written, false otherwise.</param>
        public void WithWriteEvents(bool value)
        {
            this.writeEvents = value;
        }

        /// <summary>
        /// Applies the configured intrinsic settings to an entity builder.
        /// </summary>
        /// <typeparam name="T">The type of entity builder that implements IEntityCommands.</typeparam>
        /// <param name="builder">The entity builder to apply the intrinsic configuration to.</param>
        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
#if UNITY_NETCODE
            builder.AddBuffer<IntrinsicGhost>();
#endif

            var intrinsics = builder.AddBuffer<Intrinsic>().Initialize().AsMap();

            foreach (var intrinsic in this.defaultValues)
            {
                intrinsics.GetOrAddRef(intrinsic.Key) += intrinsic.Value;
            }

            if (this.writeEvents)
            {
                builder.AddComponent<IntrinsicConditionDirty>();
            }
        }

        /// <summary>
        /// Represents a default intrinsic value configuration with a key-value pair.
        /// </summary>
        public readonly struct Default
        {
            public readonly IntrinsicKey Key;
            public readonly int Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="Default"/> struct.
            /// </summary>
            /// <param name="key">The intrinsic key.</param>
            /// <param name="value">The default value for the intrinsic.</param>
            public Default(IntrinsicKey key, int value)
            {
                this.Key = key;
                this.Value = value;
            }
        }
    }
}
