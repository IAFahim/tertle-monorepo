// <copyright file="ActiveCooldownSystemTests.cs" company="BovineLabs">
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
    /// Tests for <see cref="ActiveCooldownSystem"/>, verifying cooldown timer management
    /// for preventing reaction spam.
    /// </summary>
    public class ActiveCooldownSystemTests : ReactionTestFixture
    {
        private SystemHandle activeCooldownSystem;
        private SystemHandle activePreviousSystem;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();
            this.activeCooldownSystem = this.World.CreateSystem<ActiveCooldownSystem>();
            this.activePreviousSystem = this.World.CreateSystem<ActivePreviousSystem>();
        }

        [Test]
        public void CooldownTimer_StartsWhenActiveTriggered()
        {
            // Create entity with cooldown components
            var entity = this.CreateCooldownEntity(3.0f, 0.0f, true, false);

            // Run the system
            this.RunActiveSystem();

            // Verify cooldown timer started
            var remaining = this.Manager.GetComponentData<ActiveCooldownRemaining>(entity);
            Assert.AreEqual(3.0f, remaining.Value, 0.001f, "Cooldown timer should be set to full cooldown");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity), "ActiveOnCooldown should be enabled");
        }

        [Test]
        public void CooldownTimer_CountsDownOverTime()
        {
            // Create entity with cooldown components
            var entity = this.CreateCooldownEntity(3.0f, 2.0f, false, true);

            // Simulate time passing (0.5 seconds)
            this.World.SetTime(new TimeData(0.5, 0.5f));
            this.RunActiveSystem();

            // Verify countdown
            var remaining = this.Manager.GetComponentData<ActiveCooldownRemaining>(entity);
            Assert.AreEqual(1.5f, remaining.Value, 0.001f, "Cooldown should count down by delta time");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity), "ActiveOnCooldown should remain enabled");
        }

        [Test]
        public void CooldownTimer_DisablesWhenExpired()
        {
            // Create entity with cooldown components
            var entity = this.CreateCooldownEntity(3.0f, 0.3f, false, true);

            // Simulate time passing (0.5 seconds - should expire)
            this.World.SetTime(new TimeData(0.5, 0.5f));
            this.RunActiveSystem();

            // Verify expiration
            var remaining = this.Manager.GetComponentData<ActiveCooldownRemaining>(entity);
            Assert.LessOrEqual(remaining.Value, 0.0f, "Cooldown should be expired");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity), "ActiveOnCooldown should be disabled when expired");
        }

        [Test]
        public void CooldownTimer_HandlesZeroCooldown()
        {
            // Create entity with zero cooldown
            var entity = this.CreateCooldownEntity(0.0f, 0.0f, true, false);

            // Run the system
            this.RunActiveSystem();

            // Verify immediate expiration (no cooldown)
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity), "Zero cooldown should not enable ActiveOnCooldown");
        }

        [Test]
        public void CooldownTimer_HandlesNegativeCooldown()
        {
            // Create entity with negative cooldown
            var entity = this.CreateCooldownEntity(-1.0f, 0.0f, true, false);

            // Run the system
            this.RunActiveSystem();

            // Verify negative cooldown is handled (should not enable)
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity), "Negative cooldown should not enable ActiveOnCooldown");
        }

        [Test]
        public void CooldownTimer_AllowsRetriggeringAfterExpiry()
        {
            // Create entity with cooldown components
            var entity = this.CreateCooldownEntity(3.0f, 0.0f, false, false);

            // Retrigger after cooldown expired
            this.Manager.SetComponentEnabled<Active>(entity, true);

            // Run the system
            this.RunActiveSystem();

            // Verify new cooldown started
            var remaining = this.Manager.GetComponentData<ActiveCooldownRemaining>(entity);
            Assert.AreEqual(3.0f, remaining.Value, 0.001f, "New cooldown should start at full duration");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity), "ActiveOnCooldown should be enabled for new cooldown");
        }

        [Test]
        public void CooldownTimer_CanBeResetEarly()
        {
            // Create entity with cooldown components
            var entity = this.CreateCooldownEntity(3.0f, 2.0f, false, true);

            // Reset by setting remaining to zero
            this.Manager.SetComponentData(entity, new ActiveCooldownRemaining { Value = 0.0f });

            // Run the system
            this.RunActiveSystem();

            // Verify reset
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity), "Cooldown should be reset when remaining set to zero");
        }

        [Test]
        public void CooldownTimer_HandlesMultipleEntities()
        {
            // Create multiple entities with different cooldowns
            var entity1 = this.CreateCooldownEntity(1.0f, 0.3f, false, true);
            var entity2 = this.CreateCooldownEntity(2.0f, 1.2f, false, true);
            var entity3 = this.CreateCooldownEntity(3.0f, 2.8f, false, true);

            // Simulate time passing (0.5 seconds)
            this.World.SetTime(new TimeData(0.5, 0.5f));
            this.RunActiveSystem();

            // Verify each entity processed correctly
            var remaining1 = this.Manager.GetComponentData<ActiveCooldownRemaining>(entity1);
            var remaining2 = this.Manager.GetComponentData<ActiveCooldownRemaining>(entity2);
            var remaining3 = this.Manager.GetComponentData<ActiveCooldownRemaining>(entity3);

            Assert.LessOrEqual(remaining1.Value, 0.0f, "Entity1 should be expired");
            Assert.AreEqual(0.7f, remaining2.Value, 0.001f, "Entity2 should have 0.7s remaining");
            Assert.AreEqual(2.3f, remaining3.Value, 0.001f, "Entity3 should have 2.3s remaining");

            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity1), "Entity1 ActiveOnCooldown should be disabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity2), "Entity2 ActiveOnCooldown should be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity3), "Entity3 ActiveOnCooldown should be enabled");
        }

        [Test]
        public void CooldownTimer_ComponentStateOptimization()
        {
            // Create entity to test that component state only changes at threshold crossings
            var entity = this.CreateCooldownEntity(3.0f, 1.5f, false, true);

            // Small time step that doesn't cross zero threshold
            this.World.SetTime(new TimeData(0.1, 0.1f));
            this.RunActiveSystem();

            // Verify countdown but component still enabled
            var remaining = this.Manager.GetComponentData<ActiveCooldownRemaining>(entity);
            Assert.AreEqual(1.4f, remaining.Value, 0.001f, "Cooldown should count down");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity), "ActiveOnCooldown should remain enabled");

            // Large time step that crosses zero threshold
            this.World.SetTime(new TimeData(1.5, 1.5f));
            this.RunActiveSystem();

            // Verify expiration and component state change
            remaining = this.Manager.GetComponentData<ActiveCooldownRemaining>(entity);
            Assert.LessOrEqual(remaining.Value, 0.0f, "Cooldown should be expired");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveOnCooldown>(entity), "ActiveOnCooldown should be disabled at threshold crossing");
        }

        /// <summary>
        /// Creates a test entity with cooldown components.
        /// </summary>
        /// <param name="cooldownValue">The cooldown value to set.</param>
        /// <param name="remainingValue">The remaining cooldown value to set.</param>
        /// <param name="activeEnabled">Whether Active component should be enabled.</param>
        /// <param name="onCooldownEnabled">Whether ActiveOnCooldown component should be enabled.</param>
        /// <returns>Entity with cooldown components configured.</returns>
        private Entity CreateCooldownEntity(float cooldownValue, float remainingValue, bool activeEnabled, bool onCooldownEnabled)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(ActivePrevious),
                typeof(ActiveCooldown),
                typeof(ActiveCooldownRemaining),
                typeof(ActiveOnCooldown));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new ActiveCooldown { Value = cooldownValue });
            this.Manager.SetComponentData(entity, new ActiveCooldownRemaining { Value = remainingValue });
            this.Manager.SetComponentEnabled<Active>(entity, activeEnabled);
            this.Manager.SetComponentEnabled<ActivePrevious>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, onCooldownEnabled);

            return entity;
        }

        private void RunActiveSystem()
        {
            this.RunSystems(this.activeCooldownSystem, this.activePreviousSystem);
        }
    }
}
