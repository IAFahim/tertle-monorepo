// <copyright file="ConditionCancelActiveSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Conditions
{
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Entities;

    /// <summary>
    /// Automatically cancels active duration-based reactions when their required conditions are no longer met.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveCancelSystemGroup"/> and provides condition-based interruption
    /// of active reactions. It processes entities that are currently active with duration timers but whose
    /// required conditions have become false.
    /// </para>
    /// <para>
    /// The system targets entities with the following characteristics:
    /// - Has <see cref="ActiveOnDuration"/> and <see cref="Active"/> components (duration-based active reaction)
    /// - Does not have <see cref="ConditionAllActive"/> enabled (conditions are not currently met)
    /// - Does not have <see cref="ActiveCancel"/> enabled (not already being cancelled)
    /// </para>
    /// <para>
    /// The cancellation logic compares the <see cref="ConditionCancelActive"/> requirements against the
    /// current <see cref="ConditionActive"/> state using bitwise operations. If any required condition
    /// is missing from the active conditions, the system enables <see cref="ActiveCancel"/> to trigger
    /// immediate cancellation by <see cref="ActiveCancelSystem"/>.
    /// </para>
    /// <para>
    /// This system ensures that reactions with duration timers don't continue running when their
    /// triggering conditions are no longer valid, providing responsive condition-based state management.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveCancelSystemGroup))]
    public partial struct ConditionCancelActiveSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ConditionInterruptJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithDisabled(typeof(ConditionAllActive), typeof(ActiveCancel))] // no need to check if all conditions are active or already cancelled
        [WithAll(typeof(ActiveOnDuration), typeof(Active))]
        private partial struct ConditionInterruptJob : IJobEntity
        {
            private static void Execute(EnabledRefRW<ActiveCancel> activeCancel, in ConditionCancelActive cancel, in ConditionActive active)
            {
                activeCancel.ValueRW = cancel.Value.BitAnd(active.Value) != cancel.Value;
            }
        }
    }
}