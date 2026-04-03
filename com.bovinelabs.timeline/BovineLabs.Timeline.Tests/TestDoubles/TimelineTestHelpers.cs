// <copyright file="TimelineTestHelpers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.TestDoubles
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Entities;
    using Unity.IntegerTime;

    public static class TimelineTestHelpers
    {
        public static readonly DiscreteTime Time0 = DiscreteTime.FromTicks(0);
        public static readonly DiscreteTime Time1 = DiscreteTime.FromTicks(1);
        public static readonly DiscreteTime Time2 = DiscreteTime.FromTicks(2);
        public static readonly DiscreteTime Time5 = DiscreteTime.FromTicks(5);
        public static readonly DiscreteTime Time10 = DiscreteTime.FromTicks(10);
        public static readonly DiscreteTime Time20 = DiscreteTime.FromTicks(20);

        public static DiscreteTime Ticks(long ticks)
        {
            return DiscreteTime.FromTicks(ticks);
        }

        public static Entity CreateClockEntity(EntityManager manager, in ClockSettings clockSettings, in ClockData clockData, bool timelineActiveEnabled = true)
        {
            var archetype = manager.CreateArchetype(
                typeof(ClockSettings),
                typeof(ClockData),
                typeof(TimelineActive));

            var entity = manager.CreateEntity(archetype);
            manager.SetComponentData(entity, clockSettings);
            manager.SetComponentData(entity, clockData);
            manager.SetComponentEnabled<TimelineActive>(entity, timelineActiveEnabled);

            return entity;
        }

        public static Entity CreateTimerEntity(
            EntityManager manager, in Timer timer, in TimerRange timerRange, in ClockSettings clockSettings, in ClockData clockData,
            bool timelineActiveEnabled, bool timelineActivePreviousEnabled, bool timerPausedEnabled = false)
        {
            var archetype = manager.CreateArchetype(
                typeof(Timer),
                typeof(TimerRange),
                typeof(ClockSettings),
                typeof(ClockData),
                typeof(TimelineActive),
                typeof(TimelineActivePrevious),
                typeof(TimerPaused),
                typeof(TimerDataLink),
                typeof(CompositeTimerLink));

            var entity = manager.CreateEntity(archetype);
            manager.SetComponentData(entity, timer);
            manager.SetComponentData(entity, timerRange);
            manager.SetComponentData(entity, clockSettings);
            manager.SetComponentData(entity, clockData);
            manager.SetComponentEnabled<TimelineActive>(entity, timelineActiveEnabled);
            manager.SetComponentEnabled<TimelineActivePrevious>(entity, timelineActivePreviousEnabled);
            manager.SetComponentEnabled<TimerPaused>(entity, timerPausedEnabled);

            return entity;
        }

        public static Entity CreateTimerDataTarget(EntityManager manager, in TimerData timerData, bool timelineActiveEnabled = false)
        {
            var archetype = manager.CreateArchetype(
                typeof(TimerData),
                typeof(TimelineActive));

            var entity = manager.CreateEntity(archetype);
            manager.SetComponentData(entity, timerData);
            manager.SetComponentEnabled<TimelineActive>(entity, timelineActiveEnabled);

            return entity;
        }

        public static Entity CreateCompositeTimerEntity(
            EntityManager manager, in CompositeTimer compositeTimer, in Timer timer, bool timelineActiveEnabled, bool addTimerDataLinkBuffer = true)
        {
            var archetype = addTimerDataLinkBuffer
                ? manager.CreateArchetype(typeof(CompositeTimer), typeof(Timer), typeof(TimelineActive), typeof(TimerDataLink), typeof(CompositeTimerLink))
                : manager.CreateArchetype(typeof(CompositeTimer), typeof(Timer), typeof(TimelineActive), typeof(CompositeTimerLink));
            var entity = manager.CreateEntity(archetype);
            manager.SetComponentData(entity, compositeTimer);
            manager.SetComponentData(entity, timer);
            manager.SetComponentEnabled<TimelineActive>(entity, timelineActiveEnabled);
            return entity;
        }

        public static Entity CreateClipEntity(
            EntityManager manager, in LocalTime localTime, in TimerData timerData, in TimeTransform timeTransform,
            bool timelineActiveEnabled, bool clipActiveEnabled)
        {
            var archetype = manager.CreateArchetype(
                typeof(LocalTime),
                typeof(TimerData),
                typeof(TimeTransform),
                typeof(TimelineActive),
                typeof(ClipActive));

            var entity = manager.CreateEntity(archetype);
            manager.SetComponentData(entity, localTime);
            manager.SetComponentData(entity, timerData);
            manager.SetComponentData(entity, timeTransform);
            manager.SetComponentEnabled<TimelineActive>(entity, timelineActiveEnabled);
            manager.SetComponentEnabled<ClipActive>(entity, clipActiveEnabled);
            return entity;
        }

        public static void AddTimerDataLink(EntityManager manager, Entity timerEntity, Entity linkedTimerDataEntity)
        {
            var links = manager.GetBuffer<TimerDataLink>(timerEntity);
            links.Add(new TimerDataLink { Value = linkedTimerDataEntity });
        }

        public static void AddCompositeTimerLink(EntityManager manager, Entity timerEntity, Entity compositeTimerEntity)
        {
            var links = manager.GetBuffer<CompositeTimerLink>(timerEntity);
            links.Add(new CompositeTimerLink { Value = compositeTimerEntity });
        }

        public static void SetTimelineActive(EntityManager manager, Entity entity, bool enabled)
        {
            manager.SetComponentEnabled<TimelineActive>(entity, enabled);
        }

        public static void SetTimelineActivePrevious(EntityManager manager, Entity entity, bool enabled)
        {
            manager.SetComponentEnabled<TimelineActivePrevious>(entity, enabled);
        }

        public static void SetClipActive(EntityManager manager, Entity entity, bool enabled)
        {
            manager.SetComponentEnabled<ClipActive>(entity, enabled);
        }

        public static void SetTimerPaused(EntityManager manager, Entity entity, bool enabled)
        {
            manager.SetComponentEnabled<TimerPaused>(entity, enabled);
        }
    }
}
