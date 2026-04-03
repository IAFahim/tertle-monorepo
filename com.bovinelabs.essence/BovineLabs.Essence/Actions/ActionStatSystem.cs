// <copyright file="ActionStatSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Actions
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;

    /// <summary>
    /// Applies stat modifiers to target entities when actions activate.
    /// This system processes newly activated actions and adds their stat modifiers to affected entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the ActiveEnabledSystemGroup and processes actions that have just become
    /// active (Active enabled, ActivePrevious disabled). It supports multiple value types for
    /// calculating modifier values: Fixed, Linear (condition-based), and Range.
    /// </para>
    /// <para>
    /// The activation process:
    /// 1. Identifies newly activated actions with ActionStat components
    /// 2. Calculates modifier values based on value type (Fixed/Linear/Range)
    /// 3. Resolves target entities using the Targets system
    /// 4. Adds stat modifiers to target entities' StatModifiers buffers
    /// 5. Enables StatChanged component to trigger stat recalculation
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveEnabledSystemGroup))]
    public partial struct ActionStatSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var addStats = new NativeQueue<AddStatModifier>(state.WorldUpdateAllocator);

            var targetsCustom = SystemAPI.GetComponentLookup<TargetsCustom>(true);

            state.Dependency = new ActivateJob
                {
                    ConditionValuesHandle = SystemAPI.GetBufferTypeHandle<ConditionValues>(true),
                    TargetsCustoms = targetsCustom,
                    AddStatModifiers = addStats.AsParallelWriter(),
                }
                .ScheduleParallel(state.Dependency);

            state.Dependency = new ApplyJob
                {
                    AddStatModifiers = addStats,
                    StatModifiers = SystemAPI.GetBufferLookup<StatModifiers>(),
                    StatChangeds = SystemAPI.GetComponentLookup<StatChanged>(),
                }
                .Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Active))]
        [WithDisabled(typeof(ActivePrevious))]
        private partial struct ActivateJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public NativeQueue<AddStatModifier>.ParallelWriter AddStatModifiers;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            [ReadOnly]
            public BufferTypeHandle<ConditionValues> ConditionValuesHandle;

            [NativeDisableContainerSafetyRestriction]
            private BufferAccessor<ConditionValues> conditionValues;

            private int entityIndex;

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.conditionValues = chunk.GetBufferAccessor(ref this.ConditionValuesHandle);
                this.entityIndex = -1;
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }

            private void Execute(Entity entity, in DynamicBuffer<ActionStat> actionStats, in Targets targets)
            {
                this.entityIndex++;

                foreach (var actionStat in actionStats)
                {
                    Check.Assume(
                        actionStat.ValueType != StatValueType.Linear || this.conditionValues.Length > 0,
                        "Missing ConditionValues but using Linear condition scaling.");

                    var target = targets.Get(actionStat.Target, entity, this.TargetsCustoms);

                    var statModifier = new StatModifier
                    {
                        Type = actionStat.Type,
                        ModifyType = actionStat.ModifyType,
                        ValueRaw = actionStat.ValueType switch
                        {
                            StatValueType.Fixed => ApplyFixed(actionStat),
                            StatValueType.Linear => ApplyLinear(actionStat, this.conditionValues[this.entityIndex]),
                            StatValueType.Range => GetRangeValue(actionStat),
                            _ => 0,
                        },
                    };

                    this.AddStatModifiers.Enqueue(new AddStatModifier
                    {
                        Source = entity,
                        Target = target,
                        Modifier = statModifier,
                    });
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static uint ApplyFixed(in ActionStat actionStat)
            {
                return actionStat.Fixed.Raw;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static uint GetRangeValue(in ActionStat actionStat)
            {
                return actionStat.Range.Value.Raw;
            }

            private static uint ApplyLinear(in ActionStat actionStat, DynamicBuffer<ConditionValues> conditionValues)
            {
                var linear = actionStat.Linear;

                var value = conditionValues[linear.Index].Value;
                value = math.clamp(value, linear.FromMin, linear.FromMax);

                if (actionStat.ModifyType == StatModifyType.Added)
                {
                    var remapped = (int)math.remap(linear.FromMin, linear.FromMax, linear.ToMin.Int, linear.ToMax.Int, value);
                    return UnsafeUtility.As<int, uint>(ref remapped);
                }
                else
                {
                    var remapped = math.remap(linear.FromMin, linear.FromMax, linear.ToMin.Float, linear.ToMax.Float, value);
                    return UnsafeUtility.As<float, uint>(ref remapped);
                }
            }
        }

        [BurstCompile]
        private struct ApplyJob : IJob
        {
            public NativeQueue<AddStatModifier> AddStatModifiers;

            public BufferLookup<StatModifiers> StatModifiers;

            public ComponentLookup<StatChanged> StatChangeds;

            public void Execute()
            {
                while (this.AddStatModifiers.TryDequeue(out var add))
                {
                    if (!this.StatModifiers.TryGetBuffer(add.Target, out var statModifierBuffer))
                    {
                        continue;
                    }

                    statModifierBuffer.Add(new StatModifiers
                    {
                        SourceEntity = add.Source,
                        Value = add.Modifier,
                    });

                    this.StatChangeds.SetComponentEnabled(add.Target, true);
                }
            }
        }

        private struct AddStatModifier
        {
            public Entity Target;
            public Entity Source;
            public StatModifier Modifier;
        }
    }
}
