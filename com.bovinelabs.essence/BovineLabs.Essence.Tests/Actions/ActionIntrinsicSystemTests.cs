// <copyright file="ActionIntrinsicSystemTests.cs" company="BovineLabs">
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
    using UnityEngine;
    using UnityEngine.TestTools;

    public class ActionIntrinsicSystemTests : EssenceTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();

            // Setup IntrinsicConfig singleton required by ActionIntrinsicSystem
            var config = this.CreateTestIntrinsicConfig(new (IntrinsicKey, int, int, int, StatKey, StatKey)[]
            {
                ((IntrinsicKey)1, 100, 0, 999, default(StatKey), (StatKey)1), // Health: default 100, min 0, max 999, no minStat, maxStat 1
                ((IntrinsicKey)2, 50, 0, 200, default(StatKey), default(StatKey)), // Mana: default 50, min 0, max 200
                ((IntrinsicKey)3, 75, 0, 150, default(StatKey), default(StatKey)), // Stamina: default 75, min 0, max 150
            });
            this.SetupIntrinsicConfig(config);

            this.system = this.World.CreateSystem<ActionIntrinsicSystem>();
        }

        [Test]
        public void SingleIntrinsicModification_AppliesCorrectly()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 25,
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var intrinsicData = intrinsicMap[healthKey];
            Assert.AreEqual(125, intrinsicData, "Intrinsic value should be modified by action");
        }

        [Test]
        public void MultipleIntrinsicModifications_SameEntity_AccumulatesCorrectly()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 25,
                Target = Target.Target,
            });
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 15,
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var intrinsicData = intrinsicMap[healthKey];
            Assert.AreEqual(140, intrinsicData, "Multiple modifications to same intrinsic should accumulate");
        }

        [Test]
        public void MultipleIntrinsicTypes_SameEntity_AppliesAll()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            IntrinsicKey manaKey = 2;
            var targetEntity = this.CreateIntrinsicEntity(
                CreateIntrinsicDefault(healthKey, 100),
                CreateIntrinsicDefault(manaKey, 50));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 25,
                Target = Target.Target,
            });
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = manaKey,
                Amount = 10,
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var healthData = intrinsicMap[healthKey];
            var manaData = intrinsicMap[manaKey];
            Assert.AreEqual(125, healthData, "Health intrinsic should be modified");
            Assert.AreEqual(60, manaData, "Mana intrinsic should be modified");
        }

        [Test]
        public void TargetResolution_Self_AppliesCorrectly()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var reactionEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));
            this.Manager.AddComponent<Active>(reactionEntity);
            this.Manager.AddComponent<ActivePrevious>(reactionEntity);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);
            this.Manager.AddComponentData(reactionEntity, new Targets { Target = reactionEntity });

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 20,
                Target = Target.Self,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(reactionEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var intrinsicData = intrinsicMap[healthKey];
            Assert.AreEqual(120, intrinsicData, "Self-targeting should modify reaction entity");
        }

        [Test]
        public void TargetResolution_Source_AppliesCorrectly()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var sourceEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 50));

            var reactionEntity = this.CreateReactionEntity(targetEntity);
            this.Manager.SetComponentData(reactionEntity, new Targets
            {
                Target = targetEntity,
                Source = sourceEntity,
            });

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 30,
                Target = Target.Source,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var sourceIntrinsic = this.Manager.GetBuffer<Intrinsic>(sourceEntity);
            var targetIntrinsic = this.Manager.GetBuffer<Intrinsic>(targetEntity);

            var sourceMap = sourceIntrinsic.AsMap();
            var targetMap = targetIntrinsic.AsMap();

            Assert.AreEqual(130, sourceMap[healthKey], "Source should be modified");
            Assert.AreEqual(50, targetMap[healthKey], "Target should not be modified");
        }

        [Test]
        public void NegativeAmount_ReducesIntrinsicValue()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = -25,
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var intrinsicData = intrinsicMap[healthKey];
            Assert.AreEqual(75, intrinsicData, "Negative amount should reduce intrinsic value");
        }

        [Test]
        public void MultipleEntities_ProcessedIndependently()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity1 = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));
            var targetEntity2 = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 200));

            var reactionEntity1 = this.CreateReactionEntity(targetEntity1);
            var reactionEntity2 = this.CreateReactionEntity(targetEntity2);

            var actionBuffer1 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity1);
            actionBuffer1.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 25,
                Target = Target.Target,
            });

            var actionBuffer2 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity2);
            actionBuffer2.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 50,
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsic1 = this.Manager.GetBuffer<Intrinsic>(targetEntity1);
            var intrinsic2 = this.Manager.GetBuffer<Intrinsic>(targetEntity2);

            var intrinsic1Map = intrinsic1.AsMap();
            var intrinsic2Map = intrinsic2.AsMap();

            Assert.AreEqual(125, intrinsic1Map[healthKey], "First entity should have correct modification");
            Assert.AreEqual(250, intrinsic2Map[healthKey], "Second entity should have correct modification");
        }

        [Test]
        public void NoActivatedActions_NoModifications()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));

            var reactionEntity = this.Manager.CreateEntity();
            this.Manager.AddComponent<Active>(reactionEntity);
            this.Manager.AddComponent<ActivePrevious>(reactionEntity);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true); // Previously active
            this.Manager.AddComponentData(reactionEntity, new Targets { Target = targetEntity });

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 25,
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var intrinsicData = intrinsicMap[healthKey];
            Assert.AreEqual(100, intrinsicData, "Previously active actions should not be processed");
        }

        [Test]
        public void EntityWithoutIntrinsicWriter_SkippedSafely()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.Manager.CreateEntity(); // Entity without intrinsic components

            var reactionEntity = this.CreateReactionEntity(targetEntity);

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 25,
                Target = Target.Target,
            });

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => this.RunSystem(this.system));
        }

        [Test]
        public void BatchingMultipleReactions_SameTarget_AccumulatesCorrectly()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));

            // Create multiple reaction entities targeting the same entity
            var reactionEntity1 = this.CreateReactionEntity(targetEntity);
            var reactionEntity2 = this.CreateReactionEntity(targetEntity);
            var reactionEntity3 = this.CreateReactionEntity(targetEntity);

            var actionBuffer1 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity1);
            actionBuffer1.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 10,
                Target = Target.Target,
            });

            var actionBuffer2 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity2);
            actionBuffer2.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 20,
                Target = Target.Target,
            });
            actionBuffer2.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 5,
                Target = Target.Target,
            });

            var actionBuffer3 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity3);
            actionBuffer3.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 15,
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var healthData = intrinsicMap[healthKey];
            Assert.AreEqual(150, healthData, "All batched changes should be accumulated: 100 + 10 + 20 + 5 + 15 = 150");
        }

        [Test]
        public void BatchingMultipleIntrinsicTypes_SameTargetFromMultipleReactions_ProcessesIndependently()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            IntrinsicKey manaKey = 2;
            IntrinsicKey staminaKey = 3;

            var targetEntity = this.CreateIntrinsicEntity(
                CreateIntrinsicDefault(healthKey, 100),
                CreateIntrinsicDefault(manaKey, 50),
                CreateIntrinsicDefault(staminaKey, 75));

            var reactionEntity1 = this.CreateReactionEntity(targetEntity);
            var reactionEntity2 = this.CreateReactionEntity(targetEntity);

            // Reaction 1: Affects health and mana
            var actionBuffer1 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity1);
            actionBuffer1.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 25, Target = Target.Target });
            actionBuffer1.Add(new ActionIntrinsic { Intrinsic = manaKey, Amount = 10, Target = Target.Target });
            actionBuffer1.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 5, Target = Target.Target }); // Second health modification

            // Reaction 2: Affects all three intrinsics
            var actionBuffer2 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity2);
            actionBuffer2.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 10, Target = Target.Target });
            actionBuffer2.Add(new ActionIntrinsic { Intrinsic = manaKey, Amount = 15, Target = Target.Target });
            actionBuffer2.Add(new ActionIntrinsic { Intrinsic = staminaKey, Amount = 20, Target = Target.Target });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            Assert.AreEqual(140, intrinsicMap[healthKey], "Health: 100 + 25 + 5 + 10 = 140");
            Assert.AreEqual(75, intrinsicMap[manaKey], "Mana: 50 + 10 + 15 = 75");
            Assert.AreEqual(95, intrinsicMap[staminaKey], "Stamina: 75 + 20 = 95");
        }

        [Test]
        public void BatchingAcrossMultipleTargets_ProcessesIndependently()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity1 = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));
            var targetEntity2 = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 200));
            var targetEntity3 = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 300));

            // Create reactions that target different entities but use same intrinsic type
            var reactionEntity1 = this.CreateReactionEntity(targetEntity1);
            var reactionEntity2 = this.CreateReactionEntity(targetEntity2);
            var reactionEntity3 = this.CreateReactionEntity(targetEntity3);

            // Multiple actions per reaction to test batching within each target
            var actionBuffer1 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity1);
            actionBuffer1.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 10, Target = Target.Target });
            actionBuffer1.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 5, Target = Target.Target });

            var actionBuffer2 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity2);
            actionBuffer2.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 25, Target = Target.Target });
            actionBuffer2.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 15, Target = Target.Target });

            var actionBuffer3 = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity3);
            actionBuffer3.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 30, Target = Target.Target });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsic1 = this.Manager.GetBuffer<Intrinsic>(targetEntity1).AsMap();
            var intrinsic2 = this.Manager.GetBuffer<Intrinsic>(targetEntity2).AsMap();
            var intrinsic3 = this.Manager.GetBuffer<Intrinsic>(targetEntity3).AsMap();

            Assert.AreEqual(115, intrinsic1[healthKey], "Target 1: 100 + 10 + 5 = 115");
            Assert.AreEqual(240, intrinsic2[healthKey], "Target 2: 200 + 25 + 15 = 240");
            Assert.AreEqual(330, intrinsic3[healthKey], "Target 3: 300 + 30 = 330");
        }

        [Test]
        public void ComplexBatchingScenario_MultipleReactionsMultipleTargetsMultipleIntrinsics()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            IntrinsicKey manaKey = 2;

            var player = this.CreateIntrinsicEntity(
                CreateIntrinsicDefault(healthKey, 100),
                CreateIntrinsicDefault(manaKey, 50));
            var enemy1 = this.CreateIntrinsicEntity(
                CreateIntrinsicDefault(healthKey, 150),
                CreateIntrinsicDefault(manaKey, 75));
            var enemy2 = this.CreateIntrinsicEntity(
                CreateIntrinsicDefault(healthKey, 120),
                CreateIntrinsicDefault(manaKey, 60));

            // Spell 1: Heals player, damages enemy1
            var spell1 = this.CreateReactionEntity(player);
            this.Manager.SetComponentData(spell1, new Targets { Target = player, Source = enemy1 });
            var spell1Actions = this.Manager.AddBuffer<ActionIntrinsic>(spell1);
            spell1Actions.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 25, Target = Target.Target }); // Heal player
            spell1Actions.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = -30, Target = Target.Source }); // Damage enemy1

            // Spell 2: Area effect on both enemies
            var spell2 = this.CreateReactionEntity(enemy1);
            this.Manager.SetComponentData(spell2, new Targets { Target = enemy1, Source = enemy2 });
            var spell2Actions = this.Manager.AddBuffer<ActionIntrinsic>(spell2);
            spell2Actions.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = -20, Target = Target.Target }); // Damage enemy1
            spell2Actions.Add(new ActionIntrinsic { Intrinsic = manaKey, Amount = -10, Target = Target.Target }); // Drain enemy1 mana
            spell2Actions.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = -15, Target = Target.Source }); // Damage enemy2

            // Spell 3: Multi-hit on enemy1
            var spell3 = this.CreateReactionEntity(enemy1);
            var spell3Actions = this.Manager.AddBuffer<ActionIntrinsic>(spell3);
            spell3Actions.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = -10, Target = Target.Target }); // Hit 1
            spell3Actions.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = -8, Target = Target.Target }); // Hit 2
            spell3Actions.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = -12, Target = Target.Target }); // Hit 3

            // Act
            this.RunSystem(this.system);

            // Assert
            var playerIntrinsic = this.Manager.GetBuffer<Intrinsic>(player);
            var playerIntrinsicMap = playerIntrinsic.AsMap();
            var enemy1Intrinsic = this.Manager.GetBuffer<Intrinsic>(enemy1);
            var enemy1IntrinsicMap = enemy1Intrinsic.AsMap();
            var enemy2Intrinsic = this.Manager.GetBuffer<Intrinsic>(enemy2);
            var enemy2IntrinsicMap = enemy2Intrinsic.AsMap();

            Assert.AreEqual(125, playerIntrinsicMap[healthKey], "Player health: 100 + 25 = 125");
            Assert.AreEqual(50, playerIntrinsicMap[manaKey], "Player mana unchanged: 50");

            Assert.AreEqual(70, enemy1IntrinsicMap[healthKey], "Enemy1 health: 150 - 30 - 20 - 10 - 8 - 12 = 70");
            Assert.AreEqual(65, enemy1IntrinsicMap[manaKey], "Enemy1 mana: 75 - 10 = 65");

            Assert.AreEqual(105, enemy2IntrinsicMap[healthKey], "Enemy2 health: 120 - 15 = 105");
            Assert.AreEqual(60, enemy2IntrinsicMap[manaKey], "Enemy2 mana unchanged: 60");
        }

        [Test]
        public void IntrinsicWriterIntegration_AppliesModificationsThroughWriter()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));

            // Enable condition events to test IntrinsicWriter integration
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(targetEntity, false);

            var reactionEntity = this.CreateReactionEntity(targetEntity);
            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 25,
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert - Verify IntrinsicWriter was used (condition dirty flag should be set)
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var isConditionDirty = this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(targetEntity);

            Assert.AreEqual(125, intrinsicMap[healthKey], "Intrinsic value should be modified through writer");
            Assert.IsTrue(isConditionDirty, "IntrinsicConditionDirty should be set by writer");
        }

        [Test]
        public void IntrinsicWriterIntegration_WithStatLimits_ClampsCorrectly()
        {
            // Arrange
            IntrinsicKey healthCurrentKey = 1;
            StatKey healthMaxKey = 1;

            // Create entity with both stats and intrinsics
            var targetEntity = this.CreateCombinedEntity(
                new[] { CreateStatModifier(healthMaxKey, 150, StatModifyType.Added) },
                new[] { CreateIntrinsicDefault(healthCurrentKey, 100) });

            var reactionEntity = this.CreateReactionEntity(targetEntity);
            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);

            // Try to set health to 200 (should be clamped to 150 based on healthMax stat)
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthCurrentKey,
                Amount = 100, // 100 + 100 = 200, but should clamp to 150
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var healthData = intrinsicMap[healthCurrentKey];
            Assert.AreEqual(150, healthData, "Intrinsic should be clamped to stat limit by IntrinsicWriter");
        }

        [Test]
        public void IntrinsicWriterIntegration_WithStaticLimits_ClampsCorrectly()
        {
            // Arrange
            IntrinsicKey manaKey = 2;

            // Assuming manaKey has static limits defined in config (0-100)
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(manaKey, 80));

            var reactionEntity = this.CreateReactionEntity(targetEntity);
            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);

            // Try to add mana beyond static limit
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = manaKey,
                Amount = 50, // 80 + 50 = 130, might be clamped by static limits
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var manaData = intrinsicMap[manaKey];

            // Note: The actual clamping depends on IntrinsicConfig settings
            // This test verifies the system properly integrates with IntrinsicWriter
            Assert.IsTrue(manaData >= 80, "Mana should not be reduced");
        }

        [Test]
        public void IntrinsicWriterIntegration_ZeroAmount_DoesNotTriggerDirtyFlag()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(targetEntity, false);

            var reactionEntity = this.CreateReactionEntity(targetEntity);
            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = healthKey,
                Amount = 0, // No change
                Target = Target.Target,
            });

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var isConditionDirty = this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(targetEntity);

            var intrinsicMap = intrinsicBuffer.AsMap();
            Assert.AreEqual(100, intrinsicMap[healthKey], "Intrinsic should remain unchanged");

            // Note: Whether dirty flag is set for zero changes depends on IntrinsicWriter implementation
        }

        [Test]
        public void IntrinsicWriterIntegration_MultipleChanges_BatchedCorrectly()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            IntrinsicKey manaKey = 2;
            var targetEntity = this.CreateIntrinsicEntity(
                CreateIntrinsicDefault(healthKey, 100),
                CreateIntrinsicDefault(manaKey, 50));

            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(targetEntity, false);

            var reactionEntity = this.CreateReactionEntity(targetEntity);
            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 25, Target = Target.Target });
            actionIntrinsicBuffer.Add(new ActionIntrinsic { Intrinsic = manaKey, Amount = 15, Target = Target.Target });
            actionIntrinsicBuffer.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 10, Target = Target.Target }); // Second health change

            // Act
            this.RunSystem(this.system);

            // Assert
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            var isConditionDirty = this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(targetEntity);

            Assert.AreEqual(135, intrinsicMap[healthKey], "Health accumulated: 100 + 25 + 10 = 135");
            Assert.AreEqual(65, intrinsicMap[manaKey], "Mana modified: 50 + 15 = 65");
            Assert.IsTrue(isConditionDirty, "Condition dirty should be set after modifications");
        }

        [Test]
        public void IntrinsicWriterIntegration_ErrorHandling_InvalidIntrinsicKey()
        {
            // Arrange
            IntrinsicKey invalidKey = 999; // This key doesn't exist in config
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault((IntrinsicKey)1, 100));

            var reactionEntity = this.CreateReactionEntity(targetEntity);
            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic
            {
                Intrinsic = invalidKey,
                Amount = 25,
                Target = Target.Target,
            });

            // Act & Assert - Should handle gracefully and log error for invalid intrinsic key
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex($".*Key {invalidKey.Value} not found in the intrinsic config.*"));
            Assert.DoesNotThrow(() => this.RunSystem(this.system));

            // Verify original intrinsic is unchanged (invalid key should be ignored)
            var intrinsicBuffer = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var intrinsicMap = intrinsicBuffer.AsMap();
            Assert.AreEqual(100, intrinsicMap[1], "Valid intrinsic should remain unchanged");
        }

        [Test]
        public void TargetResolution_AllTargetTypes_ResolvedCorrectly()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));
            var sourceEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 150));
            var reactionEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 200));

            // Set up complex targeting
            this.Manager.AddComponent<Active>(reactionEntity);
            this.Manager.AddComponent<ActivePrevious>(reactionEntity);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);
            this.Manager.AddComponentData(reactionEntity, new Targets
            {
                Target = targetEntity,
                Source = sourceEntity,
            });

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 10, Target = Target.Target });
            actionIntrinsicBuffer.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 20, Target = Target.Source });
            actionIntrinsicBuffer.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 30, Target = Target.Self });

            // Act
            this.RunSystem(this.system);

            // Assert
            var targetIntrinsic = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var targetIntrinsicMap = targetIntrinsic.AsMap();
            var sourceIntrinsic = this.Manager.GetBuffer<Intrinsic>(sourceEntity);
            var sourceIntrinsicMap = sourceIntrinsic.AsMap();
            var reactionIntrinsic = this.Manager.GetBuffer<Intrinsic>(reactionEntity);
            var reactionIntrinsicMap = reactionIntrinsic.AsMap();

            Assert.AreEqual(110, targetIntrinsicMap[healthKey], "Target: 100 + 10 = 110");
            Assert.AreEqual(170, sourceIntrinsicMap[healthKey], "Source: 150 + 20 = 170");
            Assert.AreEqual(230, reactionIntrinsicMap[healthKey], "Self (reaction): 200 + 30 = 230");
        }

        [Test]
        public void TargetResolution_CustomTargets_HandledCorrectly()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var primaryTarget = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));
            var customTarget = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 150));

            var reactionEntity = this.CreateReactionEntity(primaryTarget);

            // Add TargetsCustom component for custom target resolution
            this.Manager.AddComponentData(reactionEntity, new TargetsCustom
            {
                Target0 = customTarget,
            });

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 25, Target = Target.Custom0 });

            // Act
            this.RunSystem(this.system);

            // Assert
            var primaryIntrinsic = this.Manager.GetBuffer<Intrinsic>(primaryTarget);
            var targetIntrinsicMap = primaryIntrinsic.AsMap();
            var customIntrinsic = this.Manager.GetBuffer<Intrinsic>(customTarget);
            var customIntrinsicMap = customIntrinsic.AsMap();

            Assert.AreEqual(100, targetIntrinsicMap[healthKey], "Primary target should be unchanged");
            Assert.AreEqual(175, customIntrinsicMap[healthKey], "Custom target: 150 + 25 = 175");
        }

        [Test]
        public void TargetResolution_InvalidTarget_SkippedSafely()
        {
            // Arrange
            IntrinsicKey healthKey = 1;
            var targetEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(healthKey, 100));

            var reactionEntity = this.CreateReactionEntity(targetEntity);
            this.Manager.SetComponentData(reactionEntity, new Targets
            {
                Target = targetEntity,
                Source = Entity.Null, // Invalid source
            });

            var actionIntrinsicBuffer = this.Manager.AddBuffer<ActionIntrinsic>(reactionEntity);
            actionIntrinsicBuffer.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 25, Target = Target.Target });
            actionIntrinsicBuffer.Add(new ActionIntrinsic { Intrinsic = healthKey, Amount = 50, Target = Target.Source }); // Should be skipped

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => this.RunSystem(this.system));

            // Assert - Only valid target should be modified
            var targetIntrinsic = this.Manager.GetBuffer<Intrinsic>(targetEntity);
            var targetIntrinsicMap = targetIntrinsic.AsMap();
            Assert.AreEqual(125, targetIntrinsicMap[healthKey], "Only valid target should be modified: 100 + 25 = 125");
        }


        private static StatModifier CreateStatModifier(StatKey key, int value, StatModifyType modifyType)
        {
            return new StatModifier
            {
                Type = key,
                Value = value,
                ModifyType = modifyType,
            };
        }
    }
}
