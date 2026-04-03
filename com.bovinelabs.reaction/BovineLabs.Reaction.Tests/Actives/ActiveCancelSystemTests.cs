// <copyright file="ActiveCancelSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Actives
{
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Data.Active;
    using NUnit.Framework;
    using Unity.Entities;

    /// <summary>
    /// Tests for <see cref="ActiveCancelSystem"/>, verifying cancel operation handling
    /// for active reactions.
    /// </summary>
    public class ActiveCancelSystemTests : ReactionTestFixture
    {
        private SystemHandle activeCancelSystem;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();
            this.activeCancelSystem = this.World.CreateSystem<ActiveCancelSystem>();
        }

        [Test]
        public void Cancel_DisablesActiveCancelComponent()
        {
            // Create entity with ActiveCancel enabled and some remaining duration
            var entity = this.CreateCancelEntity(5.0f, true);

            // Run the system
            this.activeCancelSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify ActiveCancel component is disabled
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity), "ActiveCancel should be disabled after processing");
        }

        [Test]
        public void Cancel_SetsRemainingDurationToZero()
        {
            // Create entity with ActiveCancel enabled and some remaining duration
            var entity = this.CreateCancelEntity(5.0f, true);

            // Run the system
            this.activeCancelSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify duration remaining is set to zero
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(0.0f, remaining.Value, 0.001f, "ActiveDurationRemaining should be set to zero");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be cancelled");
        }

        [Test]
        public void Cancel_ProcessesMultipleEntities()
        {
            // Create multiple entities with ActiveCancel enabled
            var entity1 = this.CreateCancelEntity(3.0f, true);
            var entity2 = this.CreateCancelEntity(7.0f, true);
            var entity3 = this.CreateCancelEntity(1.5f, true);

            // Run the system
            this.activeCancelSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify all entities processed
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity1), "Entity1 ActiveCancel should be disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity2), "Entity2 ActiveCancel should be disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity3), "Entity3 ActiveCancel should be disabled");

            var remaining1 = this.Manager.GetComponentData<ActiveDurationRemaining>(entity1);
            var remaining2 = this.Manager.GetComponentData<ActiveDurationRemaining>(entity2);
            var remaining3 = this.Manager.GetComponentData<ActiveDurationRemaining>(entity3);

            Assert.AreEqual(0.0f, remaining1.Value, 0.001f, "Entity1 duration should be zero");
            Assert.AreEqual(0.0f, remaining2.Value, 0.001f, "Entity2 duration should be zero");
            Assert.AreEqual(0.0f, remaining3.Value, 0.001f, "Entity3 duration should be zero");

            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity1), "ActiveOnDuration should be cancelled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity2), "ActiveOnDuration should be cancelled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity3), "ActiveOnDuration should be cancelled");
        }

        [Test]
        public void Cancel_IgnoresDisabledActiveCancelComponent()
        {
            // Create entity with ActiveCancel disabled
            var entity = this.CreateCancelEntity(5.0f, false);
            var originalRemaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);

            // Run the system
            this.activeCancelSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify no changes occurred
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity), "ActiveCancel should remain disabled");
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(originalRemaining.Value, remaining.Value, 0.001f, "Duration should remain unchanged");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be cancelled");
        }

        [Test]
        public void Cancel_HandlesZeroDurationRemaining()
        {
            // Create entity with ActiveCancel enabled and zero remaining duration
            var entity = this.CreateCancelEntity(0.0f, true);

            // Run the system
            this.activeCancelSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify system processes entity correctly even with zero duration
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity), "ActiveCancel should be disabled");
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(0.0f, remaining.Value, 0.001f, "Duration should remain zero");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be cancelled");
        }

        [Test]
        public void Cancel_HandlesNegativeDurationRemaining()
        {
            // Create entity with ActiveCancel enabled and negative remaining duration
            var entity = this.CreateCancelEntity(-2.0f, true);

            // Run the system
            this.activeCancelSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify system processes entity correctly even with negative duration
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity), "ActiveCancel should be disabled");
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(0.0f, remaining.Value, 0.001f, "Duration should be set to zero regardless of initial value");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be cancelled");
        }

        [Test]
        public void Cancel_CanBeReenabledAfterProcessing()
        {
            // Create entity with ActiveCancel enabled
            var entity = this.CreateCancelEntity(5.0f, true);

            // Run the system first time
            this.activeCancelSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify first cancellation
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity), "ActiveCancel should be disabled after first run");
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(0.0f, remaining.Value, 0.001f, "Duration should be zero after first run");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be cancelled");

            // Re-enable ActiveCancel and set new duration
            this.Manager.SetComponentEnabled<ActiveCancel>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.Manager.SetComponentData(entity, new ActiveDurationRemaining { Value = 3.0f });

            // Run the system second time
            this.activeCancelSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify second cancellation works
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity), "ActiveCancel should be disabled after second run");
            remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(0.0f, remaining.Value, 0.001f, "Duration should be zero after second run");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be cancelled");
        }

        /// <summary>
        /// Creates a test entity with ActiveCancel and ActiveDurationRemaining components.
        /// </summary>
        /// <param name="remainingValue">The remaining duration value to set.</param>
        /// <param name="cancelEnabled">Whether ActiveCancel component should be enabled.</param>
        /// <returns>Entity with cancel components configured.</returns>
        private Entity CreateCancelEntity(float remainingValue, bool cancelEnabled)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ActiveCancel),
                typeof(ActiveOnDuration),
                typeof(ActiveDurationRemaining));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new ActiveDurationRemaining { Value = remainingValue });
            this.Manager.SetComponentEnabled<ActiveCancel>(entity, cancelEnabled);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);

            return entity;
        }
    }
}