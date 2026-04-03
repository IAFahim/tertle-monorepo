// <copyright file="StatIntrinsicIntegrationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests.Integration
{
    using BovineLabs.Essence.Data;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Mathematics;

    public class StatIntrinsicIntegrationTests : EssenceTestsFixture
    {
        private SystemHandle statCalculationSystem;
        private SystemHandle intrinsicValidationSystem;

        public override void Setup()
        {
            base.Setup();
            this.statCalculationSystem = this.World.CreateSystem<StatCalculationSystem>();
            this.intrinsicValidationSystem = this.World.CreateSystem<IntrinsicValidationSystem>();
        }

        [Test]
        public void HealthMaxHealthPattern_StatIncreases_IntrinsicUnchanged()
        {
            // Arrange - Classic RPG pattern: Current Health limited by Max Health
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 100, 0, 999, default(StatKey), maxHealthStat) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(stats: new[] { CreateStatModifier(maxHealthStat, 100, StatModifyType.Added) },
                intrinsics: new[] { CreateIntrinsicDefault(healthIntrinsic, 80) });

            // Add modifier to increase max health
            this.AddStatModifiers(entity, CreateStatModifier(maxHealthStat, 50, StatModifyType.Added));

            // Act - Run both systems in order
            this.statCalculationSystem.Update(this.WorldUnmanaged);
            this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(150, (int)statMap[maxHealthStat].Value, "Max Health should increase to 150");
            Assert.AreEqual(80, intrinsicMap[healthIntrinsic], "Current Health should remain at 80 (not clamped up)");
        }

        [Test]
        public void HealthMaxHealthPattern_StatDecreases_IntrinsicClamped()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 100, 0, 999, default(StatKey), maxHealthStat) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(stats: new[] { CreateStatModifier(maxHealthStat, 100, StatModifyType.Added) },
                intrinsics: new[] { CreateIntrinsicDefault(healthIntrinsic, 100) });

            // Add modifier to decrease max health below current health
            this.AddStatModifiers(entity, CreateStatModifier(maxHealthStat, -30, StatModifyType.Added));

            // Act
            this.statCalculationSystem.Update(this.WorldUnmanaged);
            this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(70, (int)statMap[maxHealthStat].Value, "Max Health should decrease to 70");
            Assert.AreEqual(70, intrinsicMap[healthIntrinsic], "Current Health should be clamped down to 70");
        }

        [Test]
        public void ComplexRPGScenario_MultipleStatsAndIntrinsics_WorksCorrectly()
        {
            // Arrange - Complex RPG scenario with multiple interconnected stats
            IntrinsicKey healthIntrinsic = 1;
            IntrinsicKey manaIntrinsic = 2;
            IntrinsicKey staminaIntrinsic = 3;

            StatKey maxHealthStat = 1;
            StatKey maxManaStat = 2;
            StatKey maxStaminaStat = 3;
            StatKey constitutionStat = 4;
            StatKey intelligenceStat = 5;

            var config = this.CreateTestIntrinsicConfig(new (IntrinsicKey, int, int, int, StatKey, StatKey)[]
            {
                (healthIntrinsic, 50, 1, 999, default, maxHealthStat),
                (manaIntrinsic, 30, 0, 999, default, maxManaStat),
                (staminaIntrinsic, 40, 0, 999, default, maxStaminaStat),
            });

            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(
                stats: new[]
                {
                    CreateStatModifier(maxHealthStat, 80, StatModifyType.Added),
                    CreateStatModifier(maxManaStat, 60, StatModifyType.Added),
                    CreateStatModifier(maxStaminaStat, 70, StatModifyType.Added),
                    CreateStatModifier(constitutionStat, 15, StatModifyType.Added),
                    CreateStatModifier(intelligenceStat, 12, StatModifyType.Added),
                }, intrinsics: new[]
                {
                    CreateIntrinsicDefault(healthIntrinsic, 85), // Above max
                    CreateIntrinsicDefault(manaIntrinsic, 50), // Below max
                    CreateIntrinsicDefault(staminaIntrinsic, 75), // Above max
                });

            // Add modifiers - Constitution affects max health, Intelligence affects max mana
            this.AddStatModifiers(entity, CreateStatModifier(maxHealthStat, 5, StatModifyType.Added), // Constitution bonus
                CreateStatModifier(maxManaStat, 0.5f, StatModifyType.Additive), // Intelligence bonus
                CreateStatModifier(maxStaminaStat, -10, StatModifyType.Added)); // Fatigue penalty

            // Act
            this.statCalculationSystem.Update(this.WorldUnmanaged);
            this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            // Check calculated stats
            Assert.AreEqual(85, (int)statMap[maxHealthStat].Value, "Max Health: 80 + 5 = 85");
            Assert.AreEqual(90, (int)statMap[maxManaStat].Value, "Max Mana: 60 * (1 + 0.5) = 90");
            Assert.AreEqual(60, (int)statMap[maxStaminaStat].Value, "Max Stamina: 70 - 10 = 60");

            // Check clamped intrinsics
            Assert.AreEqual(85, intrinsicMap[healthIntrinsic], "Health should be clamped to new max (85)");
            Assert.AreEqual(50, intrinsicMap[manaIntrinsic], "Mana should remain unchanged (below max)");
            Assert.AreEqual(60, intrinsicMap[staminaIntrinsic], "Stamina should be clamped to reduced max (60)");
        }

        [Test]
        public void DualLimitSystem_MinAndMaxFromDifferentStats_BothApplied()
        {
            // Arrange - Temperature system with both minimum and maximum limits from different stats
            IntrinsicKey temperatureIntrinsic = 1;
            StatKey environmentMinStat = 1;
            StatKey equipmentMaxStat = 2;

            var config = this.CreateTestIntrinsicConfig(new[] { (temperatureIntrinsic, 20, -999, 999, environmentMinStat, equipmentMaxStat) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(stats: new[]
            {
                CreateStatModifier(environmentMinStat, 10, StatModifyType.Added), // Minimum temperature
                CreateStatModifier(equipmentMaxStat, 50, StatModifyType.Added), // Maximum from equipment
            }, intrinsics: new[] { CreateIntrinsicDefault(temperatureIntrinsic, 5) }); // Below min

            // Modify environment to be colder (higher minimum)
            this.AddStatModifiers(entity, CreateStatModifier(environmentMinStat, 15, StatModifyType.Added));

            // Act
            this.statCalculationSystem.Update(this.WorldUnmanaged);
            this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(25, (int)statMap[environmentMinStat].Value, "Environment min should be 25");
            Assert.AreEqual(50, (int)statMap[equipmentMaxStat].Value, "Equipment max should remain 50");
            Assert.AreEqual(25, intrinsicMap[temperatureIntrinsic], "Temperature should be raised to new minimum");
        }

        [Test]
        public void EventPropagation_ConditionDirtyFlags_SetCorrectlyThroughPipeline()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(stats: new[] { CreateStatModifier(maxHealthStat, 100, StatModifyType.Added) },
                intrinsics: new[] { CreateIntrinsicDefault(healthIntrinsic, 120) }); // Above max

            // Clear dirty flags
            this.Manager.SetComponentEnabled<StatConditionDirty>(entity, false);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(entity, false);

            // Act
            this.statCalculationSystem.Update(this.WorldUnmanaged);
            this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatConditionDirty>(entity), "StatConditionDirty should be set by stat calculation");
            Assert.IsTrue(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(entity), "IntrinsicConditionDirty should be set by intrinsic validation");
        }

        [Test]
        public void PerformanceScenario_ManyEntitiesWithInterconnectedStats_ProcessesEfficiently()
        {
            // Arrange - Performance test with many entities
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;
            StatKey constitutionStat = 2;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });
            this.SetupIntrinsicConfig(config);

            var entityCount = 100;
            var entities = new Entity[entityCount];

            for (int i = 0; i < entityCount; i++)
            {
                entities[i] = this.CreateCombinedEntity(
                    stats: new[]
                    {
                        CreateStatModifier(maxHealthStat, 100, StatModifyType.Added),
                        CreateStatModifier(constitutionStat, (short)(10 + (i % 20)), StatModifyType.Added),
                    }, intrinsics: new[] { CreateIntrinsicDefault(healthIntrinsic, 80 + (i % 40)) });

                // Add constitution modifier to max health
                this.AddStatModifiers(entities[i], CreateStatModifier(maxHealthStat, 2, StatModifyType.Added));
            }

            // Act - Measure performance implicitly through test execution time
            this.statCalculationSystem.Update(this.WorldUnmanaged);
            this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert - Verify correctness for a sample of entities
            for (int i = 0; i < 10; i++)
            {
                var statBuffer = this.Manager.GetBuffer<Stat>(entities[i]);
                var statMap = statBuffer.AsMap();
                var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entities[i]);
                var intrinsicMap = intrinsicBuffer.AsMap();

                Assert.AreEqual(102, (int)statMap[maxHealthStat].Value, $"Entity {i} max health should be 102");

                var expectedHealth = math.min(80 + (i % 40), 102);
                Assert.AreEqual(expectedHealth, intrinsicMap[healthIntrinsic], $"Entity {i} health should be properly clamped");
            }
        }

        [Test]
        public void SystemOrdering_IntrinsicValidationAfterStatCalculation_WorksCorrectly()
        {
            // Arrange - Test that the systems work correctly when run in the right order
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(stats: new[] { CreateStatModifier(maxHealthStat, 100, StatModifyType.Added) },
                intrinsics: new[] { CreateIntrinsicDefault(healthIntrinsic, 80) });

            // Add modifier that will reduce max health below current health
            this.AddStatModifiers(entity, CreateStatModifier(maxHealthStat, -50, StatModifyType.Added));

            // Act - Run systems in correct order
            this.statCalculationSystem.Update(this.WorldUnmanaged);
            this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(50, (int)statMap[maxHealthStat].Value, "Max Health should be calculated first");
            Assert.AreEqual(50, intrinsicMap[healthIntrinsic], "Health should be clamped using updated stat");
        }

        [Test]
        public void AllNegativeStats_WithIntrinsics_HandlesCorrectly()
        {
            // Arrange - Test with all negative stat values
            IntrinsicKey temperatureIntrinsic = 1;
            IntrinsicKey depthIntrinsic = 2;
            StatKey minTempStat = 1;
            StatKey maxDepthStat = 2;

            var config = this.CreateTestIntrinsicConfig(new (IntrinsicKey, int, int, int, StatKey, StatKey)[]
            {
                (temperatureIntrinsic, -10, -999, 999, minTempStat, default), (depthIntrinsic, -5, -999, 999, default, maxDepthStat),
            });

            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(stats: new[]
            {
                CreateStatModifier(minTempStat, -30, StatModifyType.Added), // Min temp: -30
                CreateStatModifier(maxDepthStat, -50, StatModifyType.Added), // Max depth: -50
            }, intrinsics: new[]
            {
                CreateIntrinsicDefault(temperatureIntrinsic, -40), // Below min
                CreateIntrinsicDefault(depthIntrinsic, -20), // Above max
            });

            // Act
            this.statCalculationSystem.Update(this.WorldUnmanaged);
            this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(-30, (int)statMap[minTempStat].Value, "Min temp stat should be -30");
            Assert.AreEqual(-50, (int)statMap[maxDepthStat].Value, "Max depth stat should be -50");
            Assert.AreEqual(-30, intrinsicMap[temperatureIntrinsic], "Temperature should be clamped to min (-30)");
            Assert.AreEqual(-50, intrinsicMap[depthIntrinsic], "Depth should be clamped to max (-50)");
        }

        [Test]
        public void MixedPositiveNegativeChanges_BulkEntities_ProcessesCorrectly()
        {
            // Arrange - Test bulk processing with mixed positive/negative changes
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });
            this.SetupIntrinsicConfig(config);

            var entityCount = 20;
            var entities = new Entity[entityCount];

            for (int i = 0; i < entityCount; i++)
            {
                entities[i] = this.CreateCombinedEntity(stats: new[] { CreateStatModifier(maxHealthStat, 100, StatModifyType.Added) },
                    intrinsics: new[] { CreateIntrinsicDefault(healthIntrinsic, 100) });

                // Alternate between positive and negative changes
                var change = (i % 2 == 0) ? 50 : -30; // +50 or -30
                this.AddStatModifiers(entities[i], CreateStatModifier(maxHealthStat, change, StatModifyType.Added));
            }

            // Act
            this.statCalculationSystem.Update(this.WorldUnmanaged);
            this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            for (int i = 0; i < entityCount; i++)
            {
                var statBuffer = this.Manager.GetBuffer<Stat>(entities[i]);
                var statMap = statBuffer.AsMap();
                var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entities[i]);
                var intrinsicMap = intrinsicBuffer.AsMap();

                if (i % 2 == 0)
                {
                    // Positive change: 100 + 50 = 150, health remains 100 (below max)
                    Assert.AreEqual(150, (int)statMap[maxHealthStat].Value, $"Entity {i} max health should be 150");
                    Assert.AreEqual(100, intrinsicMap[healthIntrinsic], $"Entity {i} health should remain 100");
                }
                else
                {
                    // Negative change: 100 - 30 = 70, health clamped to 70
                    Assert.AreEqual(70, (int)statMap[maxHealthStat].Value, $"Entity {i} max health should be 70");
                    Assert.AreEqual(70, intrinsicMap[healthIntrinsic], $"Entity {i} health should be clamped to 70");
                }
            }
        }

        [Test]
        public void ChainedModifications_MultipleUpdateCycles_MaintainsConsistency()
        {
            // Arrange - Test multiple update cycles to ensure consistency
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(stats: new[] { CreateStatModifier(maxHealthStat, 100, StatModifyType.Added) },
                intrinsics: new[] { CreateIntrinsicDefault(healthIntrinsic, 80) });

            // Act - Multiple modification cycles
            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Add modifier
                this.AddStatModifiers(entity, CreateStatModifier(maxHealthStat, -20, StatModifyType.Added));

                this.statCalculationSystem.Update(this.WorldUnmanaged);
                this.intrinsicValidationSystem.Update(this.WorldUnmanaged);
                this.Manager.CompleteAllTrackedJobs();
            }

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            // Final: 100 - 20 - 20 - 20 = 40
            Assert.AreEqual(40, (int)statMap[maxHealthStat].Value, "Max health should be 40 after 3 cycles");
            Assert.AreEqual(40, intrinsicMap[healthIntrinsic], "Health should be clamped to final max (40)");
        }
    }
}
