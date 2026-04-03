// <copyright file="InitializeStatsSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests
{
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;
    using UnityEngine.TestTools;

    public class InitializeStatsSystemTests : EssenceTestsFixture
    {
        private SystemHandle system;
        private Entity configSingleton;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<InitializeStatsSystem>();
            this.configSingleton = this.SetupInitializeStatsConfig();
        }

        [Test]
        public void BasicStatCopy_SourceToTarget()
        {
            // Arrange
            var objectId = new ObjectId(1);
            var healthStat = (StatKey)1;
            var manaStat = (StatKey)2;

            // Create source entity with stats
            var sourceEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added),
                CreateStatModifier(manaStat, 50, StatModifyType.Added));

            // Create target entity for initialization
            var targetEntity = this.CreateInitializationEntity(objectId, target: sourceEntity);
            var targetStatBuffer = this.Manager.AddBuffer<Stat>(targetEntity);
            targetStatBuffer.Initialize(); // Initialize dynamic hashmap

            // Configure InitializeStats to copy from Target
            this.AddInitializeStatsData(this.configSingleton, objectId, Target.Target);

            // Act
            this.RunSystem(this.system);

            // Assert
            this.AssertStatsCopied(sourceEntity, targetEntity, "Stats should be copied from source to target");
        }

        [Test]
        public void StatModifiersExclusion_SafetyMechanism()
        {
            // Arrange
            var objectId = new ObjectId(1);
            var healthStat = (StatKey)1;

            var sourceEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            var targetEntity = this.CreateInitializationEntity(objectId, target: sourceEntity);

            // Add Stat buffer AND StatModifiers buffer (this should exclude the entity)
            var targetStatBuffer = this.Manager.AddBuffer<Stat>(targetEntity);
            targetStatBuffer.Initialize(); // Initialize dynamic hashmap
            this.Manager.AddBuffer<StatModifiers>(targetEntity);

            this.AddInitializeStatsData(this.configSingleton, objectId, Target.Target);

            // Get initial state
            var initialTargetStats = this.Manager.GetBuffer<Stat>(targetEntity).ToNativeArray(Unity.Collections.Allocator.Temp);

            // Act
            this.RunSystem(this.system);

            // Assert
            var finalTargetStats = this.Manager.GetBuffer<Stat>(targetEntity);
            Assert.AreEqual(initialTargetStats.Length, finalTargetStats.Length, "Entities with StatModifiers should not be processed for safety");

            initialTargetStats.Dispose();
        }

        [Test]
        public void TargetResolution_AllTargetTypes()
        {
            // Arrange
            var objectId = new ObjectId(1);
            var healthStat = (StatKey)1;

            var ownerEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 80, StatModifyType.Added));
            var sourceEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 90, StatModifyType.Added));
            var targetEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));

            // Test Target.Owner resolution
            var initEntity1 = this.CreateInitializationEntity(objectId, owner: ownerEntity, source: sourceEntity, target: targetEntity);
            var initStatBuffer1 = this.Manager.AddBuffer<Stat>(initEntity1);
            initStatBuffer1.Initialize();
            this.AddInitializeStatsData(this.configSingleton, objectId, Target.Owner);

            // Act
            this.RunSystem(this.system);

            // Assert
            this.AssertStatsCopied(ownerEntity, initEntity1, "Should copy stats from Owner");

            // Test Target.Source resolution
            var objectId2 = new ObjectId(2);
            var initEntity2 = this.CreateInitializationEntity(objectId2, owner: ownerEntity, source: sourceEntity, target: targetEntity);
            var initStatBuffer2 = this.Manager.AddBuffer<Stat>(initEntity2);
            initStatBuffer2.Initialize();
            this.AddInitializeStatsData(this.configSingleton, objectId2, Target.Source);

            this.RunSystem(this.system);
            this.AssertStatsCopied(sourceEntity, initEntity2, "Should copy stats from Source");

            // Test Target.Target resolution
            var objectId3 = new ObjectId(3);
            var initEntity3 = this.CreateInitializationEntity(objectId3, owner: ownerEntity, source: sourceEntity, target: targetEntity);
            var initStatBuffer3 = this.Manager.AddBuffer<Stat>(initEntity3);
            initStatBuffer3.Initialize();
            this.AddInitializeStatsData(this.configSingleton, objectId3, Target.Target);

            this.RunSystem(this.system);
            this.AssertStatsCopied(targetEntity, initEntity3, "Should copy stats from Target");
        }

        [Test]
        public void MissingConfiguration_GracefulHandling()
        {
            // Arrange
            var objectId = new ObjectId(999); // Non-existent configuration
            var healthStat = (StatKey)1;

            var sourceEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            var targetEntity = this.CreateInitializationEntity(objectId, target: sourceEntity);

            // Add stat buffer with initial content
            var targetStats = this.Manager.AddBuffer<Stat>(targetEntity);
            targetStats.Initialize();
            targetStats
            .AsMap()
            .Add(healthStat, new StatValue
            {
                Added = 999,
                Multi = 1f,
            }); // Different initial value

            // Note: No configuration added for objectId 999

            // Act
            this.RunSystem(this.system);

            // Assert - stats should remain unchanged
            var finalStats = this.Manager.GetBuffer<Stat>(targetEntity).AsMap();
            Assert.AreEqual(1, finalStats.Count, "Target should retain its original stats");
            Assert.IsTrue(finalStats.TryGetValue(healthStat, out var statValue), "Health stat should exist");
            Assert.AreEqual(999, statValue.Added, "Original stat value should be preserved when no config exists");
        }

        [Test]
        public void MissingTargets_WarningsLogged()
        {
            // Arrange
            var objectId = new ObjectId(1);

            // Create target entity that points to a non-existent/null target
            var targetEntity = this.CreateInitializationEntity(objectId, target: Entity.Null);
            var targetStatBuffer = this.Manager.AddBuffer<Stat>(targetEntity);
            targetStatBuffer.Initialize();

            this.AddInitializeStatsData(this.configSingleton, objectId, Target.Target);

            // Expect the warning log when target is Entity.Null and doesn't have Stat buffer
            var expectedWarningPattern = $"W | 0    | Test    | Target Entity.Null from {(byte)Target.Target} on {targetEntity.ToFixedString()} does not have a Stat buffer";
            LogAssert.Expect(UnityEngine.LogType.Warning, expectedWarningPattern);

            // Act
            this.RunSystem(this.system);

            // Assert - no stats should be copied when target is missing
            var finalStats = this.Manager.GetBuffer<Stat>(targetEntity).AsMap();
            Assert.AreEqual(0, finalStats.Count, "No stats should be copied when target is missing");
        }

        [Test]
        public void SelfReferencing_IgnoredCorrectly()
        {
            // Arrange
            var objectId = new ObjectId(1);
            var healthStat = (StatKey)1;

            var initEntity = this.CreateInitializationEntity(objectId);
            var targetStats = this.Manager.AddBuffer<Stat>(initEntity);
            targetStats.Initialize();
            targetStats
            .AsMap()
            .Add(healthStat, new StatValue
            {
                Added = 50,
                Multi = 1f,
            }); // Initial value

            // Configure to copy from Self (should be ignored)
            this.AddInitializeStatsData(this.configSingleton, objectId, Target.Self);

            // Act
            this.RunSystem(this.system);

            // Assert - stats should remain unchanged (self-referencing is ignored)
            var finalStats = this.Manager.GetBuffer<Stat>(initEntity).AsMap();
            Assert.AreEqual(1, finalStats.Count, "Stats should remain unchanged");
            Assert.IsTrue(finalStats.TryGetValue(healthStat, out var statValue), "Health stat should exist");
            Assert.AreEqual(50, statValue.Added, "Self-referencing should be ignored per system logic");
        }

        [Test]
        public void MultipleEntities_BulkProcessing()
        {
            // Arrange
            var healthStat = (StatKey)1;
            var manaStat = (StatKey)2;

            var sourceEntity1 = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            var sourceEntity2 = this.CreateStatEntity(CreateStatModifier(manaStat, 200, StatModifyType.Added));

            var objectId1 = new ObjectId(1);
            var objectId2 = new ObjectId(2);

            var targetEntity1 = this.CreateInitializationEntity(objectId1, target: sourceEntity1);
            var targetEntity2 = this.CreateInitializationEntity(objectId2, target: sourceEntity2);

            var targetStatBuffer1 = this.Manager.AddBuffer<Stat>(targetEntity1);
            targetStatBuffer1.Initialize();
            var targetStatBuffer2 = this.Manager.AddBuffer<Stat>(targetEntity2);
            targetStatBuffer2.Initialize();

            this.AddInitializeStatsData(this.configSingleton, objectId1, Target.Target);
            this.AddInitializeStatsData(this.configSingleton, objectId2, Target.Target);

            // Act
            this.RunSystem(this.system);

            // Assert
            this.AssertStatsCopied(sourceEntity1, targetEntity1, "First entity should have stats copied");
            this.AssertStatsCopied(sourceEntity2, targetEntity2, "Second entity should have stats copied");
        }

        [Test]
        public void TargetNone_GracefulHandling()
        {
            // Arrange
            var objectId = new ObjectId(1);
            var healthStat = (StatKey)1;

            var targetEntity = this.CreateInitializationEntity(objectId);
            var targetStats = this.Manager.AddBuffer<Stat>(targetEntity);
            targetStats.Initialize();
            targetStats
            .AsMap()
            .Add(healthStat, new StatValue
            {
                Added = 42,
                Multi = 1f,
            }); // Initial value

            // Configure with Target.None (should be ignored)
            this.AddInitializeStatsData(this.configSingleton, objectId, Target.None);

            // Act
            this.RunSystem(this.system);

            // Assert - stats should remain unchanged
            var finalStats = this.Manager.GetBuffer<Stat>(targetEntity).AsMap();
            Assert.AreEqual(1, finalStats.Count, "Stats should remain unchanged");
            Assert.IsTrue(finalStats.TryGetValue(healthStat, out var statValue), "Health stat should exist");
            Assert.AreEqual(42, statValue.Added, "Target.None should result in no stat copying");
        }

        /// <summary>
        /// Asserts that stat values have been copied from source entity to target entity.
        /// </summary>
        /// <param name="sourceEntity">Entity that stats should be copied from.</param>
        /// <param name="targetEntity">Entity that stats should be copied to.</param>
        /// <param name="message">Optional assertion message.</param>
        private void AssertStatsCopied(Entity sourceEntity, Entity targetEntity, string message = null)
        {
            var sourceStats = this.Manager.GetBuffer<Stat>(sourceEntity).AsMap();
            var targetStats = this.Manager.GetBuffer<Stat>(targetEntity).AsMap();

            Assert.AreEqual(sourceStats.Count, targetStats.Count, $"{message} - Stat buffer counts should match");

            foreach (var kvp in sourceStats)
            {
                var statKey = kvp.Key;
                var sourceValue = kvp.Value;

                Assert.IsTrue(targetStats.TryGetValue(statKey, out var targetValue), $"{message} - Target should contain stat key {statKey}");
                Assert.AreEqual(sourceValue.Added, targetValue.Added, $"{message} - Added values should match for stat {statKey}");
                Assert.AreEqual(sourceValue.Multi, targetValue.Multi, $"{message} - Multi values should match for stat {statKey}");
            }
        }

        private Entity SetupInitializeStatsConfig()
        {
            var entity = this.Manager.CreateSingletonBuffer<InitializeStats>();
            this.Manager.GetBuffer<InitializeStats>(entity).Initialize();
            return entity;
        }

        private void AddInitializeStatsData(Entity singleton, ObjectId objectId, Target source)
        {
            var buffer = this.Manager.GetBuffer<InitializeStats>(singleton).AsMap();
            buffer.Add(objectId, new InitializeStats.Data { Source = source });
        }
    }
}
