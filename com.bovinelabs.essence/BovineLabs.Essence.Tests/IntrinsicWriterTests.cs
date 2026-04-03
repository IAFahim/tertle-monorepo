// <copyright file="IntrinsicWriterTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Essence.Data;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Unit tests for IntrinsicWriter, covering all access patterns, value clamping, and event integration.
    /// </summary>
    public class IntrinsicWriterTests : EssenceTestsFixture
    {
        // Test constants
        private readonly IntrinsicKey healthKey = 1;
        private readonly IntrinsicKey manaKey = 2;
        private readonly IntrinsicKey staminaKey = 3;
        private readonly StatKey maxHealthStatKey = 1;

        private Entity testEntity;
        private IntrinsicWriter.Lookup intrinsicWriterLookup;
        private IntrinsicWriter.TypeHandle intrinsicWriterTypeHandle;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();
            this.CreateTestEntity();
            this.SetupLookupAndTypeHandle();
        }

        [Test]
        public void Add_WithPositiveDelta_IncreasesValueCorrectly()
        {
            // Arrange
            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act
            var result = intrinsicWriter.Add(this.healthKey, 25);

            // Assert
            Assert.AreEqual(125, result, "Add should return the new clamped value");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.healthKey, out var actualValue), "Health intrinsic should exist in buffer");
            Assert.AreEqual(125, actualValue, "Health intrinsic should be increased by delta");
        }

        [Test]
        public void Add_ExceedsStaticMaxLimit_ClampsCorrectly()
        {
            // Arrange
            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act - Try to add way more than the static max (200 for mana)
            var result = intrinsicWriter.Add(this.manaKey, 1000);

            // Assert
            Assert.AreEqual(200, result, "Add should clamp to static max limit");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.manaKey, out var actualValue), "Mana intrinsic should exist in buffer");
            Assert.AreEqual(200, actualValue, "Mana should be clamped to static max limit");
        }

        [Test]
        public void Set_WithValidValue_SetsExactValue()
        {
            // Arrange
            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act
            var result = intrinsicWriter.Set(this.healthKey, 85);

            // Assert
            Assert.AreEqual(85, result, "Set should return the new clamped value");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.healthKey, out var actualValue), "Health intrinsic should exist in buffer");
            Assert.AreEqual(85, actualValue, "Health should be set to exact value");
        }

        [Test]
        public void Subtract_WithPositiveValue_DecreasesIntrinsic()
        {
            // Arrange
            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act
            var result = intrinsicWriter.Subtract(this.healthKey, 20);

            // Assert
            Assert.AreEqual(80, result, "Subtract should return the new value");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.healthKey, out var actualValue), "Health intrinsic should exist in buffer");
            Assert.AreEqual(80, actualValue, "Health should be decreased by subtract amount");
        }

        [Test]
        public void Add_ExceedsDynamicMaxLimit_ClampsToStatValue()
        {
            // Arrange - Set MaxHealthStat to 120, then try to exceed it
            var statMap = this.Manager.GetBuffer<Stat>(this.testEntity).AsMap();
            statMap[this.maxHealthStatKey] = new StatValue
            {
                Added = 120,
                Multi = 1f,
            };

            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act - Try to add beyond the dynamic max (current 100 + 50 = 150, but max stat is 120)
            var result = intrinsicWriter.Add(this.healthKey, 50);

            // Assert
            Assert.AreEqual(120, result, "Add should clamp to dynamic max limit from stat");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.healthKey, out var actualValue), "Health intrinsic should exist in buffer");
            Assert.AreEqual(120, actualValue, "Health should be clamped to MaxHealthStat value");
        }

        [Test]
        public void Add_BelowDynamicMinLimit_ClampsToStatValue()
        {
            // Arrange - Add MinStaminaStat to existing entity and set its value to 30
            var minStaminaStatKey = (StatKey)10;
            var statMap = this.Manager.GetBuffer<Stat>(this.testEntity).AsMap();
            statMap[minStaminaStatKey] = new StatValue
            {
                Added = 30,
                Multi = 1f,
            };

            // Update intrinsic config to include min stat for stamina (recreate config with min stat)
            var updatedConfig = this.CreateTestIntrinsicConfig(new (IntrinsicKey, int, int, int, StatKey, StatKey)[]
            {
                // Health: default 100, static limits 0-999, dynamic max from MaxHealthStat
                (this.healthKey, 100, 0, 999, default, this.maxHealthStatKey),

                // Mana: default 50, static limits 0-200, no dynamic limits
                (this.manaKey, 50, 0, 200, default, default),

                // Stamina: default 75, static limits 0-150, with dynamic min limit
                (this.staminaKey, 75, 0, 150, minStaminaStatKey, default),
            });

            // Remove old config and set new one (find and destroy existing config entity)
            using var configQuery = this.Manager.CreateEntityQuery(typeof(EssenceConfig));
            this.Manager.DestroyEntity(configQuery);
            this.SetupIntrinsicConfig(updatedConfig);

            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act - Try to subtract below the dynamic min (75 - 50 = 25, but min stat is 30)
            var result = intrinsicWriter.Subtract(this.staminaKey, 50);

            // Assert
            Assert.AreEqual(30, result, "Subtract should clamp to dynamic min limit from stat");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.staminaKey, out var actualValue), "Stamina intrinsic should exist in buffer");
            Assert.AreEqual(30, actualValue, "Stamina should be clamped to MinStaminaStat value");
        }

        [Test]
        public void Add_WithZeroDelta_ReturnsCurrentValue()
        {
            // Arrange
            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act
            var result = intrinsicWriter.Add(this.healthKey, 0);

            // Assert
            Assert.AreEqual(100, result, "Add with zero delta should return current value");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.healthKey, out var actualValue), "Health intrinsic should exist in buffer");
            Assert.AreEqual(100, actualValue, "Health should remain unchanged with zero delta");
        }

        [Test]
        public void Add_WithInvalidKey_LogsErrorAndReturnsZero()
        {
            // Arrange
            var intrinsicWriter = this.GetDirectIntrinsicWriter();
            var invalidKey = (IntrinsicKey)999; // Key not in config

            // Act & Assert - Expect error log for invalid key
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex(".*Key.*not found.*"));
            var result = intrinsicWriter.Add(invalidKey, 10);

            // Assert
            Assert.AreEqual(0, result, "Add with invalid key should return 0");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsFalse(intrinsicMap.TryGetValue(invalidKey, out _), "Invalid intrinsic should not be added to buffer");
        }

        [Test]
        public void AccessPatterns_LookupVsResolvedChunk_ProduceSameResults()
        {
            // Arrange - Get writers from both access patterns
            var lookupWriter = this.GetDirectIntrinsicWriter(); // Uses Lookup pattern

            // Get ResolvedChunk writer
            this.intrinsicWriterTypeHandle.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>(), this.Manager.GetSingleton<EssenceConfig>());
            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Intrinsic, IntrinsicConditionDirty>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this.Manager);

            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            Assert.Greater(chunks.Length, 0, "Should have at least one chunk with test entity");

            var resolvedChunk = this.intrinsicWriterTypeHandle.Resolve(chunks[0]);
            Assert.IsTrue(resolvedChunk.Intrinsics.Length > 0, "ResolvedChunk should exist");
            var chunkWriter = resolvedChunk[0]; // First entity in chunk

            // Act - Perform same operation with both writers
            var lookupResult = lookupWriter.Add(this.healthKey, 30);

            // Reset health back to 100 for fair comparison
            lookupWriter.Set(this.healthKey, 100);

            var chunkResult = chunkWriter.Add(this.healthKey, 30);

            // Assert - Both should produce identical results
            Assert.AreEqual(130, lookupResult, "Lookup writer should return correct result");
            Assert.AreEqual(130, chunkResult, "ResolvedChunk writer should return correct result");
            Assert.AreEqual(lookupResult, chunkResult, "Both access patterns should produce identical results");

            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.healthKey, out var finalValue), "Health intrinsic should exist");
            Assert.AreEqual(130, finalValue, "Final health value should match both operations");
        }

        [Test]
        public void Set_WithDynamicMaxLimit_ClampsToStatValue()
        {
            // Arrange - Set MaxHealthStat to a value lower than what we'll try to set
            var statMap = this.Manager.GetBuffer<Stat>(this.testEntity).AsMap();
            statMap[this.maxHealthStatKey] = new StatValue
            {
                Added = 90,
                Multi = 1f,
            }; // Lower than current health

            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act - Try to set health to 150, but it should clamp to 90 (MaxHealthStat)
            var result = intrinsicWriter.Set(this.healthKey, 150);

            // Assert
            Assert.AreEqual(90, result, "Set should clamp to dynamic max limit from stat");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.healthKey, out var actualValue), "Health intrinsic should exist in buffer");
            Assert.AreEqual(90, actualValue, "Health should be clamped to MaxHealthStat value");
        }

        [Test]
        public void Add_ModifiesIntrinsic_SetsIntrinsicConditionDirtyFlag()
        {
            // Arrange - Ensure the flag starts as disabled
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(this.testEntity, false);
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(this.testEntity), "IntrinsicConditionDirty should start disabled");

            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act - Modify an intrinsic value
            intrinsicWriter.Add(this.healthKey, 25);

            // Assert - The dirty flag should be enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(this.testEntity),
                "IntrinsicConditionDirty should be enabled after modifying intrinsic");
        }

        [Test]
        public void Subtract_BelowDynamicMinLimit_ClampsToMinStatValue()
        {
            // Arrange - Create a dynamic min limit scenario for health using a MinHealthStat
            var minHealthStatKey = (StatKey)20;
            var statMap = this.Manager.GetBuffer<Stat>(this.testEntity).AsMap();
            statMap[minHealthStatKey] = new StatValue
            {
                Added = 40,
                Multi = 1f,
            }; // Min health = 40

            // Update config to include min stat for health
            var updatedConfig = this.CreateTestIntrinsicConfig(new (IntrinsicKey, int, int, int, StatKey, StatKey)[]
            {
                // Health: default 100, static limits 0-999, both dynamic min and max limits
                (this.healthKey, 100, 0, 999, minHealthStatKey, this.maxHealthStatKey),
                (this.manaKey, 50, 0, 200, default, default),
                (this.staminaKey, 75, 0, 150, default, default),
            });

            // Replace config
            using var configQuery = this.Manager.CreateEntityQuery(typeof(EssenceConfig));
            this.Manager.DestroyEntity(configQuery);
            this.SetupIntrinsicConfig(updatedConfig);

            var intrinsicWriter = this.GetDirectIntrinsicWriter();

            // Act - Try to subtract below the dynamic min (100 - 80 = 20, but min stat is 40)
            var result = intrinsicWriter.Subtract(this.healthKey, 80);

            // Assert
            Assert.AreEqual(40, result, "Subtract should clamp to dynamic min limit from MinHealthStat");
            var intrinsicMap = this.Manager.GetBuffer<Intrinsic>(this.testEntity).AsMap();
            Assert.IsTrue(intrinsicMap.TryGetValue(this.healthKey, out var actualValue), "Health intrinsic should exist in buffer");
            Assert.AreEqual(40, actualValue, "Health should be clamped to MinHealthStat value");
        }

        [Test]
        public void Set_WithSameValue_ReturnsEarlyWithoutConditionTrigger()
        {
            // Arrange
            var intrinsicWriter = this.GetDirectIntrinsicWriter();
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(this.testEntity, false);

            // Act - Set health to its current value (100)
            var result = intrinsicWriter.Set(this.healthKey, 100);

            // Assert
            Assert.AreEqual(100, result, "Set with same value should return current value");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(this.testEntity),
                "IntrinsicConditionDirty should remain disabled when no change occurs");
        }

        private void CreateTestEntity()
        {
            // Setup comprehensive IntrinsicConfig for testing
            var config = this.CreateTestIntrinsicConfig(new (IntrinsicKey, int, int, int, StatKey, StatKey)[]
            {
                // Health: default 100, static limits 0-999, dynamic max from MaxHealthStat
                (this.healthKey, 100, 0, 999, default, this.maxHealthStatKey),

                // Mana: default 50, static limits 0-200, no dynamic limits
                (this.manaKey, 50, 0, 200, default, default),

                // Stamina: default 75, static limits 0-150, no dynamic limits
                (this.staminaKey, 75, 0, 150, default, default),
            });

            this.SetupIntrinsicConfig(config);

            // Create test entity with both stats and intrinsics (needed for dynamic limits)
            this.testEntity = this.CreateCombinedEntity(
                new[] { CreateStatModifier(this.maxHealthStatKey, 150, StatModifyType.Added) }, // Default max health stat
                new[] { CreateIntrinsicDefault(this.healthKey, 100), CreateIntrinsicDefault(this.manaKey, 50), CreateIntrinsicDefault(this.staminaKey, 75) });
        }

        private void SetupLookupAndTypeHandle()
        {
            // Create a test system to get SystemState
            this.World.CreateSystem<TestSystem>();
            ref var state = ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>();

            this.intrinsicWriterLookup.Create(ref state);
            this.intrinsicWriterTypeHandle.Create(ref state);
        }

        private IntrinsicWriter GetDirectIntrinsicWriter()
        {
            // Update the lookup to ensure it's synchronized
            ref var state = ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>();
            this.intrinsicWriterLookup.Update(ref state, this.Manager.GetSingleton<EssenceConfig>());

            return this.intrinsicWriterLookup[this.testEntity];
        }

        // Simple test system for getting SystemState
        private partial struct TestSystem : ISystem
        {
            public void OnCreate(ref SystemState state)
            {
            }

            public void OnUpdate(ref SystemState state)
            {
            }
        }
    }
}
