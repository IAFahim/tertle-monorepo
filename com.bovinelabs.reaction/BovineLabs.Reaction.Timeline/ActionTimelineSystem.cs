// <copyright file="ActionTimelineSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Timeline
{
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Groups;
    using BovineLabs.Reaction.Timeline.Data;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.IntegerTime;

    /// <summary>
    /// Manages timeline activation and entity binding for reaction-driven timeline playback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveEnabledSystemGroup"/> and handles the activation and deactivation
    /// of timeline entities when reactions change state. It processes entities with <see cref="ActionTimeline"/>
    /// components and manages their timeline playback lifecycle.
    /// </para>
    /// <para>
    /// The system performs two primary operations:
    /// 1. **Timeline Activation**: When reactions become active (Active=true, ActivePrevious=false):
    ///    - Enables the <see cref="TimelineActive"/> component to start timeline playback
    ///    - Sets the initial timer value based on <see cref="ActionTimeline.InitialTime"/>
    ///    - Resolves target entities and binds them to timeline tracks
    ///    - Updates <see cref="TrackBinding"/> components to connect tracks with their target entities
    /// 2. **Timeline Deactivation**: When reactions become inactive (Active=false, ActivePrevious=true):
    ///    - Conditionally disables <see cref="TimelineActive"/> based on <see cref="ActionTimeline.DisableTimelineOnDeactivate"/> setting
    /// </para>
    /// <para>
    /// The binding process uses the following workflow:
    /// 1. Iterates through <see cref="ActionTimelineBinding"/> entries to find required target bindings
    /// 2. Resolves target entities using the <see cref="Targets"/> system for each binding
    /// 3. Matches track identifiers between <see cref="DirectorBinding"/> and <see cref="ActionTimelineBinding"/>
    /// 4. Updates the corresponding <see cref="TrackBinding.Value"/> with the resolved target entity
    /// </para>
    /// <para>
    /// This system enables reaction-driven timeline animation where timelines can be triggered by
    /// game events and automatically bind to relevant entities (such as animation targets, effect spawns, etc.).
    /// The flexible binding system allows timelines to operate on different target entities each time they're activated.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveEnabledSystemGroup))]
    public partial struct ActionTimelineSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ActivatedJob
                {
                    TrackBindings = SystemAPI.GetComponentLookup<TrackBinding>(),
                    TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                    Timers = SystemAPI.GetComponentLookup<Timer>(),
                    TimelineActives = SystemAPI.GetComponentLookup<TimelineActive>(),
                    DirectorBindings = SystemAPI.GetBufferLookup<DirectorBinding>(true),
                }
                .ScheduleParallel();

            new DeactivatedJob
            {
                TimelineActives = SystemAPI.GetComponentLookup<TimelineActive>(),
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(Active))]
        [WithDisabled(typeof(ActivePrevious))]
        private partial struct ActivatedJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<TrackBinding> TrackBindings;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<Timer> Timers;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<TimelineActive> TimelineActives;

            [ReadOnly]
            public BufferLookup<DirectorBinding> DirectorBindings;

            private void Execute(
                Entity entity, in Targets targets, in DynamicBuffer<ActionTimeline> actionTimelines, in DynamicBuffer<ActionTimelineBinding> actionTimelineBindings)
            {
                for (var index = 0; index < actionTimelines.Length; index++)
                {
                    var actionTimeline = actionTimelines[index];

                    var timelineActive = this.TimelineActives.GetEnabledRefRW<TimelineActive>(actionTimeline.Director);

                    if (!actionTimeline.ResetWhenActive)
                    {
                        if (timelineActive.ValueRO)
                        {
                            continue;
                        }
                    }

                    ref var timer = ref this.Timers.GetRefRW(actionTimeline.Director).ValueRW;
                    var directorBindings = this.DirectorBindings[actionTimeline.Director];

                    timelineActive.ValueRW = true;
                    timer.Time = new DiscreteTime(actionTimeline.InitialTime);

                    foreach (var bindingTarget in actionTimelineBindings)
                    {
                        if (bindingTarget.Index != index)
                        {
                            continue;
                        }

                        var target = targets.Get(bindingTarget.Target, entity, this.TargetsCustoms);
                        if (target == Entity.Null)
                        {
                            continue;
                        }

                        foreach (var binding in directorBindings)
                        {
                            if (binding.TrackIdentifier != bindingTarget.TrackIdentifier)
                            {
                                continue;
                            }

                            ref var trackBinding = ref this.TrackBindings.GetRefRW(binding.TrackEntity).ValueRW;
                            trackBinding.Value = target;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(ActivePrevious))]
        [WithDisabled(typeof(Active))]
        private partial struct DeactivatedJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<TimelineActive> TimelineActives;

            private void Execute(in DynamicBuffer<ActionTimeline> actionTimelines)
            {
                foreach (var actionTimeline in actionTimelines)
                {
                    if (actionTimeline.DisableTimelineOnDeactivate)
                    {
                        this.TimelineActives.SetComponentEnabled(actionTimeline.Director, false);
                    }
                }
            }
        }
    }
}
