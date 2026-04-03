// <copyright file="TimerUpdateSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Runtime.Schedular
{
    using BovineLabs.Testing;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using BovineLabs.Timeline.Schedular;
    using BovineLabs.Timeline.Tests.TestDoubles;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.IntegerTime;

    public class TimerUpdateSystemTests : ECSTestsFixture
    {
        private SystemHandle timerUpdateSystem;

        public override void Setup()
        {
            base.Setup();
            this.timerUpdateSystem = this.World.CreateSystem<TimerUpdateSystem>();
        }

        [Test]
        public void StartedTimeline_ForwardClock_InitializesTimerAtRangeStart()
        {
            var linkedTimerData = TimelineTestHelpers.CreateTimerDataTarget(this.Manager, default);
            var timerEntity = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(123) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.AutoPause,
                    Range = new DiscreteTimeInterval(Ticks(10), Ticks(20)),
                },
                new ClockSettings
                {
                    Reverse = false,
                },
                new ClockData
                {
                    DeltaTime = Ticks(3),
                    Scale = 1.5,
                },
                true,
                false);

            TimelineTestHelpers.AddTimerDataLink(this.Manager, timerEntity, linkedTimerData);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timer = this.Manager.GetComponentData<Timer>(timerEntity);
            Assert.AreEqual(Ticks(10), timer.Time);
            Assert.AreEqual(DiscreteTime.Zero, timer.DeltaTime);
            Assert.AreEqual(1.5, timer.TimeScale, 0.0001);
            Assert.IsTrue(this.Manager.IsComponentEnabled<TimelineActive>(linkedTimerData));

            var linkedData = this.Manager.GetComponentData<TimerData>(linkedTimerData);
            Assert.AreEqual(Ticks(10), linkedData.Time);
            Assert.AreEqual(DiscreteTime.Zero, linkedData.DeltaTime);
            Assert.AreEqual(1.5, linkedData.TimeScale, 0.0001);
        }

        [Test]
        public void StartedTimeline_ReverseClock_InitializesTimerAtRangeEnd()
        {
            var linkedTimerData = TimelineTestHelpers.CreateTimerDataTarget(this.Manager, default);
            var timerEntity = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(123) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.AutoPause,
                    Range = new DiscreteTimeInterval(Ticks(10), Ticks(20)),
                },
                new ClockSettings
                {
                    Reverse = true,
                },
                new ClockData
                {
                    DeltaTime = Ticks(-3),
                    Scale = 0.5,
                },
                true,
                false);

            TimelineTestHelpers.AddTimerDataLink(this.Manager, timerEntity, linkedTimerData);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timer = this.Manager.GetComponentData<Timer>(timerEntity);
            Assert.AreEqual(Ticks(20), timer.Time);
            Assert.AreEqual(DiscreteTime.Zero, timer.DeltaTime);
            Assert.AreEqual(0.5, timer.TimeScale, 0.0001);
            Assert.IsTrue(this.Manager.IsComponentEnabled<TimelineActive>(linkedTimerData));
        }

        [Test]
        public void AutoPause_Forward_ClampsEndAndEnablesPaused()
        {
            var timerEntity = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(9) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.AutoPause,
                    Range = new DiscreteTimeInterval(Ticks(0), Ticks(10)),
                },
                new ClockSettings { Reverse = false },
                new ClockData
                {
                    DeltaTime = Ticks(5),
                    Scale = 1.0,
                },
                true,
                true);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timer = this.Manager.GetComponentData<Timer>(timerEntity);
            Assert.AreEqual(Ticks(10), timer.Time);
            Assert.IsTrue(this.Manager.IsComponentEnabled<TimerPaused>(timerEntity));
        }

        [Test]
        public void AutoPause_Reverse_ClampsStartAndEnablesPaused()
        {
            var timerEntity = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(2) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.AutoPause,
                    Range = new DiscreteTimeInterval(Ticks(0), Ticks(10)),
                },
                new ClockSettings { Reverse = true },
                new ClockData
                {
                    DeltaTime = Ticks(-5),
                    Scale = 1.0,
                },
                true,
                true);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timer = this.Manager.GetComponentData<Timer>(timerEntity);
            Assert.AreEqual(Ticks(0), timer.Time);
            Assert.IsTrue(this.Manager.IsComponentEnabled<TimerPaused>(timerEntity));
        }

        [Test]
        public void AutoStop_Forward_SampleLastFrameThenResetAndDisable()
        {
            var timerEntity = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(6) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.AutoStop,
                    Range = new DiscreteTimeInterval(Ticks(0), Ticks(10)),
                    SampleLastFrame = true,
                },
                new ClockSettings { Reverse = false },
                new ClockData
                {
                    DeltaTime = Ticks(6),
                    Scale = 1.0,
                },
                true,
                true);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timerAfterFirstUpdate = this.Manager.GetComponentData<Timer>(timerEntity);
            Assert.AreEqual(Ticks(10), timerAfterFirstUpdate.Time);
            Assert.IsTrue(this.Manager.IsComponentEnabled<TimelineActive>(timerEntity));

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timerAfterSecondUpdate = this.Manager.GetComponentData<Timer>(timerEntity);
            Assert.AreEqual(Ticks(0), timerAfterSecondUpdate.Time);
            Assert.IsFalse(this.Manager.IsComponentEnabled<TimelineActive>(timerEntity));
        }

        [Test]
        public void AutoStop_Reverse_SampleLastFrameThenResetAndDisable()
        {
            var timerEntity = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(4) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.AutoStop,
                    Range = new DiscreteTimeInterval(Ticks(0), Ticks(10)),
                    SampleLastFrame = true,
                },
                new ClockSettings { Reverse = true },
                new ClockData
                {
                    DeltaTime = Ticks(-6),
                    Scale = 1.0,
                },
                true,
                true);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timerAfterFirstUpdate = this.Manager.GetComponentData<Timer>(timerEntity);
            Assert.AreEqual(Ticks(0), timerAfterFirstUpdate.Time);
            Assert.IsTrue(this.Manager.IsComponentEnabled<TimelineActive>(timerEntity));

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timerAfterSecondUpdate = this.Manager.GetComponentData<Timer>(timerEntity);
            Assert.AreEqual(Ticks(10), timerAfterSecondUpdate.Time);
            Assert.IsFalse(this.Manager.IsComponentEnabled<TimelineActive>(timerEntity));
        }

        [Test]
        public void Loop_Forward_WrapsAndIncrementsLoopCount()
        {
            var timerEntity = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(8) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.Loop,
                    Range = new DiscreteTimeInterval(Ticks(0), Ticks(10)),
                },
                new ClockSettings { Reverse = false },
                new ClockData
                {
                    DeltaTime = Ticks(7),
                    Scale = 1.0,
                },
                true,
                true);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timer = this.Manager.GetComponentData<Timer>(timerEntity);
            var timerRange = this.Manager.GetComponentData<TimerRange>(timerEntity);
            Assert.AreEqual(Ticks(5), timer.Time);
            Assert.AreEqual(1u, timerRange.LoopCount);
        }

        [Test]
        public void Loop_Reverse_WrapsUsingRemainderAndIncrementsLoopCount()
        {
            var timerEntity = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(2) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.Loop,
                    Range = new DiscreteTimeInterval(Ticks(0), Ticks(10)),
                },
                new ClockSettings { Reverse = true },
                new ClockData
                {
                    DeltaTime = Ticks(-15),
                    Scale = 1.0,
                },
                true,
                true);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var timer = this.Manager.GetComponentData<Timer>(timerEntity);
            var timerRange = this.Manager.GetComponentData<TimerRange>(timerEntity);
            Assert.AreEqual(Ticks(7), timer.Time);
            Assert.AreEqual(1u, timerRange.LoopCount);
        }

        [Test]
        public void StoppedTimeline_ClearsPausedAndDisablesLinkedTimelineActives()
        {
            var linkedTimeline = TimelineTestHelpers.CreateTimerDataTarget(this.Manager, default, true);
            var timerEntity = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(0) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.AutoPause,
                    Range = new DiscreteTimeInterval(Ticks(0), Ticks(10)),
                },
                new ClockSettings(),
                new ClockData(),
                false,
                true,
                true);

            TimelineTestHelpers.AddTimerDataLink(this.Manager, timerEntity, linkedTimeline);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsFalse(this.Manager.IsComponentEnabled<TimerPaused>(timerEntity));
            Assert.IsFalse(this.Manager.IsComponentEnabled<TimelineActive>(linkedTimeline));
        }

        [Test]
        public void CompositeTimers_PropagateTimerDataAndToggleLinkedActives()
        {
            var rootListener = TimelineTestHelpers.CreateTimerDataTarget(this.Manager, default, true);
            var childListener = TimelineTestHelpers.CreateTimerDataTarget(this.Manager, default);

            var rootTimer = TimelineTestHelpers.CreateTimerEntity(
                this.Manager,
                new Timer { Time = Ticks(5) },
                new TimerRange
                {
                    Behaviour = RangeBehaviour.Loop,
                    Range = new DiscreteTimeInterval(Ticks(0), Ticks(100)),
                },
                new ClockSettings { Reverse = false },
                new ClockData
                {
                    DeltaTime = Ticks(4),
                    Scale = 1.5,
                },
                true,
                true);

            var childComposite = TimelineTestHelpers.CreateCompositeTimerEntity(
                this.Manager,
                new CompositeTimer
                {
                    SourceTimer = rootTimer,
                    Scale = 2.0,
                    Offset = Ticks(3),
                    ActiveRange = new ActiveRange
                    {
                        Start = Ticks(8),
                        End = Ticks(10),
                    },
                },
                new Timer(),
                false);

            TimelineTestHelpers.AddTimerDataLink(this.Manager, rootTimer, rootListener);
            TimelineTestHelpers.AddTimerDataLink(this.Manager, childComposite, childListener);
            TimelineTestHelpers.AddCompositeTimerLink(this.Manager, rootTimer, childComposite);

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var rootListenerData = this.Manager.GetComponentData<TimerData>(rootListener);
            Assert.AreEqual(Ticks(9), rootListenerData.Time);
            Assert.AreEqual(Ticks(4), rootListenerData.DeltaTime);
            Assert.AreEqual(1.5, rootListenerData.TimeScale, 0.0001);

            var childTimer = this.Manager.GetComponentData<Timer>(childComposite);
            Assert.AreEqual(Ticks(21), childTimer.Time);
            Assert.AreEqual(Ticks(8), childTimer.DeltaTime);
            Assert.AreEqual(3.0, childTimer.TimeScale, 0.0001);

            var childListenerData = this.Manager.GetComponentData<TimerData>(childListener);
            Assert.AreEqual(Ticks(21), childListenerData.Time);
            Assert.AreEqual(Ticks(8), childListenerData.DeltaTime);
            Assert.AreEqual(3.0, childListenerData.TimeScale, 0.0001);

            Assert.IsTrue(this.Manager.IsComponentEnabled<TimelineActive>(childComposite));
            Assert.IsTrue(this.Manager.IsComponentEnabled<TimelineActive>(childListener));

            this.Manager.SetComponentData(rootTimer, new ClockData
            {
                DeltaTime = Ticks(2),
                Scale = 1.5,
            });

            this.timerUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsFalse(this.Manager.IsComponentEnabled<TimelineActive>(childComposite));
            Assert.IsFalse(this.Manager.IsComponentEnabled<TimelineActive>(childListener));
        }

        private static DiscreteTime Ticks(long ticks)
        {
            return DiscreteTime.FromTicks(ticks);
        }
    }
}
