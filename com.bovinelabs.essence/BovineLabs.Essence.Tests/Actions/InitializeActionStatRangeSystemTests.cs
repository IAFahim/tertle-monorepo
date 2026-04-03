// <copyright file="InitializeActionStatRangeSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests.Actions
{
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Essence.Actions;
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Actions;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;

    public class InitializeActionStatRangeSystemTests : EssenceTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<InitializeActionStatRangeSystem>();
        }

        [Test]
        public void RangeInitialization_OnlyProcessesRangeValueType()
        {
            // Arrange
            var entity = this.CreateEntityWithInitialize();
            var actionStats = this.Manager.AddBuffer<ActionStat>(entity);

            // Add both Range and Fixed value types
            actionStats.Add(CreateRangeActionStat(1, StatModifyType.Added, 10, 20));
            actionStats.Add(CreateFixedActionStat(2, 15, StatModifyType.Added, Target.Target));
            actionStats.Add(CreateRangeActionStat(3, StatModifyType.Multiplicative, 1.0f, 2.0f));

            // Act
            this.RunSystem(this.system);

            // Assert
            var buffer = this.Manager.GetBuffer<ActionStat>(entity);

            // Range type should be initialized
            Assert.That(buffer[0].Range.Value.Int, Is.InRange(10, 20), "Range Added stat should be initialized within bounds");
            Assert.That(buffer[2].Range.Value.Float, Is.InRange(1.0f, 2.0f), "Range Multiplicative stat should be initialized within bounds");

            // Fixed type should remain unchanged
            Assert.That(buffer[1].Fixed.Int, Is.EqualTo(15), "Fixed value type should not be modified by Range system");
        }

        [Test]
        public void AddedModifyType_InitializesIntegerRange()
        {
            // Arrange
            var entity = this.CreateEntityWithInitialize();
            var actionStats = this.Manager.AddBuffer<ActionStat>(entity);
            actionStats.Add(CreateRangeActionStat(1, StatModifyType.Added, 50, 100));

            // Act - Run multiple times to verify randomness
            for (int i = 0; i < 10; i++)
            {
                this.RunSystem(this.system);

                var buffer = this.Manager.GetBuffer<ActionStat>(entity);
                var value = buffer[0].Range.Value.Int;

                Assert.That(value, Is.InRange(50, 100), $"Iteration {i}: Added stat should use integer range [50,100]");
            }
        }

        [Test]
        public void NonAddedModifyType_InitializesFloatRange()
        {
            // Arrange
            var entity = this.CreateEntityWithInitialize();
            var actionStats = this.Manager.AddBuffer<ActionStat>(entity);
            actionStats.Add(CreateRangeActionStat(1, StatModifyType.Multiplicative, 0.5f, 1.5f));

            // Act - Run multiple times to verify randomness
            for (int i = 0; i < 10; i++)
            {
                this.RunSystem(this.system);

                var buffer = this.Manager.GetBuffer<ActionStat>(entity);
                var value = buffer[0].Range.Value.Float;

                Assert.That(value, Is.InRange(0.5f, 1.5f), $"Iteration {i}: Non-Added stat should use float range [0.5,1.5]");
            }
        }

        [Test]
        public void MultipleRangeStats_AllInitializedCorrectly()
        {
            // Arrange
            var entity = this.CreateEntityWithInitialize();
            var actionStats = this.Manager.AddBuffer<ActionStat>(entity);

            actionStats.Add(CreateRangeActionStat(1, StatModifyType.Added, 10, 20));
            actionStats.Add(CreateRangeActionStat(2, StatModifyType.Added, 30, 40));
            actionStats.Add(CreateRangeActionStat(3, StatModifyType.Multiplicative, 2.0f, 3.0f));

            // Act
            this.RunSystem(this.system);

            // Assert
            var buffer = this.Manager.GetBuffer<ActionStat>(entity);

            Assert.That(buffer[0].Range.Value.Int, Is.InRange(10, 20), "First range stat should be initialized");
            Assert.That(buffer[1].Range.Value.Int, Is.InRange(30, 40), "Second range stat should be initialized");
            Assert.That(buffer[2].Range.Value.Float, Is.InRange(2.0f, 3.0f), "Third range stat should be initialized");
        }

        [Test]
        public void EmptyActionStatBuffer_NoErrors()
        {
            // Arrange
            var entity = this.CreateEntityWithInitialize();
            this.Manager.AddBuffer<ActionStat>(entity); // Empty buffer

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => this.RunSystem(this.system));
        }

        [Test]
        public void InitializeEntity_ProcessedCorrectly()
        {
            // Arrange
            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponent<InitializeEntity>(entity);
            var actionStats = this.Manager.AddBuffer<ActionStat>(entity);
            actionStats.Add(CreateRangeActionStat(1, StatModifyType.Added, 10, 20));

            // Act
            this.RunSystem(this.system);

            // Assert
            var buffer = this.Manager.GetBuffer<ActionStat>(entity);
            Assert.That(buffer[0].Range.Value.Int, Is.InRange(10, 20), "Entity with InitializeEntity should be processed");
        }

        [Test]
        public void InitializeSubSceneEntity_ProcessedCorrectly()
        {
            // Arrange
            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponent<InitializeSubSceneEntity>(entity);
            var actionStats = this.Manager.AddBuffer<ActionStat>(entity);
            actionStats.Add(CreateRangeActionStat(1, StatModifyType.Added, 10, 20));

            // Act
            this.RunSystem(this.system);

            // Assert
            var buffer = this.Manager.GetBuffer<ActionStat>(entity);
            Assert.That(buffer[0].Range.Value.Int, Is.InRange(10, 20), "Entity with InitializeSubSceneEntity should be processed");
        }

        [Test]
        public void NoInitializeComponent_NotProcessed()
        {
            // Arrange
            var entity = this.Manager.CreateEntity();
            var actionStats = this.Manager.AddBuffer<ActionStat>(entity);
            var originalStat = CreateRangeActionStat(1, StatModifyType.Added, 10, 20);
            actionStats.Add(originalStat);

            // Act
            this.RunSystem(this.system);

            // Assert
            var buffer = this.Manager.GetBuffer<ActionStat>(entity);
            Assert.That(buffer[0].Range.Value.Int, Is.EqualTo(0), "Entity without Initialize component should not be processed");
        }

        [Test]
        public void RandomValueGeneration_ProducesVariedResults()
        {
            // Arrange
            const int testIterations = 50;
            const int minValue = 1;
            const int maxValue = 100;
            var results = new System.Collections.Generic.HashSet<int>();

            // Act - Generate many values to verify randomness
            for (int i = 0; i < testIterations; i++)
            {
                var entity = this.CreateEntityWithInitialize();
                var actionStats = this.Manager.AddBuffer<ActionStat>(entity);
                actionStats.Add(CreateRangeActionStat(1, StatModifyType.Added, minValue, maxValue));

                this.RunSystem(this.system);

                var buffer = this.Manager.GetBuffer<ActionStat>(entity);
                results.Add(buffer[0].Range.Value.Int);

                this.Manager.DestroyEntity(entity);
            }

            // Assert - Should have generated multiple different values
            Assert.That(results.Count, Is.GreaterThan(5), "Random generation should produce varied results");

            foreach (var value in results)
            {
                Assert.That(value, Is.InRange(minValue, maxValue), $"All generated values should be within range [{minValue},{maxValue}]");
            }
        }

        [Test]
        public void FloatRandomGeneration_ProducesVariedResults()
        {
            // Arrange
            const int testIterations = 50;
            const float minValue = 0.1f;
            const float maxValue = 2.0f;
            var results = new System.Collections.Generic.HashSet<float>();

            // Act
            for (int i = 0; i < testIterations; i++)
            {
                var entity = this.CreateEntityWithInitialize();
                var actionStats = this.Manager.AddBuffer<ActionStat>(entity);
                actionStats.Add(CreateRangeActionStat(1, StatModifyType.Multiplicative, minValue, maxValue));

                this.RunSystem(this.system);

                var buffer = this.Manager.GetBuffer<ActionStat>(entity);
                results.Add(buffer[0].Range.Value.Float);

                this.Manager.DestroyEntity(entity);
            }

            // Assert
            Assert.That(results.Count, Is.GreaterThan(5), "Float random generation should produce varied results");

            foreach (var value in results)
            {
                Assert.That(value, Is.InRange(minValue, maxValue), $"All generated values should be within range [{minValue},{maxValue}]");
            }
        }

        [Test]
        public void MixedValueTypes_OnlyRangeProcessed()
        {
            // Arrange
            var entity = this.CreateEntityWithInitialize();
            var actionStats = this.Manager.AddBuffer<ActionStat>(entity);

            actionStats.Add(CreateFixedActionStat(1, 50, StatModifyType.Added, Target.Target));
            actionStats.Add(CreateLinearActionStat(2, 0, 0, 100, new ActionStat.ValueUnion { Float = 2.0f }, new ActionStat.ValueUnion { Int = 10 }, StatModifyType.Added, Target.Target));
            actionStats.Add(CreateRangeActionStat(3, StatModifyType.Added, 20, 30));

            // Store original values
            var originalBuffer = this.Manager.GetBuffer<ActionStat>(entity);
            var originalFixed = originalBuffer[0].Fixed.Int;
            var originalLinearFromMin = originalBuffer[1].Linear.FromMin;

            // Act
            this.RunSystem(this.system);

            // Assert
            var buffer = this.Manager.GetBuffer<ActionStat>(entity);

            Assert.That(buffer[0].Fixed.Int, Is.EqualTo(originalFixed), "Fixed value should remain unchanged");
            Assert.That(buffer[1].Linear.FromMin, Is.EqualTo(originalLinearFromMin), "Linear value should remain unchanged");
            Assert.That(buffer[2].Range.Value.Int, Is.InRange(20, 30), "Range value should be initialized");
        }

        private Entity CreateEntityWithInitialize()
        {
            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponent<InitializeEntity>(entity);
            return entity;
        }

        private static ActionStat CreateRangeActionStat(StatKey type, StatModifyType modifyType, int min, int max)
        {
            return new ActionStat
            {
                Type = type,
                ModifyType = modifyType,
                ValueType = StatValueType.Range,
                Target = Target.Target,
                Range = new ActionStat.RangeData
                {
                    Min = new ActionStat.ValueUnion { Int = min },
                    Max = new ActionStat.ValueUnion { Int = max },
                    Value = new ActionStat.ValueUnion { Int = 0 }, // Will be set by system
                },
            };
        }

        private static ActionStat CreateRangeActionStat(StatKey type, StatModifyType modifyType, float min, float max)
        {
            return new ActionStat
            {
                Type = type,
                ModifyType = modifyType,
                ValueType = StatValueType.Range,
                Target = Target.Target,
                Range = new ActionStat.RangeData
                {
                    Min = new ActionStat.ValueUnion { Float = min },
                    Max = new ActionStat.ValueUnion { Float = max },
                    Value = new ActionStat.ValueUnion { Float = 0f }, // Will be set by system
                },
            };
        }
    }
}
