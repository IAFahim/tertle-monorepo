// <copyright file="ActionStatSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests.Actions
{
    using BovineLabs.Essence.Actions;
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;

    public class ActionStatSystemTests : EssenceTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<ActionStatSystem>();
        }

        [Test]
        public void FixedValue_SingleStat_AppliesCorrectly()
        {
            // Arrange
            StatKey healthStat = 1;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = healthStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 50 },
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(1, modifiers.Length, "Should have one modifier");
            Assert.AreEqual(50, modifiers[0].Value.Value, "Modifier value should match");
            Assert.AreEqual(reactionEntity, modifiers[0].SourceEntity, "Source entity should match");
            Assert.AreEqual(healthStat, modifiers[0].Value.Type, "Stat key should match");
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(targetEntity), " StatChanged should be enabled");
        }

        [Test]
        public void LinearValue_WithConditionValues_RemapsCorrectly()
        {
            // Arrange
            StatKey manaStat = 2;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(manaStat, 50, StatModifyType.Added));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            // Add condition values for linear calculation
            var conditionValuesBuffer = this.Manager.AddBuffer<ConditionValues>(reactionEntity);
            conditionValuesBuffer.Add(new ConditionValues { Value = 75 }); // Middle of range 50-100

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = manaStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Linear,
                Target = Target.Target,
                Linear = new ActionStat.LinearData
                {
                    Index = 0,
                    FromMin = 50,
                    FromMax = 100,
                    ToMin = new ActionStat.ValueUnion { Int = 10 },
                    ToMax = new ActionStat.ValueUnion { Int = 20 },
                },
            });

            // Act
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(1, modifiers.Length, "Should have one stat modifier added");

            // Expected: remap(75, 50, 100, 10, 20) = 15
            Assert.AreEqual(15, modifiers[0].Value.Value, "Linear value should remap 75 from [50,100] to [10,20] = 15");
        }

        [Test]
        public void RangeValue_UsesPresetValue()
        {
            // Arrange
            StatKey strengthStat = 3;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(strengthStat, 10, StatModifyType.Added));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = strengthStat,
                ModifyType = StatModifyType.Additive,
                ValueType = StatValueType.Range,
                Target = Target.Target,
                Range = new ActionStat.RangeData
                {
                    Value = new ActionStat.ValueUnion { Float = 0.5f }, // Pre-rolled value
                    Min = new ActionStat.ValueUnion { Float = 0.1f },
                    Max = new ActionStat.ValueUnion { Float = 1.0f },
                },
            });

            // Act
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(1, modifiers.Length, "Should have one stat modifier added");
            Assert.AreEqual(0.5f, modifiers[0].Value.ValueFloat, 0.001f, "Range value should use preset value");
        }

        [Test]
        public void TargetResolution_AllTargetTypes_ResolvesCorrectly()
        {
            // Arrange
            StatKey testStat = 1;
            var ownerEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
            var sourceEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
            var targetEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
            var custom0Entity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
            var custom1Entity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));

            var reactionEntity = this.CreateReactionEntity(ownerEntity, sourceEntity, targetEntity);

            // Add stat components to reactionEntity so it can be targeted by Self
            this.Manager.AddBuffer<StatModifiers>(reactionEntity);
            this.Manager.AddComponentData(reactionEntity, new StatChanged());

            // Add TargetsCustom for custom target resolution
            this.Manager.AddComponentData(reactionEntity, new TargetsCustom
            {
                Target0 = custom0Entity,
                Target1 = custom1Entity,
            });

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Owner,
                Fixed = new ActionStat.ValueUnion { Int = 1 },
            });
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Source,
                Fixed = new ActionStat.ValueUnion { Int = 2 },
            });
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 3 },
            });
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Self,
                Fixed = new ActionStat.ValueUnion { Int = 4 },
            });
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Custom0,
                Fixed = new ActionStat.ValueUnion { Int = 5 },
            });
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Custom1,
                Fixed = new ActionStat.ValueUnion { Int = 6 },
            });

            // Act
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert target resolution
            var ownerModifiers = this.Manager.GetBuffer<StatModifiers>(ownerEntity);
            var sourceModifiers = this.Manager.GetBuffer<StatModifiers>(sourceEntity);
            var targetModifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            var selfModifiers = this.Manager.GetBuffer<StatModifiers>(reactionEntity);
            var custom0Modifiers = this.Manager.GetBuffer<StatModifiers>(custom0Entity);
            var custom1Modifiers = this.Manager.GetBuffer<StatModifiers>(custom1Entity);

            Assert.AreEqual(1, ownerModifiers.Length, "Owner should have one modifier");
            Assert.AreEqual(1, ownerModifiers[0].Value.Value, "Owner modifier value should be 1");

            Assert.AreEqual(1, sourceModifiers.Length, "Source should have one modifier");
            Assert.AreEqual(2, sourceModifiers[0].Value.Value, "Source modifier value should be 2");

            Assert.AreEqual(1, targetModifiers.Length, "Target should have one modifier");
            Assert.AreEqual(3, targetModifiers[0].Value.Value, "Target modifier value should be 3");

            Assert.AreEqual(1, selfModifiers.Length, "Self should have one modifier");
            Assert.AreEqual(4, selfModifiers[0].Value.Value, "Self modifier value should be 4");

            Assert.AreEqual(1, custom0Modifiers.Length, "Custom0 should have one modifier");
            Assert.AreEqual(5, custom0Modifiers[0].Value.Value, "Custom0 modifier value should be 5");

            Assert.AreEqual(1, custom1Modifiers.Length, "Custom1 should have one modifier");
            Assert.AreEqual(6, custom1Modifiers[0].Value.Value, "Custom1 modifier value should be 6");
        }

        [Test]
        public void MultipleActionStats_SameTarget_AccumulatesCorrectly()
        {
            // Arrange
            StatKey healthStat = 1;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = healthStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 25 },
            });
            actionStatBuffer.Add(new ActionStat
            {
                Type = healthStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 30 },
            });
            actionStatBuffer.Add(new ActionStat
            {
                Type = healthStat,
                ModifyType = StatModifyType.Additive,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Float = 0.2f },
            });

            // Act
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(3, modifiers.Length, "Should have three stat modifiers added");

            // Check all modifiers are present
            Assert.AreEqual(25, modifiers[0].Value.Value, "First modifier should be 25");
            Assert.AreEqual(30, modifiers[1].Value.Value, "Second modifier should be 30");
            Assert.AreEqual(0.2f, modifiers[2].Value.ValueFloat, 0.001f, "Third modifier should be 0.2");

            // All should reference the same source
            Assert.AreEqual(reactionEntity, modifiers[0].SourceEntity, "All modifiers should reference reaction entity");
            Assert.AreEqual(reactionEntity, modifiers[1].SourceEntity, "All modifiers should reference reaction entity");
            Assert.AreEqual(reactionEntity, modifiers[2].SourceEntity, "All modifiers should reference reaction entity");
        }

        [Test]
        public void BulkProcessing_MultipleEntities_ProcessesAllCorrectly()
        {
            // Arrange
            StatKey testStat = 1;
            var targetEntities = new Entity[5];
            var reactionEntities = new Entity[5];

            for (int i = 0; i < 5; i++)
            {
                targetEntities[i] = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
                reactionEntities[i] = this.CreateActionStatEntity(targetEntities[i],
                    CreateFixedActionStat(testStat, (i + 1) * 10, StatModifyType.Added, Target.Target));
            }

            // Act
            this.RunSystem(this.system);

            // Assert
            for (int i = 0; i < 5; i++)
            {
                var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntities[i]);
                Assert.AreEqual(1, modifiers.Length, $"Entity {i} should have one modifier");
                Assert.AreEqual((i + 1) * 10, modifiers[0].Value.Value, $"Entity {i} should have modifier value {(i + 1) * 10}");
                Assert.AreEqual(reactionEntities[i], modifiers[0].SourceEntity, $"Entity {i} modifier should reference correct reaction entity");
            }
        }

        [Test]
        public void ChangeDetection_OnlyNewlyActivated_AreProcessed()
        {
            // Arrange
            StatKey testStat = 1;
            var targetEntity1 = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
            var targetEntity2 = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));

            // Entity 1: Newly activated (Active enabled, ActivePrevious disabled)
            var reactionEntity1 = this.CreateReactionEntity(targetEntity1);

            // Entity 2: Already active (Active enabled, ActivePrevious enabled)
            var reactionEntity2 = this.CreateReactionEntity(targetEntity2);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity2, true);

            // Add action stats to both
            var actionStatBuffer1 = this.Manager.AddBuffer<ActionStat>(reactionEntity1);
            actionStatBuffer1.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 50 },
            });

            var actionStatBuffer2 = this.Manager.AddBuffer<ActionStat>(reactionEntity2);
            actionStatBuffer2.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 75 },
            });

            // Act
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var modifiers1 = this.Manager.GetBuffer<StatModifiers>(targetEntity1);
            var modifiers2 = this.Manager.GetBuffer<StatModifiers>(targetEntity2);

            Assert.AreEqual(1, modifiers1.Length, "Newly activated entity should have modifier added");
            Assert.AreEqual(0, modifiers2.Length, "Already active entity should not have modifier added");
            Assert.AreEqual(50, modifiers1[0].Value.Value, "Modifier value should be correct");
        }

        [Test]
        public void NoTargetEntity_SkipsProcessing()
        {
            // Arrange
            StatKey testStat = 1;
            var reactionEntity = this.CreateReactionEntity();

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 50 },
            });

            // Act
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert - No exceptions should be thrown, system should handle gracefully
            Assert.Pass("System should handle entities without valid targets gracefully");
        }

        [Test]
        public void LinearValue_AdditiveModifier_RemapsAsFloat()
        {
            // Arrange
            StatKey testStat = 1;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var conditionValuesBuffer = this.Manager.AddBuffer<ConditionValues>(reactionEntity);
            conditionValuesBuffer.Add(new ConditionValues { Value = 60 });

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Additive,
                ValueType = StatValueType.Linear,
                Target = Target.Target,
                Linear = new ActionStat.LinearData
                {
                    Index = 0,
                    FromMin = 0,
                    FromMax = 100,
                    ToMin = new ActionStat.ValueUnion { Float = 0.0f },
                    ToMax = new ActionStat.ValueUnion { Float = 1.0f },
                },
            });

            // Act
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(1, modifiers.Length, "Should have one stat modifier added");
            Assert.AreEqual(0.6f, modifiers[0].Value.ValueFloat, 0.001f, "Additive linear should remap as float: 60/100 = 0.6");
        }

        [Test]
        public void LinearValue_ClampedInput_UsesClampedValue()
        {
            // Arrange
            StatKey testStat = 1;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var conditionValuesBuffer = this.Manager.AddBuffer<ConditionValues>(reactionEntity);
            conditionValuesBuffer.Add(new ConditionValues { Value = 150 }); // Above max

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Linear,
                Target = Target.Target,
                Linear = new ActionStat.LinearData
                {
                    Index = 0,
                    FromMin = 50,
                    FromMax = 100,
                    ToMin = new ActionStat.ValueUnion { Int = 10 },
                    ToMax = new ActionStat.ValueUnion { Int = 20 },
                },
            });

            // Act
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(1, modifiers.Length, "Should have one stat modifier added");

            // Input 150 should be clamped to 100, then remapped: remap(100, 50, 100, 10, 20) = 20
            Assert.AreEqual(20, modifiers[0].Value.Value, "Input should be clamped to max before remapping");
        }
    }
}
