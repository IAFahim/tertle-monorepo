// <copyright file="ClipActivePreviousSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Runtime.Timeline
{
    using BovineLabs.Testing;
    using BovineLabs.Timeline.Data;
    using NUnit.Framework;
    using Unity.Entities;

    public class ClipActivePreviousSystemTests : ECSTestsFixture
    {
        private SystemHandle clipActivePreviousSystem;

        public override void Setup()
        {
            base.Setup();
            this.clipActivePreviousSystem = this.World.CreateSystem<ClipActivePreviousSystem>();
        }

        [Test]
        public void EnabledClipActive_IsCopiedToClipActivePrevious()
        {
            var entity = this.Manager.CreateEntity(typeof(ClipActive), typeof(ClipActivePrevious));
            this.Manager.SetComponentEnabled<ClipActive>(entity, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(entity, false);

            this.clipActivePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.IsComponentEnabled<ClipActivePrevious>(entity));
        }

        [Test]
        public void DisabledClipActive_IsCopiedToClipActivePrevious()
        {
            var entity = this.Manager.CreateEntity(typeof(ClipActive), typeof(ClipActivePrevious));
            this.Manager.SetComponentEnabled<ClipActive>(entity, false);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(entity, true);

            this.clipActivePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsFalse(this.Manager.IsComponentEnabled<ClipActivePrevious>(entity));
        }

        [Test]
        public void MissingEitherComponent_IsIgnoredSafely()
        {
            var onlyActive = this.Manager.CreateEntity(typeof(ClipActive));
            var onlyPrevious = this.Manager.CreateEntity(typeof(ClipActivePrevious));

            this.Manager.SetComponentEnabled<ClipActive>(onlyActive, true);
            this.Manager.SetComponentEnabled<ClipActivePrevious>(onlyPrevious, true);

            this.clipActivePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.IsComponentEnabled<ClipActive>(onlyActive));
            Assert.IsTrue(this.Manager.IsComponentEnabled<ClipActivePrevious>(onlyPrevious));
        }
    }
}
