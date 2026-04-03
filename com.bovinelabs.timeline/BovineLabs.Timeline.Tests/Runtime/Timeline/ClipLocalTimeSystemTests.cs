// <copyright file="ClipLocalTimeSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Runtime.Timeline
{
    using BovineLabs.Testing;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using BovineLabs.Timeline.Tests.TestDoubles;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.IntegerTime;

    public class ClipLocalTimeSystemTests : ECSTestsFixture
    {
        private SystemHandle clipLocalTimeSystem;

        public override void Setup()
        {
            base.Setup();
            this.clipLocalTimeSystem = this.World.CreateSystem<ClipLocalTimeSystem>();
        }

        [Test]
        public void BasePath_ComputesLocalTimeAndClipActive()
        {
            var clip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime { Value = Ticks(0) },
                new TimerData { Time = Ticks(15) },
                new TimeTransform
                {
                    Start = Ticks(10),
                    End = Ticks(20),
                    ClipIn = Ticks(2),
                    Scale = 1.0,
                },
                true,
                false);

            this.clipLocalTimeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(Ticks(7), this.Manager.GetComponentData<LocalTime>(clip).Value);
            Assert.IsTrue(this.Manager.IsComponentEnabled<ClipActive>(clip));
        }

        [Test]
        public void InactiveTimeline_ResetsClipActiveToFalse()
        {
            var clip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime { Value = Ticks(5) },
                new TimerData { Time = Ticks(12) },
                new TimeTransform
                {
                    Start = Ticks(10),
                    End = Ticks(20),
                    Scale = 1.0,
                },
                false,
                true);

            this.clipLocalTimeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsFalse(this.Manager.IsComponentEnabled<ClipActive>(clip));
        }

        [Test]
        public void LoopExtrapolation_PreAndPost_RemapAsExpected()
        {
            var preClip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime(),
                new TimerData { Time = Ticks(7) },
                CreateTransform(Ticks(10), Ticks(20), Ticks(1), 1.0),
                true,
                false);
            this.Manager.AddComponentData(preClip, new ExtrapolationLoop { ExtrapolateOptions = ExtrapolationPosition.Pre });

            var postClip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime(),
                new TimerData { Time = Ticks(24) },
                CreateTransform(Ticks(10), Ticks(20), Ticks(1), 1.0),
                true,
                false);
            this.Manager.AddComponentData(postClip, new ExtrapolationLoop { ExtrapolateOptions = ExtrapolationPosition.Post });

            this.clipLocalTimeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(Ticks(8), this.Manager.GetComponentData<LocalTime>(preClip).Value);
            Assert.AreEqual(Ticks(5), this.Manager.GetComponentData<LocalTime>(postClip).Value);
        }

        [Test]
        public void PingPongExtrapolation_PreAndPost_RemapAsExpected()
        {
            var preClip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime(),
                new TimerData { Time = Ticks(7) },
                CreateTransform(Ticks(10), Ticks(20), Ticks(0), 1.0),
                true,
                false);
            this.Manager.AddComponentData(preClip, new ExtrapolationPingPong { ExtrapolateOptions = ExtrapolationPosition.Pre });

            var postClip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime(),
                new TimerData { Time = Ticks(24) },
                CreateTransform(Ticks(10), Ticks(20), Ticks(0), 1.0),
                true,
                false);
            this.Manager.AddComponentData(postClip, new ExtrapolationPingPong { ExtrapolateOptions = ExtrapolationPosition.Post });

            this.clipLocalTimeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(Ticks(3), this.Manager.GetComponentData<LocalTime>(preClip).Value);
            Assert.AreEqual(Ticks(6), this.Manager.GetComponentData<LocalTime>(postClip).Value);
        }

        [Test]
        public void HoldExtrapolation_PreAndPost_ClampToExpectedBounds()
        {
            var preClip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime(),
                new TimerData { Time = Ticks(7) },
                CreateTransform(Ticks(10), Ticks(20), Ticks(2), 1.0),
                true,
                false);
            this.Manager.AddComponentData(preClip, new ExtrapolationHold { ExtrapolateOptions = ExtrapolationPosition.Pre });

            var postClip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime(),
                new TimerData { Time = Ticks(24) },
                CreateTransform(Ticks(10), Ticks(20), Ticks(2), 1.0),
                true,
                false);
            this.Manager.AddComponentData(postClip, new ExtrapolationHold { ExtrapolateOptions = ExtrapolationPosition.Post });

            this.clipLocalTimeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(Ticks(2), this.Manager.GetComponentData<LocalTime>(preClip).Value);
            Assert.AreEqual(Ticks(12), this.Manager.GetComponentData<LocalTime>(postClip).Value);
        }

        [Test]
        public void LoopAndPingPong_ZeroOrNegativeDuration_SetLocalTimeToZero()
        {
            var loopClip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime { Value = Ticks(99) },
                new TimerData { Time = Ticks(0) },
                CreateTransform(Ticks(10), Ticks(10), Ticks(2), 1.0),
                true,
                true);
            this.Manager.AddComponentData(loopClip, new ExtrapolationLoop { ExtrapolateOptions = ExtrapolationPosition.Both });

            var pingPongClip = TimelineTestHelpers.CreateClipEntity(
                this.Manager,
                new LocalTime { Value = Ticks(99) },
                new TimerData { Time = Ticks(0) },
                CreateTransform(Ticks(10), Ticks(10), Ticks(2), 1.0),
                true,
                true);
            this.Manager.AddComponentData(pingPongClip, new ExtrapolationPingPong { ExtrapolateOptions = ExtrapolationPosition.Both });

            this.clipLocalTimeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(DiscreteTime.Zero, this.Manager.GetComponentData<LocalTime>(loopClip).Value);
            Assert.AreEqual(DiscreteTime.Zero, this.Manager.GetComponentData<LocalTime>(pingPongClip).Value);
        }

        private static TimeTransform CreateTransform(DiscreteTime start, DiscreteTime end, DiscreteTime clipIn, double scale)
        {
            return new TimeTransform
            {
                Start = start,
                End = end,
                ClipIn = clipIn,
                Scale = scale,
            };
        }

        private static DiscreteTime Ticks(long ticks)
        {
            return DiscreteTime.FromTicks(ticks);
        }
    }
}
