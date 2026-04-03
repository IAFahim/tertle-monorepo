// <copyright file="ActiveDisableOnDestroySystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Actives
{
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Data.Active;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Tests for <see cref="ActiveDisableOnDestroySystem"/>, verifying disable state management
    /// when entities are marked for destruction.
    /// </summary>
    public class ActiveDisableOnDestroySystemTests : ReactionTestFixture
    {
        private SystemHandle activeDisableOnDestroySystem;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();
            this.activeDisableOnDestroySystem = this.World.CreateSystem<ActiveDisableOnDestroySystem>();
        }

        [Test]
        public void DisableOnDestroy_DisablesActiveComponent()
        {
            // Create entity with Active enabled and marked for destruction
            var entity = this.CreateActiveDestroyEntity(true, true);

            // Verify initial state
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should initially be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should be enabled");

            // Run the system
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify Active component is disabled
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Active should be disabled when entity is marked for destruction");
            Assert.IsTrue(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should remain enabled");
        }

        [Test]
        public void DisableOnDestroy_IgnoresEntitiesNotMarkedForDestruction()
        {
            // Create entity with Active enabled but not marked for destruction
            var entity = this.CreateActiveDestroyEntity(true, false);

            // Verify initial state
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should initially be enabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should be disabled");

            // Run the system
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify Active component remains enabled (entity not marked for destruction)
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain enabled when entity is not marked for destruction");
            Assert.IsFalse(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should remain disabled");
        }

        [Test]
        public void DisableOnDestroy_ProcessesMultipleEntities()
        {
            // Create multiple entities with Active enabled and marked for destruction
            var entity1 = this.CreateActiveDestroyEntity(true, true);
            var entity2 = this.CreateActiveDestroyEntity(true, true);
            var entity3 = this.CreateActiveDestroyEntity(true, true);

            // Create one entity not marked for destruction
            var entityNotDestroyed = this.CreateActiveDestroyEntity(true, false);

            // Verify initial states
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity1), "Entity1 Active should initially be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity2), "Entity2 Active should initially be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity3), "Entity3 Active should initially be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entityNotDestroyed), "EntityNotDestroyed Active should initially be enabled");

            // Run the system
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify Active components disabled only for entities marked for destruction
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity1), "Entity1 Active should be disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity2), "Entity2 Active should be disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity3), "Entity3 Active should be disabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entityNotDestroyed), "EntityNotDestroyed Active should remain enabled");
        }

        [Test]
        public void DisableOnDestroy_HandlesAlreadyDisabledActiveComponent()
        {
            // Create entity with Active disabled and marked for destruction
            var entity = this.CreateActiveDestroyEntity(false, true);

            // Verify initial state
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Active should initially be disabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should be enabled");

            // Run the system
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify Active component remains disabled (no change expected)
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain disabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should remain enabled");
        }

        [Test]
        public void DisableOnDestroy_HandlesEntitiesWithoutDestroyEntityComponent()
        {
            // Create entity with only Active component (no DestroyEntity)
            var archetype = this.Manager.CreateArchetype(typeof(Active));
            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentEnabled<Active>(entity, true);

            // Verify initial state
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should initially be enabled");

            // Run the system
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify Active component remains enabled (entity doesn't have DestroyEntity)
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain enabled when entity has no DestroyEntity component");
        }

        [Test]
        public void DisableOnDestroy_HandlesEntitiesWithoutActiveComponent()
        {
            // Create entity with only DestroyEntity component (no Active)
            var archetype = this.Manager.CreateArchetype(typeof(DestroyEntity));
            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentEnabled<DestroyEntity>(entity, true);

            // Verify initial state
            Assert.IsTrue(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should be enabled");

            // Run the system (should not crash or cause issues)
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify DestroyEntity remains enabled and system handled gracefully
            Assert.IsTrue(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should remain enabled");
        }

        [Test]
        public void DisableOnDestroy_HandlesEmptyWorld()
        {
            // Ensure no entities exist
            Assert.AreEqual(0, new EntityQueryBuilder(Allocator.Temp)
                .WithAll<DestroyEntity>()
                .Build(this.Manager).CalculateEntityCount(),
                "World should be empty of DestroyEntity components");

            // Run system on empty world
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify system handles empty world gracefully
            Assert.IsTrue(true, "ActiveDisableOnDestroySystem should handle empty world");
        }

        [Test]
        public void DisableOnDestroy_RepeatedSystemUpdates()
        {
            // Create entity with Active enabled and marked for destruction
            var entity = this.CreateActiveDestroyEntity(true, true);

            // Run the system multiple times
            for (int i = 0; i < 3; i++)
            {
                this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
                this.Manager.CompleteAllTrackedJobs();
            }

            // Verify Active component remains disabled after multiple updates
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain disabled after multiple system updates");
        }

        [Test]
        public void DisableOnDestroy_StateTransitionFromEnabledToDisabled()
        {
            // Create entity with Active enabled and not initially marked for destruction
            var entity = this.CreateActiveDestroyEntity(true, false);

            // Run system first time (should not affect Active)
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify Active remains enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain enabled before destruction marking");

            // Now mark entity for destruction
            this.Manager.SetComponentEnabled<DestroyEntity>(entity, true);

            // Run system again
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify Active is now disabled
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Active should be disabled after entity marked for destruction");
        }

        /// <summary>
        /// Creates a test entity with Active and DestroyEntity components.
        /// </summary>
        /// <param name="activeEnabled">Whether Active component should be enabled.</param>
        /// <param name="destroyEnabled">Whether DestroyEntity component should be enabled.</param>
        /// <returns>Entity configured for destruction testing.</returns>
        private Entity CreateActiveDestroyEntity(bool activeEnabled, bool destroyEnabled)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(DestroyEntity));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentEnabled<Active>(entity, activeEnabled);
            this.Manager.SetComponentEnabled<DestroyEntity>(entity, destroyEnabled);

            return entity;
        }
    }
}
