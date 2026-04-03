// <copyright file="ActionStatAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring.Actions
{
    using System;
    using BovineLabs.Core.Authoring.EntityCommands;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Essence.Data.Actions;
    using BovineLabs.Essence.Data.Builders;
    using BovineLabs.Reaction.Authoring;
    using BovineLabs.Reaction.Authoring.Conditions;
    using BovineLabs.Reaction.Authoring.Core;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring component for configuring stat modifications that occur as part of a reaction action.
    /// This component defines how stats should be modified when a reaction is triggered, supporting various value types and modification strategies.
    /// </summary>
    [ReactionAuthoring]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReactionAuthoring))]
    public class ActionStatAuthoring : MonoBehaviour
    {
        public Data[] Stats = Array.Empty<Data>();

        /// <summary>
        /// Configuration data for a single stat modification action.
        /// Defines which stat to modify, how to modify it, and the value calculation method.
        /// </summary>
        [Serializable]
        public class Data
        {
            public StatSchemaObject? StatSchema;

            public Target Target = Target.Target;
            public StatValueType ValueType = StatValueType.Fixed;

            public Fixed Fixed = new();
            public Linear Linear = new();
            public Range Range = new();
        }

        /// <summary>
        /// Configuration for fixed-value stat modifications.
        /// Represents a constant value that will be applied to the stat.
        /// </summary>
        [Serializable]
        public class Fixed
        {
            public StatAuthoringType ModifyType = StatAuthoringType.Added;
            public float Value;
        }

        /// <summary>
        /// Configuration for linear scaling stat modifications.
        /// Maps a condition value from one range to another range for dynamic stat calculations.
        /// </summary>
        [Serializable]
        public class Linear
        {
            public StatAuthoringType ModifyType = StatAuthoringType.Added;
            public ConditionSchemaObject? ConditionSchemaObject;

            [Min(0)]
            public int FromMin;

            [Min(0)]
            public int FromMax = 100;

            public float ToMin;
            public float ToMax = 1;
        }

        /// <summary>
        /// Configuration for random range stat modifications.
        /// Defines minimum and maximum values for random stat modification calculations.
        /// </summary>
        [Serializable]
        public class Range
        {
            public StatAuthoringType ModifyType = StatAuthoringType.Added;

            public float Min;
            public float Max;
        }

        private class Baker : Baker<ActionStatAuthoring>
        {
            /// <inheritdoc/>
            public override void Bake(ActionStatAuthoring authoring)
            {
                var builder = new ActionStatsBuilder(Allocator.Temp);

                var conditionAuthoring = this.GetComponent<ReactionAuthoring>().Conditions;

                foreach (var stat in authoring.Stats)
                {
                    if (stat.StatSchema == null)
                    {
                        Debug.LogWarning("Null stat");
                        continue;
                    }

                    if (stat.StatSchema.Key == 0)
                    {
                        Debug.LogWarning("Trying to modify the null stat 0");
                        continue;
                    }

                    var statToAdd = new ActionStat
                    {
                        Type = stat.StatSchema.Key,
                        ValueType = stat.ValueType,
                        Target = stat.Target,
                    };

                    switch (stat.ValueType)
                    {
                        case StatValueType.Fixed:
                        {
                            statToAdd.ModifyType = StatAuthoringUtil.GetModifier(stat.Fixed.ModifyType);
                            statToAdd.Fixed = GetValue(stat.Fixed.ModifyType, stat.Fixed.Value);
                            break;
                        }

                        case StatValueType.Linear:
                        {
                            var condition = stat.Linear.ConditionSchemaObject;
                            if (condition == null)
                            {
                                Debug.LogWarning("Null condition being used for Linear scaling");
                            }

                            var index = conditionAuthoring.Conditions.IndexOf(t => t.Condition == stat.Linear.ConditionSchemaObject);
                            if (index == -1)
                            {
                                Debug.LogError($"Condition {stat.Linear.ConditionSchemaObject} was not added as a condition");
                                continue;
                            }

                            if (!conditionAuthoring.Conditions[index].Features.HasValue())
                            {
                                Debug.LogError($"Condition {stat.Linear.ConditionSchemaObject} was not marked to be recorded");
                                continue;
                            }

                            statToAdd.ModifyType = StatAuthoringUtil.GetModifier(stat.Linear.ModifyType);
                            statToAdd.Linear = new ActionStat.LinearData
                            {
                                Index = (byte)index,
                                FromMin = (ushort)stat.Linear.FromMin,
                                FromMax = (ushort)stat.Linear.FromMax,
                                ToMin = GetValue(stat.Linear.ModifyType, stat.Linear.ToMin),
                                ToMax = GetValue(stat.Linear.ModifyType, stat.Linear.ToMax),
                            };
                            break;
                        }

                        case StatValueType.Range:
                        {
                            var min = stat.Range.Min;
                            var max = stat.Range.Max;

                            // If we're a negative type we swap max and min because they'll be negative so the max value will now be smaller
                            if (stat.Range.ModifyType is StatAuthoringType.Subtracted or StatAuthoringType.Reduced or StatAuthoringType.Less)
                            {
                                (min, max) = (max, min);
                            }

                            statToAdd.ModifyType = StatAuthoringUtil.GetModifier(stat.Range.ModifyType);
                            statToAdd.Range = new ActionStat.RangeData
                            {
                                Min = GetValue(stat.Range.ModifyType, min),
                                Max = GetValue(stat.Range.ModifyType, max),
                            };

                            break;
                        }

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    builder.WithStat(statToAdd);
                }

                var commands = new BakerCommands(this, this.GetEntity(TransformUsageFlags.None));
                builder.ApplyTo(ref commands);
            }

            private static ActionStat.ValueUnion GetValue(StatAuthoringType modifierType, float value)
            {
                return new ActionStat.ValueUnion { Raw = StatAuthoringUtil.GetValueRaw(modifierType, value) };
            }
        }
    }
}
