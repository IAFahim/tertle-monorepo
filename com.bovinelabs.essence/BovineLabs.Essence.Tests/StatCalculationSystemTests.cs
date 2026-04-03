// <copyright file="StatCalculationSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests
{
    using BovineLabs.Essence.Data;
    using NUnit.Framework;
    using Unity.Entities;

    public class StatCalculationSystemTests : EssenceTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<StatCalculationSystem>();
        }

        [Test]
        public void BasicCalculation_SingleStatWithAdded_AppliesCorrectly()
        {
            // Arrange
            StatKey healthStat = 1;
            var baseHealthModifier = CreateStatModifier(healthStat, 100, StatModifyType.Added);
            var entity = this.CreateStatEntity(baseHealthModifier);

            var addedModifier = CreateStatModifier(healthStat, 50, StatModifyType.Added);
            this.AddStatModifiers(entity, addedModifier);

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[healthStat];

            Assert.AreEqual(150, (int)result.Value, "Base(100) + Added(50) should equal 150");
        }

        [Test]
        public void BasicCalculation_SingleStatWithAdditive_AppliesCorrectly()
        {
            // Arrange
            StatKey healthStat = 1;
            var baseHealthModifier = CreateStatModifier(healthStat, 100, StatModifyType.Added);
            var entity = this.CreateStatEntity(baseHealthModifier);

            var additiveModifier = CreateStatModifier(healthStat, 0.5f, StatModifyType.Additive);
            this.AddStatModifiers(entity, additiveModifier);

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[healthStat];

            Assert.AreEqual(150, (int)result.Value, "Base(100) * (1 + 0.5 additive) should equal 150");
        }

        [Test]
        public void BasicCalculation_SingleStatWithMultiplicative_AppliesCorrectly()
        {
            // Arrange
            StatKey healthStat = 1;
            var baseHealthModifier = CreateStatModifier(healthStat, 100, StatModifyType.Added);
            var entity = this.CreateStatEntity(baseHealthModifier);

            var multiplicativeModifier = CreateStatModifier(healthStat, 0.5f, StatModifyType.Multiplicative);
            this.AddStatModifiers(entity, multiplicativeModifier);

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[healthStat];

            Assert.AreEqual(150, (int)result.Value, "Base(100) * 1.5 multiplicative should equal 150");
        }

        [Test]
        public void ComplexCalculation_AllModifierTypes_AppliesInCorrectOrder()
        {
            // Arrange
            StatKey healthStat = 1;
            var baseHealthModifier = CreateStatModifier(healthStat, 100, StatModifyType.Added);
            var entity = this.CreateStatEntity(baseHealthModifier);

            var addedModifier = CreateStatModifier(healthStat, 20, StatModifyType.Added);
            var additiveModifier1 = CreateStatModifier(healthStat, 0.2f, StatModifyType.Additive);
            var additiveModifier2 = CreateStatModifier(healthStat, 0.3f, StatModifyType.Additive);
            var multiplicativeModifier1 = CreateStatModifier(healthStat, 0.1f, StatModifyType.Multiplicative);
            var multiplicativeModifier2 = CreateStatModifier(healthStat, 0.2f, StatModifyType.Multiplicative);

            this.AddStatModifiers(entity, addedModifier, additiveModifier1, additiveModifier2, multiplicativeModifier1, multiplicativeModifier2);

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[healthStat];

            // Expected: (100 base + 20 added) * (1 + 0.2 + 0.3 additive) * (1.1) * (1.2) = 120 * 1.5 * 1.1 * 1.2 = 237.6
            Assert.AreEqual(237.6f, result.Value, 0.1f, "Complex calculation should follow correct order");
        }

        [Test]
        public void MultipleStats_DifferentModifiers_CalculatesIndependently()
        {
            // Arrange
            StatKey healthStat = 1;
            StatKey manaStat = 2;
            StatKey strengthStat = 3;

            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added),
                CreateStatModifier(manaStat, 50, StatModifyType.Added), CreateStatModifier(strengthStat, 10, StatModifyType.Added));

            this.AddStatModifiers(entity, CreateStatModifier(healthStat, 25, StatModifyType.Added), CreateStatModifier(manaStat, 0.5f, StatModifyType.Additive),
                CreateStatModifier(strengthStat, 0.2f, StatModifyType.Multiplicative));

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();

            Assert.AreEqual(125, (int)statMap[healthStat].Value, "Health: 100 + 25 = 125");
            Assert.AreEqual(75, (int)statMap[manaStat].Value, "Mana: 50 * (1 + 0.5) = 75");
            Assert.AreEqual(12, (int)statMap[strengthStat].Value, "Strength: 10 * 1.2 = 12");
        }

        [Test]
        public void StatDefaults_NoModifiers_UsesBaseValues()
        {
            // Arrange
            StatKey healthStat = 1;
            StatKey manaStat = 2;

            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added),
                CreateStatModifier(manaStat, 50, StatModifyType.Added));

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();

            Assert.AreEqual(100, (int)statMap[healthStat].Value, "Health should remain at base value");
            Assert.AreEqual(50, (int)statMap[manaStat].Value, "Mana should remain at base value");
        }

        [Test]
        public void ConditionDirtyFlag_AfterCalculation_IsSetToTrue()
        {
            // Arrange
            StatKey healthStat = 1;
            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            this.AddStatModifiers(entity, CreateStatModifier(healthStat, 10, StatModifyType.Added));

            // Ensure condition dirty starts false
            this.Manager.SetComponentEnabled<StatConditionDirty>(entity, false);

            // Act
            this.RunSystem(this.system);

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatConditionDirty>(entity), "StatConditionDirty should be set to true after calculation");
        }

        [Test]
        public void ChangeDetection_EntityWithoutStatChanged_IsNotProcessed()
        {
            // Arrange
            StatKey healthStat = 1;
            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            this.AddStatModifiers(entity, CreateStatModifier(healthStat, 50, StatModifyType.Added));

            // Disable StatChanged flag
            this.Manager.SetComponentEnabled<StatChanged>(entity, false);

            // Manually set expected final value to detect if system ran
            var statMap = this.Manager.GetBuffer<Stat>(entity).AsMap();
            statMap[healthStat] = new StatValue
            {
                Added = 999,
                Multi = 1f,
            }; // Sentinel value

            // Act
            this.RunSystem(this.system);

            // Assert
            statMap = this.Manager.GetBuffer<Stat>(entity).AsMap();
            var result = statMap[healthStat];
            Assert.AreEqual(999, (int)result.Value, "Value should remain unchanged when StatChanged is false");
        }

        [Test]
        public void ChangeDetection_EntityWithStatChanged_IsProcessed()
        {
            // Arrange
            StatKey healthStat = 1;
            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            this.AddStatModifiers(entity, CreateStatModifier(healthStat, 50, StatModifyType.Added));

            // Explicitly ensure StatChanged is enabled
            this.Manager.SetComponentEnabled<StatChanged>(entity, true);

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[healthStat];
            Assert.AreEqual(150, (int)result.Value, "Value should be calculated when StatChanged is true");
        }

        [Test]
        public void BulkProcessing_MultipleEntities_ProcessesAllCorrectly()
        {
            // Arrange
            StatKey healthStat = 1;
            var entities = new Entity[10];

            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
                this.AddStatModifiers(entities[i], CreateStatModifier(healthStat, i * 10, StatModifyType.Added));
            }

            // Act
            this.RunSystem(this.system);

            // Assert
            for (int i = 0; i < entities.Length; i++)
            {
                var statBuffer = this.Manager.GetBuffer<Stat>(entities[i]);
                var statMap = statBuffer.AsMap();
                var result = statMap[healthStat];
                var expected = 100 + (i * 10);

                Assert.AreEqual(expected, (int)result.Value, $"Entity {i} should have health value {expected}");
            }
        }

        [Test]
        public void NegativeModifiers_AddedType_CanReduceBelowBase()
        {
            // Arrange
            StatKey healthStat = 1;
            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            this.AddStatModifiers(entity, CreateStatModifier(healthStat, -150, StatModifyType.Added));

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[healthStat];

            Assert.AreEqual(-50, (int)result.Value, "Negative modifiers should be able to reduce stats below base");
        }

        [Test]
        public void ZeroBaseValue_WithModifiers_CalculatesCorrectly()
        {
            // Arrange
            StatKey tempStat = 1;
            var entity = this.CreateStatEntity(CreateStatModifier(tempStat, 0, StatModifyType.Added));

            this.AddStatModifiers(entity, CreateStatModifier(tempStat, 50, StatModifyType.Added), CreateStatModifier(tempStat, 1.0f, StatModifyType.Additive));

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[tempStat];

            // (0 + 50) * (1 + 1.0) = 100
            Assert.AreEqual(100, (int)result.Value, "Zero base value should calculate correctly with modifiers");
        }

        [Test]
        public void EntityWithNoModifiers_OnlyDefaultValues_CalculatesCorrectly()
        {
            // Arrange - Entity with only default stat values, no additional modifiers
            StatKey healthStat = 1;
            StatKey manaStat = 2;

            var entity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added),
                CreateStatModifier(manaStat, 50, StatModifyType.Added));

            // Don't add any additional modifiers

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();

            Assert.AreEqual(100, (int)statMap[healthStat].Value, "Health should remain at default value");
            Assert.AreEqual(50, (int)statMap[manaStat].Value, "Mana should remain at default value");
        }

        [Test]
        public void ExtremelyLargeNumbers_DoesNotOverflow()
        {
            // Arrange
            StatKey damageStat = 1;
            var entity = this.CreateStatEntity(CreateStatModifier(damageStat, 1000000, StatModifyType.Added));

            this.AddStatModifiers(entity, CreateStatModifier(damageStat, 500000, StatModifyType.Added),
                CreateStatModifier(damageStat, 2.0f, StatModifyType.Additive));

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[damageStat];

            // (1000000 + 500000) * (1 + 2.0) = 4500000
            Assert.AreEqual(4500000, (int)result.Value, "Large numbers should not overflow");
            Assert.IsTrue(result.Value > 0, "Result should remain positive");
        }

        [Test]
        public void MultipleMultiplicativeModifiers_StacksCorrectly()
        {
            // Arrange
            StatKey damageStat = 1;
            var entity = this.CreateStatEntity(CreateStatModifier(damageStat, 100, StatModifyType.Added));

            this.AddStatModifiers(entity, CreateStatModifier(damageStat, 0.5f, StatModifyType.Multiplicative), // 1.5x
                CreateStatModifier(damageStat, 0.2f, StatModifyType.Multiplicative), // 1.2x
                CreateStatModifier(damageStat, 0.1f, StatModifyType.Multiplicative)); // 1.1x

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[damageStat];

            // 100 * 1.5 * 1.2 * 1.1 = 198
            Assert.AreEqual(198, (int)result.Value, "Multiple multiplicative modifiers should stack correctly");
        }

        [Test]
        public void FractionalResults_RoundsToFloat()
        {
            // Arrange
            StatKey critChanceStat = 1;
            var entity = this.CreateStatEntity(CreateStatModifier(critChanceStat, 10, StatModifyType.Added));

            this.AddStatModifiers(entity, CreateStatModifier(critChanceStat, 0.33f, StatModifyType.Additive)); // Results in 13.3

            // Act
            this.RunSystem(this.system);

            // Assert
            var statBuffer = this.Manager.GetBuffer<Stat>(entity);
            var statMap = statBuffer.AsMap();
            var result = statMap[critChanceStat];

            // 10 * (1 + 0.33) = 13.3
            Assert.AreEqual(13.3f, result.Value, 0.01f, "Fractional results should be preserved as float");
        }
    }
}
