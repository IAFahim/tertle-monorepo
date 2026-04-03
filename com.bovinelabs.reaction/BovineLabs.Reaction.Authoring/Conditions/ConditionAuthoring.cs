// <copyright file="ConditionAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Conditions
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.EntityCommands;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Reaction.Authoring.Active;
    using BovineLabs.Reaction.Data.Builders;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using JetBrains.Annotations;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring class for configuring conditions that control when reactions are triggered.
    /// Supports up to 8 conditions with chance-based triggering and reset behavior.
    /// </summary>
    [Serializable]
    public class ConditionAuthoring
    {
        [SerializeField]
        [Tooltip("The list of all conditions. Max number of conditions allowed is 8.")]
        private List<ConditionData> conditions = new();

        [SerializeField]
        [Tooltip("Optional: Use composite conditions for complex boolean logic. Leave empty to use simple AND logic.")]
        private ConditionCompositeAuthoring conditionLogic = new();

        [SerializeField]
        [Range(0, 1)]
        [Tooltip("Percent chance for the conditions to trigger. e.g. 20% chance on hit.")]
        private float chanceToTrigger = 1f;

        [SerializeField]
        [Tooltip("If set, once all conditions have been met it will remain true forever. e.g. quests")]
        private bool doNotReset;

        public IReadOnlyList<ConditionData> Conditions => this.conditions;

        public void OnValidate(ActiveAuthoring activeAuthoring)
        {
            var hasDuration = activeAuthoring.Duration > 0;

            var anyCancel = false;
            var durationWarning = false;

            for (var index = 0; index < this.conditions.Count; index++)
            {
                var c = this.conditions[index];
                c.Name = c.Condition == null ? "Null" : c.Condition.name;

                if (!c.CancelActive)
                {
                    continue;
                }

                if (hasDuration)
                {
                    anyCancel = true;
                }
                else
                {
                    c.CancelActive = false;
                    this.conditions[index] = c;

                    durationWarning = true;
                }
            }

            if (durationWarning)
            {
                Debug.LogWarning("Can't use Cancel without Active having a duration.");
            }
            else if (anyCancel && !activeAuthoring.Cancellable)
            {
                Debug.Log($"Condition has cancel therefore setting {nameof(activeAuthoring.Cancellable)} to true on {nameof(ActiveAuthoring)}");
                activeAuthoring.Cancellable = true;
            }
        }

        public void Bake(IBaker baker, Entity entity)
        {
            var conditionTypes = baker.DependsOn(AuthoringSettingsUtility.GetSettings<ConditionTypes>());

            var conditionList = new NativeList<Data.Conditions.ConditionData>(this.conditions.Count, Allocator.Temp);
            var eventValues = new NativeList<int>(Allocator.Temp);

            // Cache baked data in custom comparisons to avoid adding multiple times
            var bakedData = new Dictionary<Type, object>();

            foreach (var data in this.conditions)
            {
                if (!data.Condition)
                {
                    Debug.LogError("Null condition.");
                    continue;
                }

                if (data.Features is ConditionFeature.Invalid)
                {
                    Debug.LogError("Condition set to invalid");
                    continue;
                }

                baker.DependsOn(data.Condition);

                ValueIndex valueIndex = default;
                bool isCustom = false;

                if (data.Operation != Equality.Any)
                {
                    if (data.Operation == Equality.Between)
                    {
                        isCustom |= AddValue(ref valueIndex, baker, bakedData, eventValues, data.MinComparisonMode, data.ValueMin, data.CustomValueMin);
                        isCustom |= AddValue(ref valueIndex, baker, bakedData, eventValues, data.MaxComparisonMode, data.ValueMax, data.CustomValueMax);
                    }
                    else
                    {
                        isCustom |= AddValue(ref valueIndex, baker, bakedData, eventValues, data.ComparisonMode, data.Value, data.CustomValue);
                    }
                }

                conditionList.Add(new Data.Conditions.ConditionData
                {
                    Key = data.Condition.Key,
                    ConditionType = conditionTypes[data.Condition.ConditionType],
                    IsEvent = data.Condition.IsEvent,
                    Target = data.Condition.IsGlobal ? Target.None : data.Target,
                    Operation = data.Operation,
                    DestroyOnTargetDestroyed = data.DestroyIfTargetDestroyed,
                    ValueIndex = valueIndex,
                    Feature = data.Features,
                    CustomComparison = isCustom,
                    CancelActive = data.CancelActive,
                });
            }

            var builder = new BakerCommands(baker, entity);
            var cb = new ConditionBuilder(Allocator.Temp);
            cb.WithConditions(conditionList.AsArray());
            cb.WithEventValues(eventValues.AsArray());
            cb.WithNoReset(this.doNotReset);
            cb.WithChance(this.chanceToTrigger);
            cb.ApplyTo(ref builder);

            // Bake composite conditions if defined
            this.conditionLogic.Bake(ref builder);
        }

        private static bool AddValue(
            ref ValueIndex valueIndex, IBaker baker, Dictionary<Type, object> bakedData, NativeList<int> eventValues,
            ConditionData.ConditionComparisonMode comparisonMode, int value, ICustomComparison? customComparison)
        {
            valueIndex.Value = (byte)eventValues.Length;

            if (comparisonMode == ConditionData.ConditionComparisonMode.Constant)
            {
                eventValues.Add(value);
                return false;
            }

            // Still need to populate fields
            eventValues.Add(0);

            if (customComparison == null)
            {
                Debug.LogError("The comparison mode has not been set when using custom data.");
            }
            else
            {
                customComparison.Bake(baker, bakedData, valueIndex.Value);
            }

            return true;
        }

        /// <summary>
        /// Configuration data for a single condition within the ConditionAuthoring.
        /// </summary>
        [Serializable]
        public class ConditionData
        {
            public string Name = string.Empty;

            public ConditionSchemaObject? Condition;

            [Tooltip("What should the effect look at for its conditions when created? Usually Target or Owner.")]
            public Target Target = Target.Target;
            public Equality Operation;

            [Header("Value")]
            public ConditionComparisonMode ComparisonMode = ConditionComparisonMode.Constant;
            public int Value;
            [SerializeReference]
            public ICustomComparison? CustomValue;

            [Header("Min")]
            public ConditionComparisonMode MinComparisonMode = ConditionComparisonMode.Constant;
            public int ValueMin;
            [SerializeReference]
            public ICustomComparison? CustomValueMin;

            [Header("Max")]
            public ConditionComparisonMode MaxComparisonMode = ConditionComparisonMode.Constant;
            public int ValueMax;
            [SerializeReference]
            public ICustomComparison? CustomValueMax;

            [Header("Features")]
            [Tooltip("Invalid - Using this is an error.\n\n" + "Condition - Condition will be used in ConditionActive to check if active.\n\n" +
                "Value - Condition value will be recorded into ConditionValues.\n\n" +
                "Accumulate - Instead of replacing, each event will accumulate it's value in ConditionValues. This only works for Events not States.")]
            public ConditionFeature Features = ConditionFeature.Condition;

            [Tooltip("Cleanup this reaction entity if the target is destroyed. " +
                "The conditional will no longer work if target is destroyed so this is the expected behaviour. " +
                "However, sometimes you might want to manually setup a new target so don't want to destroy.")]
            public bool DestroyIfTargetDestroyed = true;

            [Tooltip("If set true and the effect is active, if this condition becomes invalid it will cancel the effect.")]
            public bool CancelActive;

            public enum ConditionComparisonMode : byte
            {
                Constant,
                [UsedImplicitly]
                Custom,
            }
        }
    }
}
