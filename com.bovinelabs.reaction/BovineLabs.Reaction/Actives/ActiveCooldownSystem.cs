// <copyright file="ActiveCooldownSystem.cs" company="BovineLabs">
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
    /// Manages cooldown initialization and ticking for active reactions with dual-path architecture supporting both immediate and delayed cooldown timing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system implements dual timing paths to handle two different cooldown behaviors:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Immediate Cooldown</b>: Starts when the reaction activates (entities without <see cref="ActiveCooldownAfterDuration"/>)</description></item>
    /// <item><description><b>Delayed Cooldown</b>: Starts only after duration expires (entities with <see cref="ActiveCooldownAfterDuration"/>)</description></item>
    /// </list>
    /// <para>
    /// Cooldown management is split across the following queries:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="startCooldownQuery"/>: Initializes cooldown state when a reaction first activates.</description></item>
    /// <item><description><see cref="onActivateQuery"/>: Ticks cooldowns that begin immediately on activation.</description></item>
    /// <item><description><see cref="afterDurationQuery"/>: Ticks cooldowns that wait for duration expiry before starting.</description></item>
    /// </list>
    /// <para>
    /// The cooldown timer jobs share the same countdown logic but use different activation triggers.
    /// The <see cref="StartCooldownJob"/> uses <c>math.max()</c> to handle retriggering while already on cooldown.
    /// </para>
    /// <para>
    /// This system runs in <see cref="TimerSystemGroup"/> after <see cref="ActiveDurationSystem"/> to ensure
    /// duration state is current when processing after-duration cooldowns.
    /// </para>
    /// </remarks>
    [UpdateAfter(typeof(ActiveDurationSystem))]
    [UpdateInGroup(typeof(TimerSystemGroup))]
    public partial struct ActiveCooldownSystem : ISystem
    {
        private EntityQuery startCooldownQuery;
        private EntityQuery onActivateQuery;
        private EntityQuery afterDurationQuery;

        /// <summary>
        /// Creates an entity query builder for entities that need their cooldown state reinitialized.
        /// </summary>
        /// <returns>
        /// A query builder configured for active reactions that have just transitioned into the active state and
        /// require their cooldown flags and timers to be set.
        /// </returns>
        /// <remarks>
        /// This builder captures freshly activated entities (no <see cref="ActivePrevious"/> component enabled) and
        /// ensures the <see cref="StartCooldownJob"/> can safely write to both <see cref="ActiveOnCooldown"/> and
        /// <see cref="ActiveCooldownRemaining"/> while respecting write-group constraints.
        /// </remarks>
        public static EntityQueryBuilder GetBuilderForStartCooldown()
        {
            return new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<ActiveCooldownRemaining>()
                .WithPresentRW<ActiveOnCooldown>()
                .WithAll<ActiveCooldown>()
                .WithAll<Active>()
                .WithDisabled<ActivePrevious>()
                .WithOptions(EntityQueryOptions.FilterWriteGroup);
        }

        /// <summary>
        /// Creates an entity query builder for entities that start cooldown immediately when activated.
        /// </summary>
        /// <returns>
        /// A query builder configured for entities without <see cref="ActiveCooldownAfterDuration"/>
        /// that should start their cooldown timer as soon as they become active.
        /// </returns>
        /// <remarks>
        /// This query targets the "immediate cooldown" behavior where cooldown begins when the reaction activates,
        /// regardless of whether the reaction also has a duration. This is the traditional cooldown behavior.
        /// </remarks>
        public static EntityQueryBuilder GetBuilderForCooldownOnActivate()
        {
            return Shared().WithNone<ActiveCooldownAfterDuration>().WithOptions(EntityQueryOptions.FilterWriteGroup);
        }

        /// <summary>
        /// Creates an entity query builder for entities that start cooldown only after their duration expires.
        /// </summary>
        /// <returns>
        /// A query builder configured for entities with <see cref="ActiveCooldownAfterDuration"/>
        /// that have a disabled <see cref="ActiveOnDuration"/> component, indicating their duration has ended.
        /// </returns>
        /// <remarks>
        /// This query targets the "delayed cooldown" behavior where cooldown begins only when the duration timer expires.
        /// The query specifically looks for entities with disabled <see cref="ActiveOnDuration"/> to detect when
        /// the duration has finished and cooldown should begin.
        /// </remarks>
        public static EntityQueryBuilder GetBuilderForCooldownAfterDuration()
        {
            return Shared().WithAll<ActiveCooldownAfterDuration>().WithDisabled<ActiveOnDuration>().WithOptions(EntityQueryOptions.FilterWriteGroup);
        }

        private static EntityQueryBuilder Shared()
        {
            return new EntityQueryBuilder(Allocator.Temp).WithAllRW<ActiveOnCooldown>().WithAllRW<ActiveCooldownRemaining>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.startCooldownQuery = GetBuilderForStartCooldown().Build(ref state);
            this.onActivateQuery = GetBuilderForCooldownOnActivate().Build(ref state);
            this.afterDurationQuery = GetBuilderForCooldownAfterDuration().Build(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new StartCooldownJob().ScheduleParallel(this.startCooldownQuery);

            new CooldownTimerJob { DeltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(this.onActivateQuery);
            new CooldownTimerJob { DeltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(this.afterDurationQuery);
        }

        /// <summary>
        /// Enables the cooldown flag and initializes the remaining cooldown time for newly active reactions.
        /// </summary>
        [BurstCompile]
        public partial struct StartCooldownJob : IJobEntity
        {
            private static void Execute(EnabledRefRW<ActiveOnCooldown> on, ref ActiveCooldownRemaining remaining, in ActiveCooldown duration)
            {
                on.ValueRW = true;
                remaining.Value = math.max(remaining.Value, duration.Value);
            }
        }

        /// <summary>
        /// Ticks down cooldown timers and clears the cooldown flag once the timer expires.
        /// </summary>
        [BurstCompile]
        public partial struct CooldownTimerJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(EnabledRefRW<ActiveOnCooldown> activeOnCooldown, ref ActiveCooldownRemaining remaining)
            {
                remaining.Value -= this.DeltaTime;
                if (remaining.Value <= 0)
                {
                    remaining.Value = 0;
                    activeOnCooldown.ValueRW = false;
                }
            }
        }
    }
}