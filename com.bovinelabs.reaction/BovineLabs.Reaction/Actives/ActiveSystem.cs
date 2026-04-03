// <copyright file="ActiveSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable CS8602
namespace BovineLabs.Reaction.Actives
{
    using System;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Determines the active state of reaction entities based on combinations of conditions, duration, cooldown, and trigger components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveSystemGroup"/> and is the core system responsible for calculating whether
    /// a reaction should be active based on multiple input factors. It processes all entities with an <see cref="Active"/>
    /// component and evaluates their state.
    /// </para>
    /// <para>
    /// The system supports 16 different combinations of input components (2^4 combinations of Condition, Duration, Cooldown, Trigger).
    /// Each combination has specific behavior logic that determines when an entity should be active:
    /// </para>
    /// <list type="number">
    /// <item>None (0000): No controlling components - Always active (user-controlled)</item>
    /// <item>Duration (0001): Active while duration timer runs, resets when expired to allow restart</item>
    /// <item>Cooldown (0010): Active when NOT on cooldown (inverted cooldown state)</item>
    /// <item>Duration+Cooldown (0011): Active during duration OR when not on cooldown</item>
    /// <item>Condition (0100): Active when all conditions are met</item>
    /// <item>Condition+Duration (0101): Active during duration OR when conditions are met</item>
    /// <item>Condition+Cooldown (0110): Active when conditions are met AND not on cooldown</item>
    /// <item>Condition+Duration+Cooldown (0111): Active during duration OR (conditions met AND not on cooldown)</item>
    /// <item>Trigger (1000): Active only when triggered (single-frame activation)</item>
    /// <item>Trigger+Duration (1001): Active during duration OR when triggered</item>
    /// <item>Trigger+Cooldown (1010): Active when triggered AND not on cooldown</item>
    /// <item>Trigger+Duration+Cooldown (1011): Active during duration OR (triggered AND not on cooldown)</item>
    /// <item>Trigger+Condition (1100): Active when triggered AND conditions are met</item>
    /// <item>Trigger+Condition+Duration (1101): Active during duration OR (triggered AND conditions met)</item>
    /// <item>Trigger+Condition+Cooldown (1110): Active when triggered AND conditions met AND not on cooldown</item>
    /// <item>All Components (1111): Active during duration OR (triggered AND conditions met AND not on cooldown)</item>
    /// </list>
    /// <para>
    /// It directly manipulates enabled bits avoiding individual component lookups. Active triggers are automatically reset
    /// by <see cref="ActiveTriggerSystem"/> after processing to prevent continuous activation.
    /// </para>
    /// <para>
    /// Key Behavioral Notes:
    /// Duration acts as an override (OR operation) - entities remain active during duration regardless of other states.
    /// Cooldown acts as a blocker (AND NOT operation) - prevents activation when on cooldown.
    /// Conditions and Triggers require both to be true when combined (AND operation).
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveSystemGroup))]
    public partial struct ActiveSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithPresentRW<Active>().Build();

            state.Dependency = new IsActiveJob
                {
                    ActiveHandle = SystemAPI.GetComponentTypeHandle<Active>(),
                    ConditionAllActiveHandle = SystemAPI.GetComponentTypeHandle<ConditionAllActive>(true),
                    ActiveOnDurationHandle = SystemAPI.GetComponentTypeHandle<ActiveOnDuration>(true),
                    ActiveOnCooldownHandle = SystemAPI.GetComponentTypeHandle<ActiveOnCooldown>(true),
                    ActiveTriggerHandle = SystemAPI.GetComponentTypeHandle<ActiveTrigger>(true),
                }
                .ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private unsafe struct IsActiveJob : IJobChunk
        {
            public ComponentTypeHandle<Active> ActiveHandle;

            [ReadOnly]
            public ComponentTypeHandle<ConditionAllActive> ConditionAllActiveHandle;

            [ReadOnly]
            public ComponentTypeHandle<ActiveOnDuration> ActiveOnDurationHandle;

            [ReadOnly]
            public ComponentTypeHandle<ActiveOnCooldown> ActiveOnCooldownHandle;

            [ReadOnly]
            public ComponentTypeHandle<ActiveTrigger> ActiveTriggerHandle;

            [Flags]
            private enum Cases : byte
            {
                None,
                Duration = 1,
                Cooldown = 2,
                CooldownDuration = Cooldown | Duration,
                Condition = 4,
                ConditionDuration = Condition | Duration,
                ConditionCooldown = Condition | Cooldown,
                ConditionDurationCooldown = Condition | Cooldown | Duration,
                Trigger = 8,
                DurationTrigger = Duration | Trigger,
                CooldownTrigger = Cooldown | Trigger,
                CooldownDurationTrigger = CooldownDuration | Trigger,
                ConditionTrigger = Condition | Trigger,
                ConditionDurationTrigger = ConditionDuration | Trigger,
                ConditionCooldownTrigger = ConditionCooldown | Trigger,
                ConditionDurationCooldownTrigger = Condition | Cooldown | Duration | Trigger,
            }

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var conditionAllActives = chunk.GetEnabledBitsRO(ref this.ConditionAllActiveHandle);
                var activeOnDurations = chunk.GetEnabledBitsRO(ref this.ActiveOnDurationHandle);
                var activeOnCooldowns = chunk.GetEnabledBitsRO(ref this.ActiveOnCooldownHandle);
                var activeTriggers = chunk.GetEnabledBitsRO(ref this.ActiveTriggerHandle);

                var hasCondition = conditionAllActives != null ? Cases.Condition : 0;
                var hasCooldown = activeOnCooldowns != null ? Cases.Cooldown : 0;
                var hasDuration = activeOnDurations != null ? Cases.Duration : 0;
                var hasTrigger = activeTriggers != null ? Cases.Trigger : 0;

                switch (hasCondition | hasCooldown | hasDuration | hasTrigger)
                {
                    case Cases.None:
                    {
                        // Case 1 (0000): No controlling components - Always active (user-controlled)
                        // Logic: Always true - allows manual control via other systems
                        // Behavior: Entity remains permanently active unless manually disabled
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = ulong.MaxValue & mask0;
                        actives.ULong1 = ulong.MaxValue & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.Duration:
                    {
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);

                        // Case 2 (0001): Duration only - Active while duration timer runs
                        // Logic: Active = Duration OR (NOT Active) - resets when duration expires
                        // Behavior: Active during duration, auto-resets when expired to allow restart
                        // Special case: Uses (~actives | duration) to reset inactive entities for restart
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = (~actives.ULong0 | activeOnDurations->ULong0) & mask0;
                        actives.ULong1 = (~actives.ULong1 | activeOnDurations->ULong1) & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.Cooldown:
                    {
                        // Case 3 (0010): Cooldown only - Active when NOT on cooldown
                        // Logic: Active = NOT Cooldown (inverted cooldown state)
                        // Behavior: Active by default, blocked while on cooldown
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = ~activeOnCooldowns->ULong0 & mask0;
                        actives.ULong1 = ~activeOnCooldowns->ULong1 & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.CooldownDuration:
                    {
                        // Case 4 (0011): Duration + Cooldown - Active during duration OR when not on cooldown
                        // Logic: Active = Duration OR (NOT Cooldown)
                        // Behavior: Duration overrides cooldown; active by default when not on cooldown
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = (activeOnDurations->ULong0 | ~activeOnCooldowns->ULong0) & mask0;
                        actives.ULong1 = (activeOnDurations->ULong1 | ~activeOnCooldowns->ULong1) & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.Condition:
                    {
                        // Case 5 (0100): Condition only - Active when all conditions are met
                        // Logic: Active = Conditions
                        // Behavior: Directly mirrors condition state; reactive to condition changes
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = conditionAllActives->ULong0 & mask0;
                        actives.ULong1 = conditionAllActives->ULong1 & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.ConditionDuration:
                    {
                        // Case 6 (0101): Condition + Duration - Active during duration OR when conditions are met
                        // Logic: Active = Duration OR Conditions
                        // Behavior: Duration overrides conditions; conditions can activate independently
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = (activeOnDurations->ULong0 | conditionAllActives->ULong0) & mask0;
                        actives.ULong1 = (activeOnDurations->ULong1 | conditionAllActives->ULong1) & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.ConditionCooldown:
                    {
                        // Case 7 (0110): Condition + Cooldown - Active when conditions are met AND not on cooldown
                        // Logic: Active = Conditions AND (NOT Cooldown)
                        // Behavior: Conditions required for activation; cooldown blocks activation
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = conditionAllActives->ULong0 & ~activeOnCooldowns->ULong0 & mask0;
                        actives.ULong1 = conditionAllActives->ULong1 & ~activeOnCooldowns->ULong1 & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.ConditionDurationCooldown:
                    {
                        // Case 8 (0111): Condition + Duration + Cooldown - Active during duration OR (conditions met AND not on cooldown)
                        // Logic: Active = Duration OR (Conditions AND (NOT Cooldown))
                        // Behavior: Duration always activates; conditions activate only when not on cooldown
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = (activeOnDurations->ULong0 | conditionAllActives->ULong0 & ~activeOnCooldowns->ULong0) & mask0;
                        actives.ULong1 = (activeOnDurations->ULong1 | conditionAllActives->ULong1 & ~activeOnCooldowns->ULong1) & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.Trigger:
                    {
                        // Case 9 (1000): Trigger only - Active only when triggered
                        // Logic: Active = Trigger (single-frame activation)
                        // Behavior: Active only on trigger frame; ActiveTriggerSystem resets triggers afterward
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = activeTriggers->ULong0 & mask0;
                        actives.ULong1 = activeTriggers->ULong1 & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.DurationTrigger:
                    {
                        // Case 10 (1001): Trigger + Duration - Active during duration OR when triggered
                        // Logic: Active = Duration OR Trigger
                        // Behavior: Triggers can start duration; remains active during duration
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = (activeOnDurations->ULong0 | activeTriggers->ULong0) & mask0;
                        actives.ULong1 = (activeOnDurations->ULong1 | activeTriggers->ULong1) & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.CooldownTrigger:
                    {
                        // Case 11 (1010): Trigger + Cooldown - Active when triggered AND not on cooldown
                        // Logic: Active = Trigger AND (NOT Cooldown)
                        // Behavior: Trigger activation blocked by cooldown; requires both conditions
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = ~activeOnCooldowns->ULong0 & activeTriggers->ULong0 & mask0;
                        actives.ULong1 = ~activeOnCooldowns->ULong1 & activeTriggers->ULong1 & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.CooldownDurationTrigger:
                    {
                        // Case 12 (1011): Trigger + Duration + Cooldown - Active during duration OR (triggered AND not on cooldown)
                        // Logic: Active = Duration OR (Trigger AND (NOT Cooldown))
                        // Behavior: Duration always activates; triggers blocked by cooldown but duration overrides
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = (activeOnDurations->ULong0 | activeTriggers->ULong0 & ~activeOnCooldowns->ULong0) & mask0;
                        actives.ULong1 = (activeOnDurations->ULong1 | activeTriggers->ULong1 & ~activeOnCooldowns->ULong1) & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.ConditionTrigger:
                    {
                        // Case 13 (1100): Trigger + Condition - Active when triggered AND conditions are met
                        // Logic: Active = Trigger AND Conditions
                        // Behavior: Requires both trigger activation and satisfied conditions
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = conditionAllActives->ULong0 & activeTriggers->ULong0 & mask0;
                        actives.ULong1 = conditionAllActives->ULong1 & activeTriggers->ULong1 & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.ConditionDurationTrigger:
                    {
                        // Case 14 (1101): Trigger + Condition + Duration - Active during duration OR (triggered AND conditions met)
                        // Logic: Active = Duration OR (Trigger AND Conditions)
                        // Behavior: Duration always activates; triggers require conditions to be satisfied
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = (activeOnDurations->ULong0 | activeTriggers->ULong0 & conditionAllActives->ULong0) & mask0;
                        actives.ULong1 = (activeOnDurations->ULong1 | activeTriggers->ULong1 & conditionAllActives->ULong1) & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.ConditionCooldownTrigger:
                    {
                        // Case 15 (1110): Trigger + Condition + Cooldown - Active when triggered AND conditions met AND not on cooldown
                        // Logic: Active = Trigger AND Conditions AND (NOT Cooldown)
                        // Behavior: Requires all three conditions: trigger, satisfied conditions, and not on cooldown
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = activeTriggers->ULong0 & conditionAllActives->ULong0 & ~activeOnCooldowns->ULong0 & mask0;
                        actives.ULong1 = activeTriggers->ULong1 & conditionAllActives->ULong1 & ~activeOnCooldowns->ULong1 & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }

                    case Cases.ConditionDurationCooldownTrigger:
                    {
                        // Case 16 (1111): All Components - Active during duration OR (triggered AND conditions met AND not on cooldown)
                        // Logic: Active = Duration OR (Trigger AND Conditions AND (NOT Cooldown))
                        // Behavior: Duration overrides all; triggers require conditions and must not be on cooldown
                        ref var actives = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveHandle, out var ptrChunkDisabledCount);
                        chunk.GetEnableableActiveMasks(out var mask0, out var mask1);
                        actives.ULong0 = (activeOnDurations->ULong0 | activeTriggers->ULong0 & conditionAllActives->ULong0 & ~activeOnCooldowns->ULong0) & mask0;
                        actives.ULong1 = (activeOnDurations->ULong1 | activeTriggers->ULong1 & conditionAllActives->ULong1 & ~activeOnCooldowns->ULong1) & mask1;
                        chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, actives);
                        break;
                    }
                }
            }
        }
    }
}
