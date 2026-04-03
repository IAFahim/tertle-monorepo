// <copyright file="ClipWeightSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Runtime.Timeline
{
    using BovineLabs.Testing;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Tests.TestDoubles;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.IntegerTime;

    public class ClipWeightSystemTests : ECSTestsFixture
    {
        private SystemHandle clipWeightSystem;

        public override void Setup()
        {
            base.Setup();
            this.clipWeightSystem = this.World.CreateSystem<ClipWeightSystem>();
        }

        [Test]
        public void MissingCurve_AssignsWeightOne()
        {
            var entity = this.Manager.CreateEntity(typeof(ClipWeight), typeof(AnimatedClipWeight), typeof(LocalTime));
            this.Manager.SetComponentData(entity, new ClipWeight { Value = 0.2f });
            this.Manager.SetComponentData(entity, new AnimatedClipWeight());
            this.Manager.SetComponentData(entity, new LocalTime { Value = DiscreteTime.FromTicks(5) });

            this.clipWeightSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var weight = this.Manager.GetComponentData<ClipWeight>(entity);
            Assert.AreEqual(1f, weight.Value, 0.0001f);
        }

        [Test]
        public void CreatedCurve_EvaluatesSamplerAtLocalTime()
        {
            using var curve = BlobCurveTestHelpers.CreateLinear(0f, 0f, 10f, 1f);
            var localTime = new LocalTime { Value = new DiscreteTime(5.0) };

            var entity = this.Manager.CreateEntity(typeof(ClipWeight), typeof(AnimatedClipWeight), typeof(LocalTime));
            this.Manager.SetComponentData(entity, new ClipWeight { Value = 0f });
            this.Manager.SetComponentData(entity, new AnimatedClipWeight { Value = curve.Sampler });
            this.Manager.SetComponentData(entity, localTime);

            var expected = curve.Sampler.Evaluate((float)localTime.Value);

            this.clipWeightSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var weight = this.Manager.GetComponentData<ClipWeight>(entity);
            Assert.AreEqual(expected, weight.Value, 0.0001f);
        }
    }
}
