// <copyright file="ActiveCancelSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actives
{
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Entities;

    /// <summary>
    /// Handles the immediate cancellation of active reactions by resetting their duration timers to zero.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveSystemGroup"/> and updates before <see cref="ActiveDurationSystem"/>
    /// to ensure cancellation takes effect immediately. It processes entities with the <see cref="ActiveCancel"/>
    /// component enabled.
    /// </para>
    /// <para>
    /// The system performs the following operations for each entity to be cancelled:
    /// 1. Disables the <see cref="ActiveCancel"/> component to prevent repeated processing
    /// 2. Sets the <see cref="ActiveDurationRemaining"/> value to zero, effectively ending the active duration
    /// </para>
    /// <para>
    /// This allows other systems or user code to request immediate cancellation of active reactions by simply
    /// enabling the <see cref="ActiveCancel"/> component. The cancellation takes effect in the same frame.
    /// </para>
    /// </remarks>
    [UpdateAfter(typeof(ActiveDurationSystem))]
    [UpdateBefore(typeof(ActiveCooldownSystem))]
    [UpdateInGroup(typeof(TimerSystemGroup))]
    public partial struct ActiveCancelSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ActiveCancelJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithPresent(typeof(ActiveOnDuration))]
        private partial struct ActiveCancelJob : IJobEntity
        {
            private void Execute(EnabledRefRW<ActiveOnDuration> on, EnabledRefRW<ActiveCancel> cancel, ref ActiveDurationRemaining durationRemaining)
            {
                cancel.ValueRW = false;
                on.ValueRW = false;
                durationRemaining.Value = 0;
            }
        }
    }
}
