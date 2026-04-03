// <copyright file="StatChangedResetSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests
{
    using BovineLabs.Essence.Data;
    using NUnit.Framework;
    using Unity.Entities;

    public class StatChangedResetSystemTests : EssenceTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<StatChangedResetSystem>();
        }

        [Test]
        public void BasicReset_DisablesStatChangedFlags()
        {
            // Arrange
            var healthStat = (StatKey)1;
            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));

            // Verify StatChanged is initially enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(entity), "StatChanged should be enabled initially");

            // Act
            this.RunSystem(this.system);

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entity), "StatChanged should be disabled after reset");
        }

        [Test]
        public void MultipleEntities_AllFlagsReset()
        {
            // Arrange
            var healthStat = (StatKey)1;
            var manaStat = (StatKey)2;

            var entity1 = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            var entity2 = this.CreateStatEntity(CreateStatModifier(manaStat, 50, StatModifyType.Added));
            var entity3 = this.CreateStatEntity(CreateStatModifier(healthStat, 75, StatModifyType.Added));

            // Verify all StatChanged are initially enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(entity1), "Entity1 StatChanged should be enabled initially");
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(entity2), "Entity2 StatChanged should be enabled initially");
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(entity3), "Entity3 StatChanged should be enabled initially");

            // Act
            this.RunSystem(this.system);

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entity1), "Entity1 StatChanged should be disabled after reset");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entity2), "Entity2 StatChanged should be disabled after reset");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entity3), "Entity3 StatChanged should be disabled after reset");
        }

        [Test]
        public void EmptyQuery_NoErrors()
        {
            // Arrange - no entities with StatChanged components

            // Act & Assert - should not throw any exceptions
            Assert.DoesNotThrow(() => this.RunSystem(this.system), "System should handle empty query gracefully");
        }

        [Test]
        public void MixedStates_OnlyEnabledFlagsReset()
        {
            // Arrange
            var healthStat = (StatKey)1;
            var manaStat = (StatKey)2;

            var entityEnabled = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            var entityDisabled = this.CreateStatEntity(CreateStatModifier(manaStat, 50, StatModifyType.Added));

            // Disable StatChanged on one entity
            this.Manager.SetComponentEnabled<StatChanged>(entityDisabled, false);

            // Verify initial states
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(entityEnabled), "Enabled entity should have StatChanged enabled");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entityDisabled), "Disabled entity should have StatChanged disabled");

            // Act
            this.RunSystem(this.system);

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entityEnabled), "Enabled entity should be disabled after reset");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entityDisabled), "Disabled entity should remain disabled");
        }

        [Test]
        public void ResetCycle_EnableResetRepeat()
        {
            // Arrange
            var healthStat = (StatKey)1;
            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));

            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Verify enabled state
                Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(entity), $"Cycle {cycle}: StatChanged should be enabled");

                // Act - reset flags
                this.RunSystem(this.system);

                // Assert - flags disabled
                Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entity), $"Cycle {cycle}: StatChanged should be disabled after reset");

                // Re-enable for next cycle (simulating stat modification)
                this.Manager.SetComponentEnabled<StatChanged>(entity, true);
            }
        }

        [Test]
        public void ParallelProcessing_PerformanceOptimization()
        {
            // Arrange - create many entities to test parallel processing
            var healthStat = (StatKey)1;
            var entities = new Entity[100];

            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = this.CreateStatEntity(CreateStatModifier(healthStat, 100 + i, StatModifyType.Added));
            }

            // Verify all are enabled
            for (int i = 0; i < entities.Length; i++)
            {
                Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(entities[i]), $"Entity {i} should have StatChanged enabled");
            }

            // Act
            this.RunSystem(this.system);

            // Assert - all should be disabled
            for (int i = 0; i < entities.Length; i++)
            {
                Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entities[i]), $"Entity {i} should have StatChanged disabled");
            }
        }

        [Test]
        public void EntityWithoutStatModifiers_StillProcessed()
        {
            // Arrange - create entity with StatChanged but no StatModifiers
            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponentData(entity, new StatChanged());
            this.Manager.SetComponentEnabled<StatChanged>(entity, true);

            // Verify StatChanged is enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(entity), "StatChanged should be enabled initially");

            // Act
            this.RunSystem(this.system);

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entity), "StatChanged should be disabled even without StatModifiers");
        }

        [Test]
        public void SystemGroupOrdering_RunsAfterOtherSystems()
        {
            // Arrange
            var healthStat = (StatKey)1;
            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));

            // Simulate other systems setting StatChanged flags
            this.Manager.SetComponentEnabled<StatChanged>(entity, true);

            // Act - Run the reset system
            this.RunSystem(this.system);

            // Assert - Flag should be reset
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatChanged>(entity),
                "Reset system should clear flags set by previous systems in the group");
        }
    }
}
