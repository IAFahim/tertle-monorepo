// <copyright file="ConditionEventWriter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Conditions
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Provides efficient access patterns for writing event-based condition data with support for multiple system architectures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="ConditionEventWriter"/> is a high-performance utility struct that wraps access to
    /// <see cref="ConditionEvent"/> buffers and <see cref="EventsDirty"/> components, providing a clean API
    /// for writing event data that will be processed by <see cref="ConditionEventWriteSystem"/>.
    /// </para>
    /// <para>
    /// This writer supports multiple access patterns to accommodate different system architectures:
    /// - **Direct Access**: For single-entity operations with known entity references
    /// - **Lookup Access**: For systems using <see cref="ComponentLookup{T}"/> and <see cref="BufferLookup{T}"/>
    /// - **Chunk Access**: For high-performance batch operations using <see cref="IJobChunk"/>
    /// - **Cached Access**: For systems using <see cref="EntityCache"/> for optimized repeated access
    /// </para>
    /// <para>
    /// **Key Features:**
    /// - **Thread-Safe Event Writing**: Uses atomic operations and proper synchronization for parallel access
    /// - **Duplicate Detection**: Prevents multiple writes of the same event key within a single frame
    /// - **Dirty Flag Management**: Automatically manages <see cref="EventsDirty"/> flags for efficient processing
    /// - **Zero-Value Validation**: Enforces non-zero event values to maintain data integrity
    /// - **Null Key Handling**: Safely handles null condition keys without errors
    /// </para>
    /// <para>
    /// **Performance Considerations:**
    /// Events are stored in a <see cref="DynamicHashMap{TKey,TValue}"/> for O(1) lookup performance during
    /// condition processing. The writer minimizes allocations and uses aggressive inlining for hot path operations.
    /// </para>
    /// </remarks>
    public readonly partial struct ConditionEventWriter : IFacet
    {
        private readonly EnabledRefRW<EventsDirty> eventsDirty;
        private readonly DynamicBuffer<ConditionEvent> conditionEvents;

        /// <summary>
        /// Gets a value indicating whether this writer instance is valid and ready for use.
        /// </summary>
        /// <value>
        /// <c>true</c> if the underlying events map is created and the writer can accept event data; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid => this.conditionEvents.IsCreated;

        /// <summary>
        /// Triggers an event with the specified key and value, marking the entity as having dirty event data.
        /// </summary>
        /// <param name="key">The condition key identifying the event type and target.</param>
        /// <param name="value">The event value to write. Must be non-zero.</param>
        /// <remarks>
        /// <para>
        /// This method performs the following validations and operations:
        /// 1. **Null Key Check**: Safely returns without action if the key is <see cref="ConditionKey.Null"/>
        /// 2. **Zero Value Validation**: Asserts that the value is non-zero to maintain data integrity
        /// 3. **Duplicate Prevention**: Attempts to add the key-value pair, logging errors if the key already exists
        /// 4. **Dirty Flag Update**: Sets the <see cref="EventsDirty"/> flag to signal that processing is needed
        /// </para>
        /// <para>
        /// **Thread Safety**: This method is thread-safe when used with appropriate synchronization.
        /// Multiple threads can write to different entities simultaneously, but writing the same event
        /// key multiple times within a frame from different threads may cause race conditions.
        /// </para>
        /// <para>
        /// **Performance**: Uses aggressive inlining for optimal performance in hot code paths.
        /// The duplicate check is only active in development builds with collections checks enabled.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Trigger(in ConditionKey key, int value)
        {
            if (key.Equals(ConditionKey.Null))
            {
                return;
            }

            Check.Assume(value != 0, "Can't write 0 value event");
            var result = this.conditionEvents.AsMap().TryAdd(key, value);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!result)
            {
                Debug.LogError($"Trying to write an event {key.Value} multiple times in a frame. Each event should only be written from 1 place.");
            }
#endif

            this.eventsDirty.ValueRW = true;
        }

        /// <summary> Provides entity-based lookup access to <see cref="ConditionEventWriter"/> instances for <see cref="IJobEntity"/> </summary>
        public partial struct Lookup
        {
        }
    }
}
