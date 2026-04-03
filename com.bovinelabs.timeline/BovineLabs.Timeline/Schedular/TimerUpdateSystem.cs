// <copyright file="TimerUpdateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Schedular
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.IntegerTime;

    /// <summary>
    /// System that updates all timers based on their clock data.
    /// Handles timer initialization when timelines become active, updates running timers,
    /// and manages timer pausing and stopping. Also updates composite timers that are
    /// derived from parent timers with custom scaling and offsets.
    /// </summary>
    [UpdateInGroup(typeof(ScheduleSystemGroup))]
    public partial struct TimerUpdateSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new TimerStartedJob
            {
                TimerDatas = SystemAPI.GetComponentLookup<TimerData>(),
                Actives = SystemAPI.GetComponentLookup<TimelineActive>(),
                TimerDataLinks = SystemAPI.GetBufferLookup<TimerDataLink>(true),
                CompositeTimerLinks = SystemAPI.GetBufferLookup<CompositeTimerLink>(true),
                CompositeTimers = SystemAPI.GetComponentLookup<CompositeTimer>(true),
                Timers = SystemAPI.GetComponentLookup<Timer>(),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new TimersUpdateJob
            {
                TimerDatas = SystemAPI.GetComponentLookup<TimerData>(),
                Actives = SystemAPI.GetComponentLookup<TimelineActive>(),
                TimerPauseds = SystemAPI.GetComponentLookup<TimerPaused>(),
                TimerDataLinks = SystemAPI.GetBufferLookup<TimerDataLink>(true),
                CompositeTimerLinks = SystemAPI.GetBufferLookup<CompositeTimerLink>(true),
                CompositeTimers = SystemAPI.GetComponentLookup<CompositeTimer>(true),
                Timers = SystemAPI.GetComponentLookup<Timer>(),
            }.ScheduleParallel(state.Dependency);

            var stoppedQuery = SystemAPI
                .QueryBuilder()
                .WithAll<TimelineActivePrevious, TimerDataLink>()
                .WithDisabled<TimelineActive>()
                .WithPresent<TimerPaused>()
                .Build();

            state.Dependency = new TimerStoppedJob
            {
                TimerDataLinks = SystemAPI.GetBufferTypeHandle<TimerDataLink>(true),
                TimerPausedHandle = SystemAPI.GetComponentTypeHandle<TimerPaused>(),
                Actives = SystemAPI.GetComponentLookup<TimelineActive>(),
            }.ScheduleParallel(stoppedQuery, state.Dependency);
        }

        private static void UpdateCompositeTimers(
            Entity entity, in TimerData source, in DynamicBuffer<TimerDataLink> timerDataLinks, ComponentLookup<TimerData> timerDatas,
            BufferLookup<TimerDataLink> timerDataLinksLookup, BufferLookup<CompositeTimerLink> compositeTimerLinks,
            ComponentLookup<CompositeTimer> compositeTimers, ComponentLookup<Timer> timers, ComponentLookup<TimelineActive> actives)
        {
            foreach (var link in timerDataLinks.AsNativeArray())
            {
                timerDatas[link.Value] = source;
            }

            if (!compositeTimerLinks.TryGetBuffer(entity, out var compositeLinks))
            {
                return;
            }

            foreach (var compLink in compositeLinks.AsNativeArray())
            {
                var composite = compositeTimers[compLink.Value];
                var newLinks = timerDataLinksLookup[compLink.Value];
                ref var timer = ref timers.GetRefRW(compLink.Value).ValueRW;

                timer.Time = (source.Time * composite.Scale) + composite.Offset;
                timer.DeltaTime = source.DeltaTime * composite.Scale;
                timer.TimeScale = source.TimeScale * composite.Scale;

                var active = source.Time >= composite.ActiveRange.Start && source.Time < composite.ActiveRange.End;
                var activeRW = actives.GetEnabledRefRW<TimelineActive>(compLink.Value);
                if (active != activeRW.ValueRO)
                {
                    activeRW.ValueRW = active;

                    if (active)
                    {
                        foreach (var link in newLinks.AsNativeArray())
                        {
                            actives.SetComponentEnabled(link.Value, true);
                        }
                    }
                    else
                    {
                        foreach (var link in newLinks.AsNativeArray())
                        {
                            actives.SetComponentEnabled(link.Value, false);
                        }
                    }
                }

                var newSource = new TimerData
                {
                    DeltaTime = timer.DeltaTime,
                    TimeScale = timer.TimeScale,
                    Time = timer.Time,
                };

                UpdateCompositeTimers(compLink.Value, newSource, newLinks, timerDatas, timerDataLinksLookup, compositeTimerLinks, compositeTimers, timers,
                    actives);
            }
        }

        [WithAll(typeof(TimelineActive))]
        [WithDisabled(typeof(TimelineActivePrevious))]
        [BurstCompile]
        private partial struct TimerStartedJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<TimerData> TimerDatas;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<TimelineActive> Actives;

            [ReadOnly]
            [NativeDisableContainerSafetyRestriction]
            public BufferLookup<TimerDataLink> TimerDataLinks;

            [ReadOnly]
            public BufferLookup<CompositeTimerLink> CompositeTimerLinks;

            [ReadOnly]
            public ComponentLookup<CompositeTimer> CompositeTimers;

            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<Timer> Timers;

            private void Execute(
                Entity entity, ref Timer timer, in TimerRange timerRange, in ClockSettings clockSettings, in ClockData clockData,
                in DynamicBuffer<TimerDataLink> timerDataLinks)
            {
                timer.DeltaTime = DiscreteTime.Zero;
                timer.TimeScale = clockData.Scale;
                timer.Time = clockSettings.Reverse ? timerRange.Range.End : timerRange.Range.Start;

                foreach (var link in timerDataLinks.AsNativeArray())
                {
                    this.Actives.SetComponentEnabled(link.Value, true);
                }

                var source = new TimerData
                {
                    DeltaTime = timer.DeltaTime,
                    TimeScale = timer.TimeScale,
                    Time = timer.Time,
                };

                UpdateCompositeTimers(entity, source, timerDataLinks, this.TimerDatas, this.TimerDataLinks, this.CompositeTimerLinks, this.CompositeTimers,
                    this.Timers, this.Actives);
            }
        }

        [BurstCompile]
        private struct TimerStoppedJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<TimerDataLink> TimerDataLinks;

            public ComponentTypeHandle<TimerPaused> TimerPausedHandle;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<TimelineActive> Actives;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var timerPauseds = chunk.GetEnabledMask(ref this.TimerPausedHandle);
                var timerDataLinksAccessor = chunk.GetBufferAccessor(ref this.TimerDataLinks);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var index))
                {
                    timerPauseds[index] = false;

                    foreach (var link in timerDataLinksAccessor[index].AsNativeArray())
                    {
                        this.Actives.SetComponentEnabled(link.Value, false);
                    }
                }
            }
        }

        [WithAll(typeof(TimelineActive), typeof(TimelineActivePrevious))]
        [WithPresent(typeof(TimerPaused))]
        [WithNone(typeof(CompositeTimer))]
        [BurstCompile]
        private partial struct TimersUpdateJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<TimerData> TimerDatas;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<TimerPaused> TimerPauseds;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<TimelineActive> Actives;

            [ReadOnly]
            [NativeDisableContainerSafetyRestriction]
            public BufferLookup<TimerDataLink> TimerDataLinks;

            [ReadOnly]
            public BufferLookup<CompositeTimerLink> CompositeTimerLinks;

            [ReadOnly]
            public ComponentLookup<CompositeTimer> CompositeTimers;

            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<Timer> Timers;

            private void Execute(
                Entity entity, ref Timer timer, ref TimerRange timerRange, in ClockSettings clockSettings, in ClockData clockData,
                in DynamicBuffer<TimerDataLink> timerDataLinks)
            {
                var timerPaused = this.TimerPauseds.GetEnabledRefRW<TimerPaused>(entity);
                var active = this.Actives.GetEnabledRefRW<TimelineActive>(entity);

                var previousTime = timer.Time;

                timer.DeltaTime = timerPaused.ValueRO ? DiscreteTime.Zero : clockData.DeltaTime;
                timer.Time += timer.DeltaTime;
                timer.TimeScale = clockData.Scale;

                if (!timerPaused.ValueRO)
                {
                    TimerRangeImpl.ApplyTimerRange(ref timer, ref timerRange, previousTime, timerPaused, active, clockSettings.Reverse);
                }

                var source = new TimerData
                {
                    DeltaTime = timer.DeltaTime,
                    TimeScale = timer.TimeScale,
                    Time = timer.Time,
                };

                UpdateCompositeTimers(entity, source, timerDataLinks, this.TimerDatas, this.TimerDataLinks, this.CompositeTimerLinks, this.CompositeTimers,
                    this.Timers, this.Actives);
            }
        }
    }
}
