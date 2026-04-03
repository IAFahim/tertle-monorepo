// <copyright file="ActiveDurationSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actives
{
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Manages duration initialization and ticking for active reactions with simplified custom implementation optimized for performance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system handles time-limited reactions by starting duration timers when entities transition to active state
    /// and managing the countdown until expiration. The implementation has been rewritten from the previous generic
    /// timer abstraction to provide direct, optimized job scheduling.
    /// </para>
    /// <para>
    /// Key responsibilities are split across two queries:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="startDurationQuery"/>: Initializes <see cref="ActiveOnDuration"/> and seeds <see cref="ActiveDurationRemaining"/> for newly active reactions.</description></item>
    /// <item><description><see cref="durationTimerQuery"/>: Ticks down existing duration timers and disables <see cref="ActiveOnDuration"/> when time expires.</description></item>
    /// </list>
    /// <para>
    /// This system runs in <see cref="TimerSystemGroup"/> before <see cref="ActiveCooldownSystem"/> to ensure
    /// duration state is current when processing after-duration cooldowns.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(TimerSystemGroup))]
    public partial struct ActiveDurationSystem : ISystem
    {
        private EntityQuery startDurationQuery;
        private EntityQuery durationTimerQuery;

        /// <summary>
        /// Creates an entity query builder for entities that need their duration state initialized.
        /// </summary>
        /// <returns>
        /// A query builder configured for active reactions that have just transitioned into the active state and
        /// require their duration flags and timers to be set.
        /// </returns>
        /// <remarks>
        /// This builder captures freshly activated entities (no <see cref="ActivePrevious"/> component enabled) and
        /// ensures the <see cref="StartDurationJob"/> can safely write to both <see cref="ActiveOnDuration"/> and
        /// <see cref="ActiveDurationRemaining"/> while respecting write-group constraints.
        /// </remarks>
        public static EntityQueryBuilder GetBuilderForStartDuration()
        {
            return new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<ActiveDurationRemaining>()
                .WithPresentRW<ActiveOnDuration>()
                .WithAll<ActiveDuration>()
                .WithAll<Active>()
                .WithDisabled<ActivePrevious>()
                .WithOptions(EntityQueryOptions.FilterWriteGroup);
        }

        /// <summary>
        /// Creates an entity query builder for entities that are currently ticking down their duration.
        /// </summary>
        /// <returns>
        /// A query builder configured for reactions that have duration state enabled and need their remaining time reduced each frame.
        /// </returns>
        public static EntityQueryBuilder GetBuilderForDurationTimer()
        {
            return new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<ActiveOnDuration>()
                .WithAllRW<ActiveDurationRemaining>()
                .WithOptions(EntityQueryOptions.FilterWriteGroup);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.startDurationQuery = GetBuilderForStartDuration().Build(ref state);
            this.durationTimerQuery = GetBuilderForDurationTimer().Build(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new StartDurationJob().ScheduleParallel(this.startDurationQuery);
            new DurationTimerJob { DeltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(this.durationTimerQuery);
        }

        /// <summary>
        /// Enables the duration flag and initializes the remaining duration for newly active reactions.
        /// </summary>
        [BurstCompile]
        public partial struct StartDurationJob : IJobEntity
        {
            private static void Execute(EnabledRefRW<ActiveOnDuration> on, ref ActiveDurationRemaining remaining, in ActiveDuration duration)
            {
                on.ValueRW = true;
                remaining.Value = math.max(remaining.Value, duration.Value);
            }
        }

        /// <summary>
        /// Ticks down duration timers and clears the duration flag once the timer expires.
        /// </summary>
        [BurstCompile]
        public partial struct DurationTimerJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(EnabledRefRW<ActiveOnDuration> activeOnDuration, ref ActiveDurationRemaining remaining)
            {
                remaining.Value -= this.DeltaTime;
                if (remaining.Value <= 0)
                {
                    remaining.Value = 0;
                    activeOnDuration.ValueRW = false;
                }
            }
        }
    }
}
