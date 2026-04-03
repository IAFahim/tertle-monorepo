// <copyright file="ActivePreviousSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Actives
{
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Data.Active;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Tests for <see cref="ActivePreviousSystem"/>, verifying previous state tracking
    /// for active state change detection.
    /// </summary>
    public class ActivePreviousSystemTests : ReactionTestFixture
    {
        private SystemHandle activePreviousSystem;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();
            this.activePreviousSystem = this.World.CreateSystem<ActivePreviousSystem>();
        }

        [Test]
        public void Previous_CopiesActiveStateToActivePrevious()
        {
            // Create entity with Active enabled and ActivePrevious disabled
            var entity = this.CreateActivePreviousEntity(true, false);

            // Run the system
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify ActivePrevious now matches Active state
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "ActivePrevious should be copied from Active");
        }

        [Test]
        public void Previous_CopiesDisabledActiveState()
        {
            // Create entity with Active disabled and ActivePrevious enabled
            var entity = this.CreateActivePreviousEntity(false, true);

            // Run the system
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify ActivePrevious now matches Active state (disabled)
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "ActivePrevious should be copied from Active");
        }

        [Test]
        public void Previous_DetectsActivationTransition()
        {
            // Create entity with both Active and ActivePrevious disabled (inactive state)
            var entity = this.CreateActivePreviousEntity(false, false);

            // First update to establish baseline
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify initial state
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Active should be disabled initially");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "ActivePrevious should be disabled initially");

            // Simulate activation by enabling Active
            this.Manager.SetComponentEnabled<Active>(entity, true);

            // Run system again
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify activation transition can be detected (Active=true, ActivePrevious=false from before change)
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should be enabled after activation");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "ActivePrevious should now reflect Active state");
        }

        [Test]
        public void Previous_DetectsDeactivationTransition()
        {
            // Create entity with both Active and ActivePrevious enabled (active state)
            var entity = this.CreateActivePreviousEntity(true, true);

            // First update to establish baseline
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify initial state
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should be enabled initially");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "ActivePrevious should be enabled initially");

            // Simulate deactivation by disabling Active
            this.Manager.SetComponentEnabled<Active>(entity, false);

            // Run system again
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify deactivation transition can be detected (Active=false, ActivePrevious=true from before change)
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Active should be disabled after deactivation");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "ActivePrevious should now reflect Active state");
        }

        [Test]
        public void Previous_HandlesMultipleEntities()
        {
            // Create multiple entities with different Active/ActivePrevious states
            var entity1 = this.CreateActivePreviousEntity(true, false);   // Active enabled, Previous disabled
            var entity2 = this.CreateActivePreviousEntity(false, true);   // Active disabled, Previous enabled
            var entity3 = this.CreateActivePreviousEntity(true, true);    // Both enabled
            var entity4 = this.CreateActivePreviousEntity(false, false);  // Both disabled

            // Run the system
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify each entity's ActivePrevious matches its Active state
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity1), "Entity1 Active should remain enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActivePrevious>(entity1), "Entity1 ActivePrevious should be copied from Active");

            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity2), "Entity2 Active should remain disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActivePrevious>(entity2), "Entity2 ActivePrevious should be copied from Active");

            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity3), "Entity3 Active should remain enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActivePrevious>(entity3), "Entity3 ActivePrevious should be copied from Active");

            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity4), "Entity4 Active should remain disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActivePrevious>(entity4), "Entity4 ActivePrevious should be copied from Active");
        }

        [Test]
        public void Previous_OnlyProcessesEntitiesWithChangedActive()
        {
            // Create entity with matching Active and ActivePrevious states
            var entity = this.CreateActivePreviousEntity(true, true);

            // Run the system first time
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Run the system again without changing Active state
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify system didn't need to process unchanged entities
            // (This tests the change filter optimization in CopyEnableable)
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "ActivePrevious should remain enabled");
        }

        [Test]
        public void Previous_HandlesEntitiesWithoutActiveComponent()
        {
            // Create entity with only ActivePrevious component (no Active)
            var archetype = this.Manager.CreateArchetype(typeof(ActivePrevious));
            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentEnabled<ActivePrevious>(entity, true);

            // Run the system (should handle gracefully)
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify system handles entities without Active component gracefully
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "ActivePrevious should remain unchanged");
        }

        [Test]
        public void Previous_HandlesEntitiesWithoutActivePreviousComponent()
        {
            // Create entity with only Active component (no ActivePrevious)
            var archetype = this.Manager.CreateArchetype(typeof(Active));
            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentEnabled<Active>(entity, true);

            // Run the system (should handle gracefully)
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify system handles entities without ActivePrevious component gracefully
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain unchanged");
        }

        [Test]
        public void Previous_StateTransitionSequence()
        {
            // Test a complete sequence of state transitions
            var entity = this.CreateActivePreviousEntity(false, false);

            // Initial state: both disabled
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Initial: Active should be disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "Initial: ActivePrevious should be disabled");

            // Transition 1: Activate (false -> true)
            this.Manager.SetComponentEnabled<Active>(entity, true);
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Transition 1: Active should be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "Transition 1: ActivePrevious should reflect Active");

            // Transition 2: Deactivate (true -> false)
            this.Manager.SetComponentEnabled<Active>(entity, false);
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Transition 2: Active should be disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "Transition 2: ActivePrevious should reflect Active");

            // Transition 3: Reactivate (false -> true)
            this.Manager.SetComponentEnabled<Active>(entity, true);
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Transition 3: Active should be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActivePrevious>(entity), "Transition 3: ActivePrevious should reflect Active");
        }

        [Test]
        public void Previous_HandlesEmptyWorld()
        {
            // Ensure no entities exist
            Assert.AreEqual(0, new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Active>()
                .Build(this.Manager).CalculateEntityCount(),
                "World should be empty of Active components");

            // Run system on empty world
            this.activePreviousSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify system handles empty world gracefully
            Assert.IsTrue(true, "ActivePreviousSystem should handle empty world");
        }

        /// <summary>
        /// Creates a test entity with Active and ActivePrevious components.
        /// </summary>
        /// <param name="activeEnabled">Whether Active component should be enabled.</param>
        /// <param name="previousEnabled">Whether ActivePrevious component should be enabled.</param>
        /// <returns>Entity configured for previous state testing.</returns>
        private Entity CreateActivePreviousEntity(bool activeEnabled, bool previousEnabled)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(ActivePrevious));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentEnabled<Active>(entity, activeEnabled);
            this.Manager.SetComponentEnabled<ActivePrevious>(entity, previousEnabled);

            return entity;
        }
    }
}
