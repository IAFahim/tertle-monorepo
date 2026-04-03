// <copyright file="ActiveDurationSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Actives
{
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Data.Active;
    using NUnit.Framework;
    using Unity.Core;
    using Unity.Entities;

    /// <summary>
    /// Tests for <see cref="ActiveDurationSystem"/>, verifying duration timer management
    /// for time-limited reactions.
    /// </summary>
    public class ActiveDurationSystemTests : ReactionTestFixture
    {
        private SystemHandle activeDurationSystem;
        private SystemHandle activePreviousSystem;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();
            this.activeDurationSystem = this.World.CreateSystem<ActiveDurationSystem>();
            this.activePreviousSystem = this.World.CreateSystem<ActivePreviousSystem>();
        }

        [Test]
        public void DurationTimer_StartsWhenActiveTriggered()
        {
            // Create entity with duration components
            var entity = this.CreateDurationEntity(5.0f, 0.0f, true, false);

            // Run the system
            this.RunActiveSystem();

            // Verify duration timer started
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(5.0f, remaining.Value, 0.001f, "Duration timer should be set to full duration");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be enabled");
        }

        [Test]
        public void DurationTimer_CountsDownOverTime()
        {
            // Create entity with duration components
            var entity = this.CreateDurationEntity(5.0f, 3.0f, false, true);

            // Simulate time passing (1 second)
            this.World.SetTime(new TimeData(1.0, 1.0f));
            this.RunActiveSystem();

            // Verify countdown
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(2.0f, remaining.Value, 0.001f, "Duration should count down by delta time");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should remain enabled");
        }

        [Test]
        public void DurationTimer_DisablesWhenExpired()
        {
            // Create entity with duration components
            var entity = this.CreateDurationEntity(5.0f, 0.5f, false, true);

            // Simulate time passing (1 second - should expire)
            this.World.SetTime(new TimeData(1.0, 1.0f));
            this.RunActiveSystem();

            // Verify expiration
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.LessOrEqual(remaining.Value, 0.0f, "Duration should be expired");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be disabled when expired");
        }

        [Test]
        public void DurationTimer_HandlesZeroDuration()
        {
            // Create entity with zero duration
            var entity = this.CreateDurationEntity(0.0f, 0.0f, true, false);

            // Run the system
            this.RunActiveSystem();

            // Verify immediate expiration
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "Zero duration should not enable ActiveOnDuration");
        }

        [Test]
        public void DurationTimer_HandlesNegativeDuration()
        {
            // Create entity with negative duration
            var entity = this.CreateDurationEntity(-1.0f, 0.0f, true, false);

            // Run the system
            this.RunActiveSystem();

            // Verify negative duration is handled (should not enable)
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "Negative duration should not enable ActiveOnDuration");
        }

        [Test]
        public void DurationTimer_CanBeCancelledEarly()
        {
            // Create entity with duration components
            var entity = this.CreateDurationEntity(5.0f, 3.0f, false, true);

            // Cancel by setting remaining to zero
            this.Manager.SetComponentData(entity, new ActiveDurationRemaining { Value = 0.0f });

            // Run the system
            this.RunActiveSystem();

            // Verify cancellation
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "Duration should be cancelled when remaining set to zero");
        }

        [Test]
        public void DurationTimer_RestartsWhenActiveRetriggered()
        {
            // Create entity with duration components
            var entity = this.CreateDurationEntity(5.0f, 0.0f, true, false);

            // Run first to start timer
            this.RunActiveSystem();

            // Verify timer started
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(5.0f, remaining.Value, 0.001f, "Duration should start at full");

            // Let timer expire completely by running past the duration
            this.World.SetTime(new TimeData(6.0, 6.0f));
            this.RunActiveSystem();

            // Verify timer expired and ActiveOnDuration is disabled
            remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.LessOrEqual(remaining.Value, 0.0f, "Duration should be expired");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be disabled when expired");

            // Reset time and retrigger Active to restart the timer
            this.World.SetTime(new TimeData(6.0, 0.0f));
            this.Manager.SetComponentEnabled<ActivePrevious>(entity, false);
            this.Manager.SetComponentEnabled<Active>(entity, true);
            this.RunActiveSystem();

            // Verify timer restarted to full duration
            remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(5.0f, remaining.Value, 0.001f, "Duration timer should restart to full duration when retriggered after expiry");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be enabled");
        }

        [Test]
        public void DurationTimer_HandlesMultipleEntities()
        {
            // Create multiple entities with different durations
            var entity1 = this.CreateDurationEntity(1.0f, 0.5f, false, true);
            var entity2 = this.CreateDurationEntity(2.0f, 1.5f, false, true);
            var entity3 = this.CreateDurationEntity(3.0f, 2.5f, false, true);

            // Simulate time passing (1 second)
            this.World.SetTime(new TimeData(1.0, 1.0f));
            this.RunActiveSystem();

            // Verify each entity processed correctly
            var remaining1 = this.Manager.GetComponentData<ActiveDurationRemaining>(entity1);
            var remaining2 = this.Manager.GetComponentData<ActiveDurationRemaining>(entity2);
            var remaining3 = this.Manager.GetComponentData<ActiveDurationRemaining>(entity3);

            Assert.LessOrEqual(remaining1.Value, 0.0f, "Entity1 should be expired");
            Assert.AreEqual(0.5f, remaining2.Value, 0.001f, "Entity2 should have 0.5s remaining");
            Assert.AreEqual(1.5f, remaining3.Value, 0.001f, "Entity3 should have 1.5s remaining");

            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity1), "Entity1 ActiveOnDuration should be disabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity2), "Entity2 ActiveOnDuration should be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity3), "Entity3 ActiveOnDuration should be enabled");
        }

        [Test]
        public void DurationTimer_ComponentStateOptimization()
        {
            // Create entity to test that component state only changes at threshold crossings
            var entity = this.CreateDurationEntity(5.0f, 2.0f, false, true);

            // Small time step that doesn't cross zero threshold
            this.World.SetTime(new TimeData(0.1, 0.1f));
            this.RunActiveSystem();

            // Verify countdown but component still enabled
            var remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.AreEqual(1.9f, remaining.Value, 0.001f, "Duration should count down");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should remain enabled");

            // Large time step that crosses zero threshold
            this.World.SetTime(new TimeData(2.0, 2.0f));
            this.RunActiveSystem();

            // Verify expiration and component state change
            remaining = this.Manager.GetComponentData<ActiveDurationRemaining>(entity);
            Assert.LessOrEqual(remaining.Value, 0.0f, "Duration should be expired");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnDuration>(entity), "ActiveOnDuration should be disabled at threshold crossing");
        }

        /// <summary>
        /// Creates a test entity with duration components.
        /// </summary>
        /// <param name="durationValue">The duration value to set.</param>
        /// <param name="remainingValue">The remaining duration value to set.</param>
        /// <param name="activeEnabled">Whether Active component should be enabled.</param>
        /// <param name="onDurationEnabled">Whether ActiveOnDuration component should be enabled.</param>
        /// <returns>Entity with duration components configured.</returns>
        private Entity CreateDurationEntity(float durationValue, float remainingValue, bool activeEnabled, bool onDurationEnabled)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(ActivePrevious),
                typeof(ActiveDuration),
                typeof(ActiveDurationRemaining),
                typeof(ActiveOnDuration));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new ActiveDuration { Value = durationValue });
            this.Manager.SetComponentData(entity, new ActiveDurationRemaining { Value = remainingValue });
            this.Manager.SetComponentEnabled<Active>(entity, activeEnabled);
            this.Manager.SetComponentEnabled<ActivePrevious>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, onDurationEnabled);

            return entity;
        }

        private void RunActiveSystem()
        {
            this.RunSystems(this.activeDurationSystem, this.activePreviousSystem);
        }
    }
}
