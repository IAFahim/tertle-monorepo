// <copyright file="ConditionAllActiveSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Conditions
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Utility;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Entities;

    /// <summary>
    /// Evaluates condition logic and determines if all required conditions are met for reaction activation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ConditionsSystemGroup"/> and is the primary system responsible for
    /// evaluating whether condition-based reactions should be active. It processes entities with
    /// <see cref="ConditionAllActive"/> components and uses change filters on <see cref="ConditionActive"/>
    /// to optimize performance.
    /// </para>
    /// <para>
    /// The system supports two distinct evaluation modes:
    /// 1. **Simple AND Logic**: For entities without <see cref="ConditionComposite"/>, all conditions in
    ///    the <see cref="ConditionActive"/> bitmask must be true for the reaction to activate
    /// 2. **Complex Boolean Logic**: For entities with <see cref="ConditionComposite"/>, evaluates
    ///    sophisticated boolean expressions with AND, OR, XOR, and NOT operations, including nested
    ///    grouping and parentheses support
    /// </para>
    /// <para>
    /// The system processes four different entity archetypes through specialized jobs:
    /// - Simple AND without chance: Basic all-true evaluation
    /// - Simple AND with chance: Applies probabilistic evaluation when all conditions would be true
    /// - Complex logic without chance: Evaluates composite boolean expressions
    /// - Complex logic with chance: Applies probability to successful composite evaluations
    /// </para>
    /// <para>
    /// Probabilistic evaluation using <see cref="ConditionChance"/> only applies when conditions would
    /// otherwise be true, ensuring deterministic false results when conditions aren't met.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ConditionsSystemGroup))]
    public partial struct ConditionAllActiveSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Simple AND logic for entities without ConditionComposite
            new ConditionAllActiveJob().ScheduleParallel();
            new ConditionAllActiveWithChanceJob().ScheduleParallel();

            // Complex boolean logic for entities with ConditionComposite
            new ConditionCompositeJob().ScheduleParallel();
            new ConditionCompositeWithChanceJob().ScheduleParallel();
        }

        /// <summary>
        /// Evaluates a composite logic structure against the current active conditions.
        /// </summary>
        /// <param name="logic">The composite logic structure to evaluate.</param>
        /// <param name="activeConditions">Bitmask of currently active conditions.</param>
        /// <returns>True if the composite logic evaluates to true, false otherwise.</returns>
        private static bool EvaluateCompositeLogic(ref CompositeLogic logic, in BitArray32 activeConditions)
        {
            if (logic.Groups.Length == 0)
            {
                return false;
            }

            return logic.GroupCombination switch
            {
                LogicOperation.Or => EvaluateOrGroups(ref logic, activeConditions),
                LogicOperation.Xor => EvaluateXorGroups(ref logic, activeConditions),
                LogicOperation.Not => EvaluateNotGroups(ref logic, activeConditions),
                LogicOperation.And => EvaluateAndGroups(ref logic, activeConditions),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        /// <summary>
        /// Evaluates OR logic between groups - returns true if any group evaluates to true.
        /// </summary>
        /// <param name="logic">The composite logic containing the groups.</param>
        /// <param name="activeConditions">Bitmask of currently active conditions.</param>
        /// <returns>True if any group is true, false if all groups are false.</returns>
        private static bool EvaluateOrGroups(ref CompositeLogic logic, in BitArray32 activeConditions)
        {
            // OR: Return true if any group is true
            for (var i = 0; i < logic.Groups.Length; i++)
            {
                ref var group = ref logic.Groups[i];
                if (EvaluateLogicGroup(ref group, ref logic, activeConditions))
                {
                    return true; // Any group true = overall true
                }
            }

            return false; // No groups were true
        }

        /// <summary>
        /// Evaluates XOR logic between groups - returns true if an odd number of groups evaluate to true.
        /// </summary>
        /// <param name="logic">The composite logic containing the groups.</param>
        /// <param name="activeConditions">Bitmask of currently active conditions.</param>
        /// <returns>True if odd number of groups are true, false if even number (including zero).</returns>
        private static bool EvaluateXorGroups(ref CompositeLogic logic, in BitArray32 activeConditions)
        {
            // XOR: Return true if odd number of groups are true
            int trueCount = 0;
            for (var i = 0; i < logic.Groups.Length; i++)
            {
                ref var group = ref logic.Groups[i];
                if (EvaluateLogicGroup(ref group, ref logic, activeConditions))
                {
                    trueCount++;
                }
            }

            return trueCount % 2 == 1; // Odd number of true groups
        }

        /// <summary>
        /// Evaluates NOT logic between groups - returns true if all groups evaluate to false.
        /// Implements group-level negation like !(A & B) where the entire group result is inverted.
        /// </summary>
        /// <param name="logic">The composite logic containing the groups.</param>
        /// <param name="activeConditions">Bitmask of currently active conditions.</param>
        /// <returns>True if all groups are false, false if any group is true.</returns>
        private static bool EvaluateNotGroups(ref CompositeLogic logic, in BitArray32 activeConditions)
        {
            // NOT: Return true if all groups are false
            for (var i = 0; i < logic.Groups.Length; i++)
            {
                ref var group = ref logic.Groups[i];
                if (EvaluateLogicGroup(ref group, ref logic, activeConditions))
                {
                    return false; // Short-circuit NOT: any group true = overall false
                }
            }

            return true; // All groups were false
        }

        /// <summary>
        /// Evaluates AND logic between groups - returns true only if all groups evaluate to true.
        /// </summary>
        /// <param name="logic">The composite logic containing the groups.</param>
        /// <param name="activeConditions">Bitmask of currently active conditions.</param>
        /// <returns>True if all groups are true, false if any group is false.</returns>
        private static bool EvaluateAndGroups(ref CompositeLogic logic, in BitArray32 activeConditions)
        {
            // AND: Return true only if all groups are true
            for (var i = 0; i < logic.Groups.Length; i++)
            {
                ref var group = ref logic.Groups[i];
                if (!EvaluateLogicGroup(ref group, ref logic, activeConditions))
                {
                    return false; // Short-circuit AND: any group false = overall false
                }
            }

            return true; // All groups were true
        }

        /// <summary>
        /// Evaluates a single logic group, which may contain either condition masks or nested logic.
        /// </summary>
        /// <param name="group">The logic group to evaluate.</param>
        /// <param name="parentLogic">The parent composite logic (needed for nested logic access).</param>
        /// <param name="activeConditions">Bitmask of currently active conditions.</param>
        /// <returns>True if the group evaluates to true, false otherwise.</returns>
        private static bool EvaluateLogicGroup(ref LogicGroup group, ref CompositeLogic parentLogic, in BitArray32 activeConditions)
        {
            bool result;

            if (group.NestedLogicIndex >= 0)
            {
                // Evaluate nested logic recursively
                ref var nestedLogic = ref parentLogic.NestedLogics[group.NestedLogicIndex];
                result = EvaluateCompositeLogic(ref nestedLogic, activeConditions);

                // Apply NOT logic if specified - other logic types are handled by the nested logic's GroupCombination
                // This allows for constructs like BeginGroup(LogicOperation.Not).BeginGroup(LogicOperation.And)
                // which creates !(A & B) by negating the result of the nested AND group
                if (group.Logic == LogicOperation.Not)
                {
                    result = !result;
                }
            }
            else
            {
                // Evaluate condition mask
                result = group.Logic switch
                {
                    // AND condition: all specified conditions must be true
                    LogicOperation.And => (activeConditions & group.Mask).Equals(group.Mask),

                    // OR condition: at least one specified condition must be true
                    LogicOperation.Or => !(activeConditions & group.Mask).AllFalse,

                    // NOT condition: all specified conditions must be false
                    LogicOperation.Not => (activeConditions & group.Mask).AllFalse,

                    // XOR condition: exactly one specified condition must be true (odd number of true conditions)
                    LogicOperation.Xor => (activeConditions & group.Mask).CountBits() % 2 == 1,

                    _ => throw new ArgumentOutOfRangeException(),
                };
            }

            return result;
        }

        /// <summary>
        /// Job for simple AND logic - sets ConditionAllActive to true only if all conditions are active.
        /// Used for entities that don't have ConditionComposite component.
        /// </summary>
        [BurstCompile]
        [WithChangeFilter(typeof(ConditionActive))]
        [WithPresent(typeof(ConditionAllActive))]
        [WithOptions(EntityQueryOptions.FilterWriteGroup)]
        private partial struct ConditionAllActiveJob : IJobEntity
        {
            private static void Execute(EnabledRefRW<ConditionAllActive> conditionAllActive, in ConditionActive conditionActive)
            {
                conditionAllActive.ValueRW = conditionActive.Value.AllTrue;
            }
        }

        /// <summary>
        /// Job for simple AND logic with chance-based evaluation.
        /// Only applies chance when all conditions would be true, otherwise deterministically false.
        /// Used for entities that don't have ConditionComposite component but do have ConditionChance.
        /// </summary>
        [BurstCompile]
        [WithChangeFilter(typeof(ConditionActive))]
        [WithPresent(typeof(ConditionAllActive))]
        [WithOptions(EntityQueryOptions.FilterWriteGroup)]
        private partial struct ConditionAllActiveWithChanceJob : IJobEntity
        {
            private static void Execute(EnabledRefRW<ConditionAllActive> conditionAllActive, in ConditionActive conditionActive, in ConditionChance conditionChance)
            {
                var allTrue = conditionActive.Value.AllTrue;

                if (conditionAllActive.ValueRO == allTrue)
                {
                    return;
                }

                if (!allTrue)
                {
                    conditionAllActive.ValueRW = false;
                    return;
                }

                var r = GlobalRandom.NextUInt(ConditionChance.Multi);
                conditionAllActive.ValueRW = r < conditionChance.Value;
            }
        }

        /// <summary>
        /// Job for complex boolean logic evaluation using ConditionComposite.
        /// Supports AND, OR, XOR, NOT operations with nested grouping and parentheses.
        /// Used for entities that have ConditionComposite component but no ConditionChance.
        /// </summary>
        [BurstCompile]
        [WithPresent(typeof(ConditionAllActive))]
        [WithChangeFilter(typeof(ConditionActive))]
        [WithOptions(EntityQueryOptions.FilterWriteGroup)]
        private partial struct ConditionCompositeJob : IJobEntity
        {
            private static void Execute(
                EnabledRefRW<ConditionAllActive> conditionAllActive, in ConditionComposite conditionComposite, in ConditionActive conditionActive)
            {
                ref var logic = ref conditionComposite.Logic.Value;
                conditionAllActive.ValueRW = EvaluateCompositeLogic(ref logic, conditionActive.Value);
            }
        }

        /// <summary>
        /// Job for complex boolean logic evaluation with chance-based evaluation.
        /// Only applies chance when the composite logic would evaluate to true, otherwise deterministically false.
        /// Used for entities that have both ConditionComposite and ConditionChance components.
        /// </summary>
        [BurstCompile]
        [WithPresent(typeof(ConditionAllActive))]
        [WithChangeFilter(typeof(ConditionActive))]
        [WithOptions(EntityQueryOptions.FilterWriteGroup)]
        private partial struct ConditionCompositeWithChanceJob : IJobEntity
        {
            private static void Execute(EnabledRefRW<ConditionAllActive> conditionAllActive, in ConditionComposite conditionComposite,
                in ConditionActive conditionActive, in ConditionChance conditionChance)
            {
                var result = EvaluateCompositeLogic(ref conditionComposite.Logic.Value, conditionActive.Value);

                if (conditionAllActive.ValueRO == result)
                {
                    return;
                }

                if (!result)
                {
                    conditionAllActive.ValueRW = false;
                    return;
                }

                var r = GlobalRandom.NextUInt(ConditionChance.Multi);
                conditionAllActive.ValueRW = r < conditionChance.Value;
            }
        }
    }
}
