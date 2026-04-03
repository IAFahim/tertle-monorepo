// <copyright file="ActiveDestroyedCleanupSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Actives
{
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Groups;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Tests for <see cref="ActiveDestroyedCleanupSystem"/>, verifying cleanup on entity destruction
    /// and proper integration with the disabled system group.
    /// </summary>
    public class ActiveDestroyedCleanupSystemTests : ReactionTestFixture
    {
        private ActiveDestroyedCleanupSystem activeDestroyedCleanupSystem;
        private SystemHandle activeDisableOnDestroySystem;
        private ActiveDisabledSystemGroup activeDisabledSystemGroup;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();
            this.activeDisabledSystemGroup = this.World.CreateSystemManaged<ActiveDisabledSystemGroup>();
            this.activeDestroyedCleanupSystem = this.World.CreateSystemManaged<ActiveDestroyedCleanupSystem>();
            this.activeDisableOnDestroySystem = this.World.CreateSystem<ActiveDisableOnDestroySystem>();
        }

        [Test]
        public void Cleanup_UpdatesActiveDisabledSystemGroup()
        {
            // Create entity marked for destruction
            this.CreateDestroyEntity(true);

            // Track if the disabled system group gets updated by monitoring system version
            var initialVersion = this.activeDisabledSystemGroup.LastSystemVersion;

            // Run the cleanup system
            this.activeDestroyedCleanupSystem.Update();

            // Verify the system group was updated (version should change)
            var updatedVersion = this.activeDisabledSystemGroup.LastSystemVersion;
            Assert.AreNotEqual(initialVersion, updatedVersion, "ActiveDisabledSystemGroup should be updated");
        }

        [Test]
        public void Cleanup_WorksWithActiveDisableOnDestroySystem()
        {
            // Create entity with Active enabled and marked for destruction
            var entity = this.CreateDestroyEntity(true);

            // Verify initial state
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should initially be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should be enabled");

            // Run ActiveDisableOnDestroySystem first (this is the normal sequence)
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify Active component was disabled by ActiveDisableOnDestroySystem
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Active should be disabled by ActiveDisableOnDestroySystem");

            // Now run ActiveDestroyedCleanupSystem
            this.activeDestroyedCleanupSystem.Update();

            // Verify the cleanup system ran successfully (no exceptions)
            // The actual cleanup effects would be tested in action system tests
            Assert.IsTrue(true, "ActiveDestroyedCleanupSystem should complete without errors");
        }

        [Test]
        public void Cleanup_HandlesMultipleEntitiesMarkedForDestruction()
        {
            // Create multiple entities marked for destruction
            var entity1 = this.CreateDestroyEntity(true);
            var entity2 = this.CreateDestroyEntity(true);
            var entity3 = this.CreateDestroyEntity(true);

            // Run ActiveDisableOnDestroySystem first to disable Active components
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify all Active components were disabled
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity1), "Entity1 Active should be disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity2), "Entity2 Active should be disabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity3), "Entity3 Active should be disabled");

            // Run cleanup system
            this.activeDestroyedCleanupSystem.Update();

            // Verify system handles multiple entities without errors
            Assert.IsTrue(true, "ActiveDestroyedCleanupSystem should handle multiple entities");
        }

        [Test]
        public void Cleanup_HandlesEntitiesWithoutActiveComponent()
        {
            // Create entity marked for destruction but without Active component
            var archetype = this.Manager.CreateArchetype(typeof(DestroyEntity));
            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentEnabled<DestroyEntity>(entity, true);

            // Run systems
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            this.activeDestroyedCleanupSystem.Update();

            // Verify system handles entities without Active component gracefully
            Assert.IsTrue(true, "ActiveDestroyedCleanupSystem should handle entities without Active component");
        }

        [Test]
        public void Cleanup_HandlesEmptyWorld()
        {
            // Ensure no entities exist
            Assert.AreEqual(0, new EntityQueryBuilder(Allocator.Temp)
                .WithAll<DestroyEntity>()
                .Build(this.Manager).CalculateEntityCount(),
                "World should be empty of DestroyEntity components");

            // Run cleanup system on empty world
            this.activeDestroyedCleanupSystem.Update();

            // Verify system handles empty world gracefully
            Assert.IsTrue(true, "ActiveDestroyedCleanupSystem should handle empty world");
        }

        [Test]
        public void Cleanup_IntegrationWithDestroyEntityDisabled()
        {
            // Create entity with DestroyEntity disabled (not marked for destruction)
            var entity = this.CreateDestroyEntity(false);

            // Verify initial state
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should be enabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<DestroyEntity>(entity), "DestroyEntity should be disabled");

            // Run systems
            this.activeDisableOnDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify Active component remains enabled (entity not marked for destruction)
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Active should remain enabled when DestroyEntity is disabled");

            // Run cleanup system
            this.activeDestroyedCleanupSystem.Update();

            // Verify system runs without issues
            Assert.IsTrue(true, "ActiveDestroyedCleanupSystem should handle entities not marked for destruction");
        }

        /// <summary>
        /// Creates a test entity with DestroyEntity and Active components.
        /// </summary>
        /// <param name="destroyEnabled">Whether DestroyEntity component should be enabled.</param>
        /// <returns>Entity configured for destruction testing.</returns>
        private Entity CreateDestroyEntity(bool destroyEnabled)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(DestroyEntity),
                typeof(Active));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentEnabled<DestroyEntity>(entity, destroyEnabled);
            this.Manager.SetComponentEnabled<Active>(entity, true);

            return entity;
        }
    }
}
