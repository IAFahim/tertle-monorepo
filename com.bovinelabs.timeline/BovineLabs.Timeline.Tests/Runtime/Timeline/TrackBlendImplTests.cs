// <copyright file="TrackBlendImplTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Runtime.Timeline
{
    using BovineLabs.Testing;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Tests.TestDoubles;
    using NUnit.Framework;
    using Unity.Entities;

    public class TrackBlendImplTests : ECSTestsFixture
    {
        private SystemHandle hostSystem;

        public override void Setup()
        {
            base.Setup();
            this.hostSystem = this.World.CreateSystem<HostSystem>();
        }

        [Test]
        public void Update_CollectsUnblendedAndWeightedValuesForSameTrack()
        {
            ref var state = ref this.WorldUnmanaged.GetExistingSystemState<HostSystem>();
            var impl = default(TrackBlendImpl<float, TestAnimatedFloatComponent>);
            impl.OnCreate(ref state);

            try
            {
                var target = this.Manager.CreateEntity();
                CreateTrackClip(this.Manager, target, 11f, weighted: false, clipWeight: 0f, timelineActiveEnabled: true, clipActiveEnabled: true);
                CreateTrackClip(this.Manager, target, 22f, weighted: true, clipWeight: 0.75f, timelineActiveEnabled: true, clipActiveEnabled: true);

                var results = impl.Update(ref state);
                state.Dependency.Complete();

                Assert.IsTrue(results.TryGetValue(target, out var mix));
                Assert.AreEqual(11f, mix.Value1, 0.0001f);
                Assert.AreEqual(22f, mix.Value2, 0.0001f);
                Assert.AreEqual(1f, mix.Weights.x, 0.0001f);
                Assert.AreEqual(0.75f, mix.Weights.y, 0.0001f);
                Assert.AreEqual(0f, mix.Weights.z, 0.0001f);
                Assert.AreEqual(0f, mix.Weights.w, 0.0001f);
            }
            finally
            {
                impl.OnDestroy(ref state);
            }
        }

        [Test]
        public void Update_AppliesEnableFilters_AndClearsStaleResultsBetweenFrames()
        {
            ref var state = ref this.WorldUnmanaged.GetExistingSystemState<HostSystem>();
            var impl = default(TrackBlendImpl<float, TestAnimatedFloatComponent>);
            impl.OnCreate(ref state);

            try
            {
                var alwaysTarget = this.Manager.CreateEntity();
                var disabledTarget = this.Manager.CreateEntity();

                var alwaysClip = CreateTrackClip(
                    this.Manager,
                    alwaysTarget,
                    5f,
                    weighted: false,
                    clipWeight: 0f,
                    timelineActiveEnabled: true,
                    clipActiveEnabled: true);

                var disabledClip = CreateTrackClip(
                    this.Manager,
                    disabledTarget,
                    9f,
                    weighted: false,
                    clipWeight: 0f,
                    timelineActiveEnabled: true,
                    clipActiveEnabled: true);

                var firstResults = impl.Update(ref state);
                state.Dependency.Complete();
                Assert.IsTrue(firstResults.TryGetValue(alwaysTarget, out _));
                Assert.IsTrue(firstResults.TryGetValue(disabledTarget, out _));

                this.Manager.SetComponentEnabled<ClipActive>(disabledClip, false);
                this.Manager.SetComponentEnabled<TimelineActive>(alwaysClip, true);

                var secondResults = impl.Update(ref state);
                state.Dependency.Complete();

                Assert.IsTrue(secondResults.TryGetValue(alwaysTarget, out var alwaysMix));
                Assert.AreEqual(5f, alwaysMix.Value1, 0.0001f);
                Assert.IsFalse(secondResults.TryGetValue(disabledTarget, out _));
            }
            finally
            {
                impl.OnDestroy(ref state);
            }
        }

        private static Entity CreateTrackClip(
            EntityManager manager, Entity target, float value, bool weighted, float clipWeight, bool timelineActiveEnabled, bool clipActiveEnabled)
        {
            var entity = weighted
                ? manager.CreateEntity(typeof(TestAnimatedFloatComponent), typeof(TrackBinding), typeof(TimelineActive), typeof(ClipActive), typeof(ClipWeight))
                : manager.CreateEntity(typeof(TestAnimatedFloatComponent), typeof(TrackBinding), typeof(TimelineActive), typeof(ClipActive));

            manager.SetComponentData(entity, new TestAnimatedFloatComponent { CurrentValue = value });
            manager.SetComponentData(entity, new TrackBinding { Value = target });
            manager.SetComponentEnabled<TimelineActive>(entity, timelineActiveEnabled);
            manager.SetComponentEnabled<ClipActive>(entity, clipActiveEnabled);

            if (weighted)
            {
                manager.SetComponentData(entity, new ClipWeight { Value = clipWeight });
            }

            return entity;
        }

        private partial struct HostSystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
            }
        }
    }
}
