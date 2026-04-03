// <copyright file="ConditionBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Builders
{
    using System;
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// Builder for constructing condition-based reaction systems with configurable trigger chances and reset behavior.
    /// </summary>
    public struct ConditionBuilder : IDisposable
    {
        private NativeList<ConditionData> conditions;
        private NativeList<int> eventValues;
        private ushort triggerChance;
        private bool doNotReset;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionBuilder"/> struct with the specified allocator.
        /// </summary>
        /// <param name="allocator">The allocator to use for internal collections.</param>
        public ConditionBuilder(Allocator allocator)
        {
            this.conditions = new NativeList<ConditionData>(allocator);
            this.eventValues = new NativeList<int>(allocator);
            this.doNotReset = false;
            this.triggerChance = ConditionChance.Multi;
        }

        /// <summary>
        /// Releases all resources used by the ConditionBuilder.
        /// </summary>
        public void Dispose()
        {
            this.conditions.Dispose();
        }

        /// <summary>
        /// Adds multiple conditions to the builder.
        /// </summary>
        /// <param name="value">The array of condition data to add.</param>
        public void WithConditions(in NativeArray<ConditionData> value)
        {
            this.conditions.AddRange(value);
        }

        /// <summary>
        /// Adds a single condition to the builder.
        /// </summary>
        /// <param name="value">The condition data to add.</param>
        public void WithCondition(in ConditionData value)
        {
            this.conditions.Add(value);
        }

        /// <summary>
        /// Set the event values to the builder. Note this will clear any existing values
        /// </summary>
        /// <param name="value">The array of event values to add.</param>
        public void WithEventValues(in NativeArray<int> value)
        {
            this.eventValues.Clear();
            this.eventValues.AddRange(value);
        }

        /// <summary>
        /// Configures whether conditions should reset after triggering.
        /// </summary>
        /// <param name="value">True to disable automatic reset; false to enable reset.</param>
        public void WithNoReset(bool value)
        {
            this.doNotReset = value;
        }

        /// <summary>
        /// Sets the probability that the reaction will trigger when conditions are met.
        /// </summary>
        /// <param name="chanceToTrigger">The chance to trigger (0.0 to 1.0).</param>
        public void WithChance(float chanceToTrigger)
        {
            chanceToTrigger *= ConditionChance.Multi;
            chanceToTrigger = math.round(chanceToTrigger);

            switch (chanceToTrigger)
            {
                case < 0:
                    BLGlobalLogger.LogError($"Can't have a chance to trigger < 0 {chanceToTrigger}");
                    break;
                case > ConditionChance.Multi:
                    BLGlobalLogger.LogWarning("chanceToTrigger > 1");
                    break;
                case 0:
                    BLGlobalLogger.LogWarning("chanceToTrigger is 0, will never trigger is this intended?");
                    break;
            }

            this.triggerChance = (ushort)math.clamp(chanceToTrigger, 0, ConditionChance.Multi);
        }

        /// <summary>
        /// Applies the configured conditions to the specified entity builder.
        /// </summary>
        /// <typeparam name="T">The type of entity command builder.</typeparam>
        /// <param name="builder">The entity builder to apply conditions to.</param>
        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            if (this.conditions.Length > ConditionActive.MaxConditions)
            {
                BLGlobalLogger.LogError($"More than {ConditionActive.MaxConditions} conditions used.");
                return;
            }

            if (this.conditions.Length == 0)
            {
                return;
            }

            using var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var meta = ref blobBuilder.ConstructRoot<ConditionMetaData>();

            var conditionSchemaArray = blobBuilder.Allocate(ref meta.Conditions, this.conditions.Length);

            var conditionCount = this.conditions.Length;
            builder.AddComponent<ConditionAllActive>();
            builder.SetComponentEnabled<ConditionAllActive>(false);

            var reset = uint.MaxValue << conditionCount;
            var active = uint.MaxValue << conditionCount;
            var recordValues = false;
            var anyConditionIsEvent = false;
            var anyCancel = false;

            for (var i = 0; i < this.conditions.Length; i++)
            {
                var data = this.conditions[i];

                if (data.IsEvent)
                {
                    anyConditionIsEvent = true;
                }

                if (data.CancelActive)
                {
                    anyCancel = true;
                }

                // States don't get reset
                if (!data.IsEvent)
                {
                    reset |= (byte)(1 << i);
                }

                if (data.Feature == ConditionFeature.Invalid)
                {
                    Debug.LogError("Condition.Feature invalid");
                }

                // If only being used for data then we don't reset and we mark it as always active
                if (!data.Feature.HasCondition())
                {
                    reset |= (byte)(1 << i);
                    active |= (byte)(1 << i);
                }

                if (data.Feature.HasValue())
                {
                    recordValues = true;
                }

                conditionSchemaArray[i] = data;
            }

            builder.AddComponent(new ConditionActive { Value = new BitArray32(active) });

            // If we have any event we need a reset unless told otherwise
            if (!this.doNotReset && anyConditionIsEvent)
            {
                builder.AddComponent(new ConditionReset { Value = new BitArray32(reset) });
            }

            if (anyCancel)
            {
                var bits = new BitArray32();
                for (var i = 0; i < this.conditions.Length; i++)
                {
                    bits[i] = this.conditions[i].CancelActive;
                }

                builder.AddComponent(new ConditionCancelActive { Value = bits });
            }

            if (this.eventValues.Length > 0)
            {
                builder.AddBuffer<ConditionComparisonValue>().Reinterpret<int>().AddRange(this.eventValues.AsArray());
            }

            var metaValue = blobBuilder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            builder.AddBlobAsset(ref metaValue, out _);
            builder.AddComponent(new ConditionMeta { Value = metaValue });

            // If any condition wants to record values
            if (recordValues)
            {
                var values = builder.AddBuffer<ConditionValues>();
                values.Resize(this.conditions.Length, NativeArrayOptions.ClearMemory);
            }

            // max value always triggers and is the common case, no need for component
            if (this.triggerChance < ConditionChance.Multi)
            {
                builder.AddComponent(new ConditionChance { Value = this.triggerChance });
            }
        }
    }
}
