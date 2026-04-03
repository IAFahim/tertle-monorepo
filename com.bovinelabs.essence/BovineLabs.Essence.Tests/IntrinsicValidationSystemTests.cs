// <copyright file="IntrinsicValidationSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests
{
    using BovineLabs.Essence.Data;
    using NUnit.Framework;
    using Unity.Entities;

    public class IntrinsicValidationSystemTests : EssenceTestsFixture
    {
        private SystemHandle validationSystem;

        public override void Setup()
        {
            base.Setup();
            this.validationSystem = this.World.CreateSystem<IntrinsicValidationSystem>();
        }

        [Test]
        public void StatBasedMax_IntrinsicAboveStatValue_ClampsToStatValue()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });

            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(maxHealthStat, 80, StatModifyType.Added) },
                intrinsics: new (IntrinsicKey, int)[] { (healthIntrinsic, 100) });

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(80, intrinsicMap[healthIntrinsic], "Health should be clamped to MaxHealth stat value of 80");
        }

        [Test]
        public void StatBasedMin_IntrinsicBelowStatValue_ClampsToStatValue()
        {
            // Arrange
            IntrinsicKey temperatureIntrinsic = 1;
            StatKey minTempStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (temperatureIntrinsic, 20, -999, 100, minTempStat, default(StatKey)) });

            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(minTempStat, 15, StatModifyType.Added) },
                intrinsics: new (IntrinsicKey, int)[] { (temperatureIntrinsic, 10) });

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(15, intrinsicMap[temperatureIntrinsic], "Temperature should be clamped to MinTemp stat value of 15");
        }

        [Test]
        public void MixedValidation_StatMinStaticMax_ClampsCorrectly()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            StatKey minHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 100, minHealthStat, default(StatKey)) });

            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(minHealthStat, 30, StatModifyType.Added) },
                intrinsics: new (IntrinsicKey, int)[] { (healthIntrinsic, 25) });

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(30, intrinsicMap[healthIntrinsic], "Health should be raised to stat-based minimum of 30");
        }

        [Test]
        public void MixedValidation_StaticMinStatMax_ClampsCorrectly()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 10, 999, default(StatKey), maxHealthStat) });

            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(maxHealthStat, 80, StatModifyType.Added) },
                intrinsics: new (IntrinsicKey, int)[] { (healthIntrinsic, 90) });

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(80, intrinsicMap[healthIntrinsic], "Health should be clamped to stat-based maximum of 80");
        }

        [Test]
        public void MultipleIntrinsics_OneStatAffectsMultiple_ValidatesAllCorrectly()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            IntrinsicKey shieldIntrinsic = 2;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[]
            {
                (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat), (shieldIntrinsic, 25, 0, 999, default(StatKey), maxHealthStat),
            });

            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(maxHealthStat, 60, StatModifyType.Added) },
                intrinsics: new[] { (healthIntrinsic, 80), (shieldIntrinsic, 70) });

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(60, intrinsicMap[healthIntrinsic], "Health should be clamped to MaxHealth stat");
            Assert.AreEqual(60, intrinsicMap[shieldIntrinsic], "Shield should be clamped to MaxHealth stat");
        }

        [Test]
        public void ConditionDirtyFlag_NoChanges_RemainsUnchanged()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 100, default(StatKey), default(StatKey)) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthIntrinsic, 75)); // Within bounds
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(entity, false);

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(entity),
                "IntrinsicConditionDirty should remain false when no changes occur");
        }

        [Test]
        public void ChangeDetection_EntityWithoutStatChanged_NotProcessed()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });

            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(maxHealthStat, 60, StatModifyType.Added) },
                intrinsics: new (IntrinsicKey, int)[] { (healthIntrinsic, 100) });

            // Disable StatChanged to prevent processing
            this.Manager.SetComponentEnabled<StatChanged>(entity, false);

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(100, intrinsicMap[healthIntrinsic], "Value should remain unchanged when StatChanged is false");
        }

        [Test]
        public void BulkProcessing_MultipleEntities_ProcessesAllCorrectly()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });

            this.SetupIntrinsicConfig(config);

            var entities = new Entity[5];
            for (int i = 0; i < entities.Length; i++)
            {
                var maxHealth = (i + 1) * 20; // 20, 40, 60, 80, 100
                var currentHealth = maxHealth + 10; // Exceeds max by 10

                entities[i] = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(maxHealthStat, (short)maxHealth, StatModifyType.Added) },
                    intrinsics: new (IntrinsicKey, int)[] { (healthIntrinsic, currentHealth) });
            }

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            for (int i = 0; i < entities.Length; i++)
            {
                var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entities[i]);
                var intrinsicMap = intrinsicBuffer.AsMap();
                var expected = (i + 1) * 20;

                Assert.AreEqual(expected, intrinsicMap[healthIntrinsic], $"Entity {i} health should be clamped to {expected}");
            }
        }

        [Test]
        public void FloatStatValues_RoundedDown_ClampsProperly()
        {
            // Arrange
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });

            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[] // 75 * 0.9 = 67.5, should floor to 67
            {
                CreateStatModifier(maxHealthStat, 75, StatModifyType.Added), CreateStatModifier(maxHealthStat, -0.1f, StatModifyType.Additive),
            }, intrinsics: new (IntrinsicKey, int)[] { (healthIntrinsic, 80) });

            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Debug: Check what the actual stat value is
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var actualStatValue = statMap[maxHealthStat].Value;

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(67, intrinsicMap[healthIntrinsic], $"Intrinsic should be floored to 67, but stat value is {actualStatValue}");
        }

        [Test]
        public void ConflictingLimits_MinStatGreaterThanMaxStat_UsesMaxValue()
        {
            // Arrange - Edge case where minStat value > maxStat value
            IntrinsicKey healthIntrinsic = 1;
            StatKey minHealthStat = 1;
            StatKey maxHealthStat = 2;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, minHealthStat, maxHealthStat) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[]
            {
                CreateStatModifier(minHealthStat, 80, StatModifyType.Added), // Min is 80
                CreateStatModifier(maxHealthStat, 60, StatModifyType.Added), // Max is 60 (conflict!)
            }, intrinsics: new (IntrinsicKey, int)[] { (healthIntrinsic, 70) });

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            // When min > max, the system should use the max value as the limit
            Assert.AreEqual(60, intrinsicMap[healthIntrinsic], "Should use max value when min stat > max stat");
        }

        [Test]
        public void NegativeStatValues_UsedAsLimits_ClampsCorrectly()
        {
            // Arrange
            IntrinsicKey temperatureIntrinsic = 1;
            StatKey environmentStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (temperatureIntrinsic, 0, -999, 999, environmentStat, default(StatKey)) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(environmentStat, -10, StatModifyType.Added) }, // Negative min
                intrinsics: new (IntrinsicKey, int)[] { (temperatureIntrinsic, -20) });

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(-10, intrinsicMap[temperatureIntrinsic], "Should clamp to negative stat value");
        }

        [Test]
        public void ZeroStatValue_UsedAsLimit_ClampsToZero()
        {
            // Arrange
            IntrinsicKey resourceIntrinsic = 1;
            StatKey maxResourceStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (resourceIntrinsic, 50, 0, 999, default(StatKey), maxResourceStat) });
            this.SetupIntrinsicConfig(config);

            var entity = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(maxResourceStat, 0, StatModifyType.Added) }, // Zero max
                intrinsics: new (IntrinsicKey, int)[] { (resourceIntrinsic, 25) });

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entity);
            var intrinsicMap = intrinsicBuffer.AsMap();

            Assert.AreEqual(0, intrinsicMap[resourceIntrinsic], "Should clamp to zero when stat limit is zero");
        }

        [Test]
        public void LargeEntityCount_ProcessesAllCorrectly()
        {
            // Arrange - Stress test with many entities
            IntrinsicKey healthIntrinsic = 1;
            StatKey maxHealthStat = 1;

            var config = this.CreateTestIntrinsicConfig(new[] { (healthIntrinsic, 50, 0, 999, default(StatKey), maxHealthStat) });
            this.SetupIntrinsicConfig(config);

            var entityCount = 50;
            var entities = new Entity[entityCount];

            for (int i = 0; i < entityCount; i++)
            {
                var maxHealth = 100 + ((i % 10) * 10); // Varies between 100-190
                var currentHealth = maxHealth + 20; // Always exceeds max

                entities[i] = this.CreateCombinedEntity(statModifiers: new[] { CreateStatModifier(maxHealthStat, (short)maxHealth, StatModifyType.Added) },
                    intrinsics: new (IntrinsicKey, int)[] { (healthIntrinsic, currentHealth) });
            }

            // Act
            this.validationSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            for (int i = 0; i < entityCount; i++)
            {
                var expectedMax = 100 + ((i % 10) * 10);
                var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(entities[i]);
                var intrinsicMap = intrinsicBuffer.AsMap();

                Assert.AreEqual(expectedMax, intrinsicMap[healthIntrinsic], $"Entity {i} should be clamped to {expectedMax}");
            }
        }
    }
}
