// <copyright file="ActionStatDeactivatedSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests.Actions
{
    using BovineLabs.Essence.Actions;
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;

    public class ActionStatDeactivatedSystemTests : EssenceTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<ActionStatDeactivatedSystem>();
        }

        [Test]
        public void SingleModifier_RemovedOnDeactivation()
        {
            // Arrange
            StatKey healthStat = 1;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added));
            var reactionEntity = this.CreateReactionEntity(targetEntity);

            // Set up as previously active (ActivePrevious enabled, Active disabled)
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = healthStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 50 },
            });

            // Add a modifier from the reaction to the target
            var targetModifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            targetModifiers.Add(new StatModifiers
            {
                SourceEntity = reactionEntity,
                Value = new StatModifier
                {
                    Type = healthStat,
                    Value = 50,
                    ModifyType = StatModifyType.Added,
                },
            });

            // Disable StatChanged to verify it gets enabled
            this.Manager.SetComponentEnabled<StatChanged>(targetEntity, false);

            // Act
            this.RunSystem(this.system);

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(0, modifiers.Length, "All modifiers from deactivated reaction should be removed");
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatChanged>(targetEntity), "StatChanged should be enabled after modifier removal");
        }

        [Test]
        public void MultipleModifiers_SameReaction_AllRemovedOnDeactivation()
        {
            // Arrange
            StatKey healthStat = 1;
            StatKey manaStat = 2;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(healthStat, 100, StatModifyType.Added),
                CreateStatModifier(manaStat, 50, StatModifyType.Added));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            // Set up as previously active (ActivePrevious enabled, Active disabled)
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

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
                Type = manaStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 15 },
            });

            // Add modifiers from the reaction to the target
            var targetModifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            targetModifiers.Add(new StatModifiers
            {
                SourceEntity = reactionEntity,
                Value = new StatModifier
                {
                    Type = healthStat,
                    Value = 25,
                    ModifyType = StatModifyType.Added,
                },
            });

            targetModifiers.Add(new StatModifiers
            {
                SourceEntity = reactionEntity,
                Value = new StatModifier
                {
                    Type = manaStat,
                    Value = 15,
                    ModifyType = StatModifyType.Added,
                },
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(0, modifiers.Length, "All modifiers from deactivated reaction should be removed");
        }

        [Test]
        public void CorrectCount_ReferenceCountingDuringCleanup()
        {
            // Arrange
            StatKey testStat = 1;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
            var reactionEntity = this.CreateReactionEntity(targetEntity);

            // Set up as previously active (ActivePrevious enabled, Active disabled)
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            // Add 3 ActionStats of the same type (should count as 3 modifiers to remove)
            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 10 },
            });

            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 20 },
            });

            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 30 },
            });

            // Add 5 modifiers from the reaction, but only 3 should be removed
            var targetModifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            for (int i = 0; i < 5; i++)
            {
                targetModifiers.Add(new StatModifiers
                {
                    SourceEntity = reactionEntity,
                    Value = new StatModifier
                    {
                        Type = testStat,
                        Value = 10 + (i * 5),
                        ModifyType = StatModifyType.Added,
                    },
                });
            }

            // Act
            this.RunSystem(this.system);

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(2, modifiers.Length, "Only 3 modifiers should be removed based on ActionStat count, leaving 2");
            foreach (var modifier in modifiers)
            {
                Assert.AreEqual(reactionEntity, modifier.SourceEntity, "Remaining modifiers should still be from the same reaction");
            }
        }

        [Test]
        public void MultipleReactions_IndependentCleanup()
        {
            // Arrange
            StatKey testStat = 1;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));

            // Create two reactions, only one will be deactivated
            var reactionEntity1 = this.CreateReactionEntity(targetEntity);
            var reactionEntity2 = this.CreateReactionEntity(targetEntity);

            // Set reaction1 as deactivated (ActivePrevious enabled, Active disabled)
            this.Manager.SetComponentEnabled<Active>(reactionEntity1, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity1, true);

            // Keep reaction2 as active (Active enabled, ActivePrevious enabled)
            this.Manager.SetComponentEnabled<Active>(reactionEntity2, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity2, true);

            // Add ActionStats to both reactions
            var actionStatBuffer1 = this.Manager.AddBuffer<ActionStat>(reactionEntity1);
            actionStatBuffer1.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 25 },
            });

            var actionStatBuffer2 = this.Manager.AddBuffer<ActionStat>(reactionEntity2);
            actionStatBuffer2.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 35 },
            });

            // Add modifiers from both reactions to the target
            var targetModifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            targetModifiers.Add(new StatModifiers
            {
                SourceEntity = reactionEntity1,
                Value = new StatModifier
                {
                    Type = testStat,
                    Value = 25,
                    ModifyType = StatModifyType.Added,
                },
            });

            targetModifiers.Add(new StatModifiers
            {
                SourceEntity = reactionEntity2,
                Value = new StatModifier
                {
                    Type = testStat,
                    Value = 35,
                    ModifyType = StatModifyType.Added,
                },
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(1, modifiers.Length, "Only modifiers from deactivated reaction should be removed");
            Assert.AreEqual(reactionEntity2, modifiers[0].SourceEntity, "Remaining modifier should be from the active reaction");
            Assert.AreEqual(35, modifiers[0].Value.Value, "Remaining modifier should have correct value");
        }

        [Test]
        public void NoTargetEntity_NoStatModifiersBuffer_HandledGracefully()
        {
            // Arrange
            var reactionEntity = this.CreateReactionEntity(); // No target specified

            // Set up as previously active (ActivePrevious enabled, Active disabled)
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = 1,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 50 },
            });

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => this.RunSystem(this.system));
        }

        [Test]
        public void TargetWithCustomReferences_ResolvesCorrectly()
        {
            // Arrange
            StatKey testStat = 1;
            var customTargetEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
            var reactionEntity = this.CreateReactionEntity();

            // Set up as previously active (ActivePrevious enabled, Active disabled)
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            // Add TargetsCustom for custom target resolution
            this.Manager.AddComponentData(reactionEntity, new TargetsCustom
            {
                Target0 = customTargetEntity,
            });

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Custom0,
                Fixed = new ActionStat.ValueUnion { Int = 40 },
            });

            // Add modifier from the reaction to the custom target
            var targetModifiers = this.Manager.GetBuffer<StatModifiers>(customTargetEntity);
            targetModifiers.Add(new StatModifiers
            {
                SourceEntity = reactionEntity,
                Value = new StatModifier
                {
                    Type = testStat,
                    Value = 40,
                    ModifyType = StatModifyType.Added,
                },
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(customTargetEntity);
            Assert.AreEqual(0, modifiers.Length, "Custom target modifier should be removed");
        }

        [Test]
        public void NewlyActive_NotPreviouslyActive_IgnoredBySystem()
        {
            // Arrange
            StatKey testStat = 1;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
            var reactionEntity = this.CreateReactionEntity(targetEntity);

            // Set up as newly activated (Active enabled, ActivePrevious disabled) - should be ignored
            this.Manager.SetComponentEnabled<Active>(reactionEntity, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 50 },
            });

            // Add a modifier from the reaction
            var targetModifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            targetModifiers.Add(new StatModifiers
            {
                SourceEntity = reactionEntity,
                Value = new StatModifier
                {
                    Type = testStat,
                    Value = 50,
                    ModifyType = StatModifyType.Added,
                },
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(1, modifiers.Length, "Newly activated reactions should not have modifiers removed");
        }

        [Test]
        public void ActiveAndPreviouslyActive_IgnoredBySystem()
        {
            // Arrange
            StatKey testStat = 1;
            var targetEntity = this.CreateStatEntity(CreateStatModifier(testStat, 100, StatModifyType.Added));
            var reactionEntity = this.CreateReactionEntity(targetEntity);

            // Set up as continuously active (Active enabled, ActivePrevious enabled) - should be ignored
            this.Manager.SetComponentEnabled<Active>(reactionEntity, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(reactionEntity);
            actionStatBuffer.Add(new ActionStat
            {
                Type = testStat,
                ModifyType = StatModifyType.Added,
                ValueType = StatValueType.Fixed,
                Target = Target.Target,
                Fixed = new ActionStat.ValueUnion { Int = 50 },
            });

            // Add a modifier from the reaction
            var targetModifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            targetModifiers.Add(new StatModifiers
            {
                SourceEntity = reactionEntity,
                Value = new StatModifier
                {
                    Type = testStat,
                    Value = 50,
                    ModifyType = StatModifyType.Added,
                },
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var modifiers = this.Manager.GetBuffer<StatModifiers>(targetEntity);
            Assert.AreEqual(1, modifiers.Length, "Continuously active reactions should not have modifiers removed");
        }
    }
}
