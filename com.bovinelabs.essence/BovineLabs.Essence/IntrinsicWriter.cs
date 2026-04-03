// <copyright file="IntrinsicWriter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence
{
    using BovineLabs.Core;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Conditions;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Provides safe, validated modification of intrinsic values with automatic limit enforcement and event integration.
    /// This is the primary API for modifying intrinsic values in the essence system, ensuring all changes respect
    /// configured constraints and trigger appropriate condition events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IntrinsicWriter automatically handles value validation against both static limits (defined in IntrinsicConfig)
    /// and dynamic limits (derived from current stat values). All modifications are clamped to these limits,
    /// ensuring intrinsic values always remain within valid ranges.
    /// </para>
    /// <para>
    /// Key features:
    /// - Automatic value clamping based on static and dynamic limits
    /// - Condition event triggering when values change
    /// - Integration with the condition system for reactive behavior
    /// - Thread-safe design for use in job system contexts
    /// - Efficient batch processing support through various access patterns
    /// </para>
    /// <para>
    /// The writer supports three main access patterns:
    /// - Direct entity access via IntrinsicWriter.Lookup
    /// - Chunk-based processing via IntrinsicWriter.ResolvedChunk
    /// - System integration via IntrinsicWriter.TypeHandle
    /// </para>
    /// </remarks>
    public readonly partial struct IntrinsicWriter : IFacet
    {
        [Singleton]
        private readonly EssenceConfig essenceConfig;

        [FacetOptional]
        private readonly EnabledRefRW<IntrinsicConditionDirty> dirty;

        [ReadOnly]
        [FacetOptional]
        private readonly DynamicBuffer<Stat> stats;

        [Facet]
        [FacetOptional]
        private readonly ConditionEventWriter eventWriter;

        private readonly DynamicBuffer<Intrinsic> intrinsics;

        public EssenceConfig EssenceConfig => this.essenceConfig;

        /// <summary>
        /// Gets a reference to the intrinsic hash map for direct access to intrinsic values.
        /// Use this property when you need direct map access for bulk operations or iteration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property provides direct access to the underlying intrinsic storage. While it allows
        /// for efficient bulk operations, direct modifications bypass the validation and event
        /// triggering provided by Add(), Set(), and Subtract() methods.
        /// </para>
        /// <para>
        /// Use the modification methods (Add, Set, Subtract) instead of direct map access to ensure
        /// proper limit validation and condition event triggering.
        /// </para>
        /// </remarks>
        /// <value>A reference to the intrinsic hash map containing current intrinsic values.</value>
        public DynamicHashMap<IntrinsicKey, int> Intrinsics => this.intrinsics.AsMap();

        /// <summary>
        /// Gets a read-only reference to the stat hash map used for dynamic limit calculation.
        /// This property provides access to current stat values that determine intrinsic limits.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The Stats property is used internally by the writer to calculate dynamic minimum and maximum
        /// values for intrinsics based on current stat values. Some intrinsics are configured to use
        /// stat values as limits (e.g., HealthCurrent limited by HealthMax stat).
        /// </para>
        /// <para>
        /// This property is read-only to prevent modification of stat values through the intrinsic writer.
        /// Stats should be modified through the stat system, not the intrinsic writer.
        /// </para>
        /// </remarks>
        /// <value>A read-only reference to the stat hash map containing current stat values.</value>
        public DynamicHashMap<StatKey, StatValue> Stats => this.stats.AsMap();

        /// <summary>
        /// Gets a reference to the condition event writer for triggering reactive behaviors.
        /// This property provides access to the event system for custom event triggering.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The EventWriter is automatically used by the Add(), Set(), and Subtract() methods to trigger
        /// condition events when intrinsic values change. You can also use it directly to trigger
        /// custom events or check if event writing is available.
        /// </para>
        /// <para>
        /// The event writer may be invalid if the entity doesn't support condition events.
        /// Check EventWriter.IsValid before using it for custom event triggering.
        /// </para>
        /// </remarks>
        /// <value>A reference to the condition event writer for triggering events.</value>
        public ConditionEventWriter EventWriter => this.eventWriter;

        /// <summary>
        /// Adds a delta value to the specified intrinsic, automatically clamping the result within configured limits.
        /// This is the primary method for incrementing or decrementing intrinsic values safely.
        /// </summary>
        /// <param name="key">The intrinsic key identifying which intrinsic to modify.</param>
        /// <param name="delta">The amount to add to the intrinsic value. Can be negative to subtract.</param>
        /// <returns>The final clamped intrinsic value after applying the delta.</returns>
        /// <remarks>
        /// <para>
        /// This method ensures the intrinsic value remains within the valid range defined by:
        /// - Static minimum and maximum values from IntrinsicConfig
        /// - Dynamic minimum and maximum values from related stat values (if configured)
        /// </para>
        /// <para>
        /// If the intrinsic doesn't exist, it will be created with the default value from configuration
        /// before applying the delta. The method automatically triggers condition events when the
        /// value changes, enabling reactive behaviors.
        /// </para>
        /// <para>
        /// If the clamped result equals the current value (no actual change), no events are triggered
        /// and the condition dirty flag is not set.
        /// </para>
        /// </remarks>
        public int Add(IntrinsicKey key, int delta)
        {
            if (!this.EssenceConfig.Value.Value.IntrinsicDatas.TryGetValue(key, out var ptr))
            {
                BLGlobalLogger.LogError($"Key {key.Value} not found in the intrinsic config");
                return 0;
            }

            ref var data = ref ptr.Ref;
            ref var intrinsic = ref this.intrinsics.AsMap().GetOrAddRef(key, data.DefaultValue);

            var (min, max) = this.GetLimits(data);

            var before = intrinsic;
            intrinsic = math.clamp(intrinsic + delta, min, max);
            delta = intrinsic - before; // The actual delta

            if (Hint.Unlikely(delta == 0))
            {
                return intrinsic;
            }

            this.TryWriteConditions(data, delta);
            return intrinsic;
        }

        /// <summary>
        /// Subtracts a value from the specified intrinsic, automatically clamping the result within configured limits.
        /// This is a convenience method equivalent to calling Add() with a negative delta.
        /// </summary>
        /// <param name="key">The intrinsic key identifying which intrinsic to modify.</param>
        /// <param name="delta">The amount to subtract from the intrinsic value. Must be positive.</param>
        /// <returns>The final clamped intrinsic value after subtracting the delta.</returns>
        /// <remarks>
        /// <para>
        /// This method is functionally identical to Add(key, -delta) but provides clearer intent
        /// when subtracting values. It follows the same validation and event triggering rules as Add().
        /// </para>
        /// <para>
        /// The delta parameter should be positive - the method will negate it internally.
        /// For example, Subtract(healthKey, 10) will reduce the health intrinsic by 10.
        /// </para>
        /// </remarks>
        public int Subtract(IntrinsicKey key, int delta)
        {
            return this.Add(key, -delta);
        }

        /// <summary>
        /// Sets the specified intrinsic to an exact value, automatically clamping the result within configured limits.
        /// This method directly assigns a value rather than applying a delta.
        /// </summary>
        /// <param name="key">The intrinsic key identifying which intrinsic to modify.</param>
        /// <param name="value">The exact value to set for the intrinsic.</param>
        /// <returns>The final clamped intrinsic value after applying limits.</returns>
        /// <remarks>
        /// <para>
        /// This method sets the intrinsic to the specified value, then clamps it within the valid range
        /// defined by static and dynamic limits from configuration. Unlike Add(), this method doesn't
        /// consider the current value when determining the final result.
        /// </para>
        /// <para>
        /// If the intrinsic doesn't exist, it will be created with the default value from configuration,
        /// then immediately set to the specified value (after clamping). The method automatically
        /// triggers condition events when the value changes.
        /// </para>
        /// <para>
        /// Use Set() when you want to establish an absolute value, and Add() when you want to modify
        /// the current value by a relative amount.
        /// </para>
        /// </remarks>
        public int Set(IntrinsicKey key, int value)
        {
            if (!this.EssenceConfig.Value.Value.IntrinsicDatas.TryGetValue(key, out var ptr))
            {
                BLGlobalLogger.LogError($"Key {key.Value} not found in the intrinsic config");
                return 0;
            }

            ref var data = ref ptr.Ref;
            ref var intrinsic = ref this.intrinsics.AsMap().GetOrAddRef(key, data.DefaultValue);

            var (min, max) = this.GetLimits(data);

            var before = intrinsic;
            intrinsic = math.clamp(value, min, max);

            var delta = intrinsic - before;

            if (Hint.Unlikely(delta == 0))
            {
                return intrinsic;
            }

            this.TryWriteConditions(data, delta);
            return intrinsic;
        }

        internal void RestrictMin(IntrinsicKey key, float minStatValue)
        {
            ref var data = ref this.EssenceConfig.Value.Value.IntrinsicDatas[key];
            var intrinsic = this.intrinsics.AsMap().GetRef(key);
            if (!intrinsic.IsCreated)
            {
                return;
            }

            var min = (int)math.floor(minStatValue);

            var before = intrinsic.Ref;
            intrinsic.Ref = math.max(intrinsic.Ref, min);
            var delta = intrinsic.Ref - before; // The actual delta

            if (Hint.Likely(delta == 0))
            {
                return;
            }

            this.TryWriteConditions(data, delta);
        }

        internal void RestrictMax(IntrinsicKey key, float maxStatValue)
        {
            ref var data = ref this.EssenceConfig.Value.Value.IntrinsicDatas[key];
            var intrinsic = this.intrinsics.AsMap().GetRef(key);
            if (!intrinsic.IsCreated)
            {
                return;
            }

            var max = (int)math.floor(maxStatValue);

            var before = intrinsic.Ref;
            intrinsic.Ref = math.min(intrinsic.Ref, max);
            var delta = intrinsic.Ref - before; // The actual delta

            if (Hint.Likely(delta == 0))
            {
                return;
            }

            this.TryWriteConditions(data, delta);
        }

        private void TryWriteConditions(in EssenceConfig.IntrinsicData intrinsicData, int delta)
        {
            if (this.dirty.IsValid)
            {
                this.dirty.ValueRW = true;
            }

            if (this.eventWriter.IsValid && intrinsicData.Event != 0)
            {
                this.eventWriter.Trigger(intrinsicData.Event, delta);
            }
        }

        private (int Min, int Max) GetLimits(in EssenceConfig.IntrinsicData intrinsicData)
        {
            var min = intrinsicData.Min;
            var max = intrinsicData.Max;

            if (this.stats.IsCreated)
            {
                var statMap = this.stats.AsMap();

                // Get dynamic minimum if configured
                if (intrinsicData.MinStatKey != 0)
                {
                    if (statMap.TryGetValue(intrinsicData.MinStatKey, out var minStat))
                    {
                        min = (int)math.floor(minStat.Value);
                    }
                }

                // Get dynamic maximum if configured
                if (intrinsicData.MaxStatKey != 0)
                {
                    if (statMap.TryGetValue(intrinsicData.MaxStatKey, out var maxStat))
                    {
                        max = (int)math.floor(maxStat.Value);
                    }
                }
            }

            return (min, max);
        }

        public partial struct Lookup
        {
        }
    }
}
