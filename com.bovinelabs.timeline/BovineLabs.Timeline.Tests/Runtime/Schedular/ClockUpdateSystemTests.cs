// <copyright file="ClockUpdateSystemTests.cs" company="BovineLabs">
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

    public class ClockUpdateSystemTests : ECSTestsFixture
    {
        private SystemHandle clockUpdateSystem;

        public override void Setup()
        {
            base.Setup();
            this.clockUpdateSystem = this.World.CreateSystem<ClockUpdateSystem>();
        }

        [Test]
        public void ConstantMode_CopiesDeltaTimeAndScaleFromSettings()
        {
            var entity = TimelineTestHelpers.CreateClockEntity(
                this.Manager,
                new ClockSettings
                {
                    UpdateMode = ClockUpdateMode.Constant,
                    DeltaTime = Ticks(6),
                    TimeScale = 1.5f,
                    Reverse = false,
                },
                new ClockData
                {
                    DeltaTime = Ticks(99),
                    Scale = 9.0,
                });

            this.clockUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var clockData = this.Manager.GetComponentData<ClockData>(entity);
            Assert.AreEqual(Ticks(6), clockData.DeltaTime);
            Assert.AreEqual(1.5, clockData.Scale, 0.0001);
        }

        [Test]
        public void ConstantModeReverse_NegatesDeltaTime()
        {
            var entity = TimelineTestHelpers.CreateClockEntity(
                this.Manager,
                new ClockSettings
                {
                    UpdateMode = ClockUpdateMode.Constant,
                    DeltaTime = Ticks(4),
                    TimeScale = 3f,
                    Reverse = true,
                },
                new ClockData());

            this.clockUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var clockData = this.Manager.GetComponentData<ClockData>(entity);
            Assert.AreEqual(Ticks(-4), clockData.DeltaTime);
            Assert.AreEqual(3.0, clockData.Scale, 0.0001);
        }

        [Test]
        public void UnscaledMode_ForcesScaleToOne()
        {
            var entity = TimelineTestHelpers.CreateClockEntity(
                this.Manager,
                new ClockSettings
                {
                    UpdateMode = ClockUpdateMode.UnscaledGameTime,
                    DeltaTime = Ticks(999),
                    TimeScale = 7.5f,
                    Reverse = false,
                },
                new ClockData());

            this.clockUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var clockData = this.Manager.GetComponentData<ClockData>(entity);
            Assert.AreEqual(1.0, clockData.Scale, 0.0001);
        }

        [Test]
        public void DisabledTimelineActive_EntityRemainsUnchanged()
        {
            var initialData = new ClockData
            {
                DeltaTime = Ticks(77),
                Scale = 2.0,
            };

            var entity = TimelineTestHelpers.CreateClockEntity(
                this.Manager,
                new ClockSettings
                {
                    UpdateMode = ClockUpdateMode.Constant,
                    DeltaTime = Ticks(5),
                    TimeScale = 0.5f,
                    Reverse = false,
                },
                initialData,
                false);

            this.clockUpdateSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var clockData = this.Manager.GetComponentData<ClockData>(entity);
            Assert.AreEqual(initialData.DeltaTime, clockData.DeltaTime);
            Assert.AreEqual(initialData.Scale, clockData.Scale, 0.0001);
        }

        private static DiscreteTime Ticks(long ticks)
        {
            return DiscreteTime.FromTicks(ticks);
        }
    }
}
