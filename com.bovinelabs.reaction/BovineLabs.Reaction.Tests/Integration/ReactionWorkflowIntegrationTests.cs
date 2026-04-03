// <copyright file="ReactionWorkflowIntegrationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Integration
{
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Actions;
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Core;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Builders;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Groups;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Transforms;

    /// <summary>
    /// Integration tests for complete reaction workflows that combine condition evaluation,
    /// active state management, and action execution.
    /// </summary>
    /// <remarks>
    /// These integration tests use a complete system group setup that mirrors the production
    /// environment, ensuring realistic testing of system interactions and dependencies.
    /// </remarks>
    public class ReactionWorkflowIntegrationTests : ReactionTestFixture
    {
        private NativeHashMap<ObjectId, Entity> objectMap;
        private SystemHandle instantiateCommandBufferSystem;
        private SystemHandle initializeSystemGroup;
        private SystemHandle simulationSystemGroup;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();

            var singletonEntity = this.Manager.CreateSingletonBuffer<ReactionEnableables>();
            var enableablesBuffer = this.Manager.GetBuffer<ReactionEnableables>(singletonEntity).Initialize().AsMap();

            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();

            enableablesBuffer.Add(testTypeHash);

            // Register the prefab in the ObjectDefinitionRegistry (initially empty)
            this.objectMap = this.SetupObjectRegistry();

            // Setup complete system group hierarchy exactly as in production
            this.SetupSystemsAndGroups();
        }

        public override void TearDown()
        {
            base.TearDown();

            this.objectMap.Dispose();
        }

        [Test]
        public void ConditionToActiveToTagAction_CompleteWorkflow_ExecutesCorrectly()
        {
            // Arrange: Create a reaction entity that will activate when condition 0 is true
            // and add a tag component when active
            var reactionEntity = this.CreateReactionEntity();

            // Set up condition: entity activates when condition 0 is true
            var conditions = new BitArray32(0b00000001); // Condition 0 is true, others false
            var correctedConditions = CorrectUnusedConditions(conditions, 0b00000001); // Only condition 0 is used
            this.Manager.AddComponentData(reactionEntity, new ConditionActive { Value = correctedConditions });
            this.Manager.AddComponent<ConditionAllActive>(reactionEntity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reactionEntity, false);

            // Set up action: add a test tag component when active
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Initially the reaction should not be active
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            // Act: Run complete world systems exactly as in production
            this.UpdateCompleteWorldSystems();

            // Assert: Verify the complete workflow executed correctly

            // Check condition was evaluated correctly
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be enabled when condition 0 is true");

            // Check active state was set correctly
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be enabled when conditions are met");

            // Check action was executed - tag component should be added
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should be added when reaction is active");
        }

        [Test]
        public void ConditionToActiveToTagAction_DeactivationWorkflow_CleansUpCorrectly()
        {
            // Arrange: Create a reaction entity that activates when condition 0 is true
            var reactionEntity = this.CreateReactionEntity();

            // Set up condition: entity activates when condition 0 is true
            var conditions = new BitArray32(0b00000001); // Condition 0 is true
            var correctedConditions = CorrectUnusedConditions(conditions, 0b00000001);
            this.Manager.AddComponentData(reactionEntity, new ConditionActive { Value = correctedConditions });
            this.Manager.AddComponent<ConditionAllActive>(reactionEntity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reactionEntity, false);

            // Set up action: add a test tag component when active
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Initially not active
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            // Act: Activate reaction using complete world systems
            this.UpdateCompleteWorldSystems();

            // Verify activation worked
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reactionEntity), "Should be active");
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reactionEntity), "Tag should be added");

            // Act: Change condition to deactivate reaction (condition 0 becomes false)
            var deactivateConditions = new BitArray32(0b00000000); // All conditions false
            var correctedDeactivateConditions = CorrectUnusedConditions(deactivateConditions, 0b00000001);
            this.Manager.SetComponentData(reactionEntity, new ConditionActive { Value = correctedDeactivateConditions });

            // Update active previous state for next frame
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            // Run complete world systems to process deactivation
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be deactivated and tag cleaned up
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be disabled when conditions are not met");
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should be removed when reaction is deactivated");
        }

        [Test]
        public void MultipleConditionsAND_AllConditionsTrue_ActivatesReaction()
        {
            // Arrange: Create a reaction that activates when conditions 0 AND 1 are both true
            var reactionEntity = this.CreateReactionEntity();

            // Set up conditions: both condition 0 and 1 must be true (AND logic)
            var conditions = new BitArray32(0b00000011); // Conditions 0 and 1 are true
            var correctedConditions = CorrectUnusedConditions(conditions, 0b00000011); // Only conditions 0,1 are used
            this.Manager.AddComponentData(reactionEntity, new ConditionActive { Value = correctedConditions });
            this.Manager.AddComponent<ConditionAllActive>(reactionEntity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reactionEntity, false);

            // Set up action: add a test tag component when active
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Initially not active
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            // Act: Run systems with both conditions true
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be active and tag should be added
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be enabled when both conditions are true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be enabled when all required conditions are met");
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should be added when reaction is active");

            // Act: Change conditions so only condition 0 is true (condition 1 becomes false)
            var partialConditions = new BitArray32(0b00000001); // Only condition 0 is true
            var correctedPartialConditions = CorrectUnusedConditions(partialConditions, 0b00000011);
            this.Manager.SetComponentData(reactionEntity, new ConditionActive { Value = correctedPartialConditions });
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            // Run systems again
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should deactivate because not ALL conditions are met
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be disabled when not all conditions are true");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be disabled when not all required conditions are met");
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should be removed when reaction deactivates");
        }

        [Test]
        public void ConditionWithChance_100Percent_AlwaysActivatesReaction()
        {
            // Arrange: Create a reaction with 100% chance that activates when condition 0 is true
            var reactionEntity = this.CreateReactionEntity();

            // Set up condition: entity activates when condition 0 is true with 100% chance
            var conditions = new BitArray32(0b00000001); // Condition 0 is true
            var correctedConditions = CorrectUnusedConditions(conditions, 0b00000001);
            this.Manager.AddComponentData(reactionEntity, new ConditionActive { Value = correctedConditions });
            this.Manager.AddComponentData(reactionEntity, new ConditionChance { Value = 10000 }); // 100% chance
            this.Manager.AddComponent<ConditionAllActive>(reactionEntity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reactionEntity, false);

            // Set up action: add a test tag component when active
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Initially not active
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            // Act: Run systems with condition true and 100% chance (should always activate)
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be active because chance is 100%
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be enabled with 100% chance when condition is true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be enabled with 100% chance when condition is met");
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should be added when reaction with 100% chance is active");

            // Act: Change condition to false and test that chance doesn't apply
            var falseConditions = new BitArray32(0b00000000); // All conditions false
            var correctedFalseConditions = CorrectUnusedConditions(falseConditions, 0b00000001);
            this.Manager.SetComponentData(reactionEntity, new ConditionActive { Value = correctedFalseConditions });
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            // Run systems to process deactivation
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should deactivate regardless of chance when conditions aren't met
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be disabled when conditions are false, regardless of chance");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be disabled when conditions are false, regardless of chance");
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should be removed when reaction deactivates");
        }

        [Test]
        public void ConditionWithChance_0Percent_NeverActivatesReaction()
        {
            // Arrange: Create a reaction with 0% chance that would activate when condition 0 is true
            var reactionEntity = this.CreateReactionEntity();

            // Set up condition: entity would activate when condition 0 is true but with 0% chance
            var conditions = new BitArray32(0b00000001); // Condition 0 is true
            var correctedConditions = CorrectUnusedConditions(conditions, 0b00000001);
            this.Manager.AddComponentData(reactionEntity, new ConditionActive { Value = correctedConditions });
            this.Manager.AddComponentData(reactionEntity, new ConditionChance { Value = 0 }); // 0% chance
            this.Manager.AddComponent<ConditionAllActive>(reactionEntity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reactionEntity, false);

            // Set up action: add a test tag component when active
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Initially not active
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            // Act: Run systems with condition true but 0% chance (should never activate)
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should not be active because chance is 0%
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be disabled with 0% chance even when condition is true");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be disabled with 0% chance even when condition is met");
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should not be added when reaction with 0% chance doesn't activate");
        }

        [Test]
        public void MultipleReactions_TargetingSameEntity_WorkCorrectly()
        {
            // Arrange: Create two reactions that both target the same entity with different conditions and actions
            var reaction1Entity = this.CreateReactionEntity();
            var reaction2Entity = this.CreateReactionEntity();

            // Set up first reaction: activates when condition 0 is true, adds TestTagComponent
            var conditions1 = new BitArray32(0b00000001); // Condition 0 is true
            var correctedConditions1 = CorrectUnusedConditions(conditions1, 0b00000001);
            this.Manager.AddComponentData(reaction1Entity, new ConditionActive { Value = correctedConditions1 });
            this.Manager.AddComponent<ConditionAllActive>(reaction1Entity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reaction1Entity, false);

            var tagBuffer1 = this.Manager.AddBuffer<ActionTag>(reaction1Entity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer1.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Set up second reaction: activates when condition 1 is true, adds SecondTestTagComponent
            var conditions2 = new BitArray32(0b00000010); // Condition 1 is true
            var correctedConditions2 = CorrectUnusedConditions(conditions2, 0b00000010);
            this.Manager.AddComponentData(reaction2Entity, new ConditionActive { Value = correctedConditions2 });
            this.Manager.AddComponent<ConditionAllActive>(reaction2Entity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reaction2Entity, false);

            var tagBuffer2 = this.Manager.AddBuffer<ActionTag>(reaction2Entity);
            var secondTestTypeHash = GetTestComponentTypeHash<SecondTestTagComponent>();
            tagBuffer2.Add(new ActionTag
            {
                Value = secondTestTypeHash,
                Target = Target.Self,
            });

            // Initially both reactions not active
            this.Manager.SetComponentEnabled<Active>(reaction1Entity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1Entity, false);
            this.Manager.SetComponentEnabled<Active>(reaction2Entity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2Entity, false);

            // Act: Activate both conditions simultaneously
            var bothConditions1 = new BitArray32(0b00000011); // Both conditions 0 and 1 are true
            var correctedBothConditions1 = CorrectUnusedConditions(bothConditions1, 0b00000001);
            var correctedBothConditions2 = CorrectUnusedConditions(bothConditions1, 0b00000010);
            this.Manager.SetComponentData(reaction1Entity, new ConditionActive { Value = correctedBothConditions1 });
            this.Manager.SetComponentData(reaction2Entity, new ConditionActive { Value = correctedBothConditions2 });

            // Run systems to process both reactions
            this.UpdateCompleteWorldSystems();

            // Assert: Both reactions should be active and both tags should be added to their respective entities
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reaction1Entity),
                "First reaction ConditionAllActive should be enabled when condition 0 is true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reaction1Entity),
                "First reaction should be active when its condition is met");
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reaction1Entity),
                "TestTagComponent should be added by first reaction");

            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reaction2Entity),
                "Second reaction ConditionAllActive should be enabled when condition 1 is true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reaction2Entity),
                "Second reaction should be active when its condition is met");
            Assert.IsTrue(this.Manager.HasComponent<SecondTestTagComponent>(reaction2Entity),
                "SecondTestTagComponent should be added by second reaction");

            // Act: Deactivate only condition 0 (keeping condition 1 active)
            var partialConditions = new BitArray32(0b00000010); // Only condition 1 is true
            var correctedPartial1 = CorrectUnusedConditions(partialConditions, 0b00000001); // Should be false for reaction 1
            var correctedPartial2 = CorrectUnusedConditions(partialConditions, 0b00000010); // Should be true for reaction 2
            this.Manager.SetComponentData(reaction1Entity, new ConditionActive { Value = correctedPartial1 });
            this.Manager.SetComponentData(reaction2Entity, new ConditionActive { Value = correctedPartial2 });
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1Entity, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2Entity, true);

            // Run systems again
            this.UpdateCompleteWorldSystems();

            // Assert: First reaction should deactivate, second should remain active
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(reaction1Entity),
                "First reaction ConditionAllActive should be disabled when condition 0 is false");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(reaction1Entity),
                "First reaction should be inactive when its condition is not met");
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reaction1Entity),
                "TestTagComponent should be removed when first reaction deactivates");

            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reaction2Entity),
                "Second reaction ConditionAllActive should remain enabled when condition 1 is true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reaction2Entity),
                "Second reaction should remain active when its condition is still met");
            Assert.IsTrue(this.Manager.HasComponent<SecondTestTagComponent>(reaction2Entity),
                "SecondTestTagComponent should remain when second reaction stays active");
        }

        [Test]
        public void ActionCreateWorkflow_ConditionActivation_InstantiatesEntity()
        {
            // Arrange: Create a reaction that instantiates an entity when condition 0 is true
            var reactionEntity = this.CreateReactionEntity();

            // Set up condition: entity activates when condition 0 is true
            var conditions = new BitArray32(0b00000001); // Condition 0 is true
            var correctedConditions = CorrectUnusedConditions(conditions, 0b00000001);
            this.Manager.AddComponentData(reactionEntity, new ConditionActive { Value = correctedConditions });
            this.Manager.AddComponent<ConditionAllActive>(reactionEntity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reactionEntity, false);

            // Set up ActionCreate: instantiate a test entity when active
            var actionBuffer = this.Manager.AddBuffer<ActionCreate>(reactionEntity);
            var testPrefabId = new ObjectId(1);
            actionBuffer.Add(new ActionCreate
            {
                Id = testPrefabId,
                Target = Target.Self,
                DestroyOnDisabled = true,
            });

            // Set up LinkedEntityGroup and ActionCreated for cleanup tracking
            this.Manager.AddBuffer<LinkedEntityGroup>(reactionEntity);
            this.Manager.AddBuffer<ActionCreated>(reactionEntity);
            var linkedGroup = this.Manager.GetBuffer<LinkedEntityGroup>(reactionEntity);
            linkedGroup.Add(new LinkedEntityGroup { Value = reactionEntity });

            // Create and register test prefab
            var testPrefab = this.Manager.CreateEntity();
            this.Manager.AddComponent<Targets>(testPrefab);
            this.Manager.AddComponent<TestCreatedEntityTag>(testPrefab);
            this.Manager.AddComponent<DestroyEntity>(testPrefab);
            this.Manager.SetComponentEnabled<DestroyEntity>(testPrefab, false);

            // Add to the existing object registry
            this.AddToObjectRegistry(this.objectMap, (testPrefabId, testPrefab));

            // Initially not active
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            // Act: Run systems to activate reaction and create entity
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be active and entity should be instantiated
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be enabled when condition is true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be enabled when condition is met");

            // Check that entity was created
            var actionCreated = this.Manager.GetBuffer<ActionCreated>(reactionEntity);
            Assert.AreEqual(1, actionCreated.Length, "Should have created one entity");

            var createdEntity = actionCreated[0].Value;
            Assert.IsTrue(this.Manager.Exists(createdEntity), "Created entity should exist");
            Assert.IsTrue(this.Manager.HasComponent<Targets>(createdEntity), "Created entity should have Targets component");
            Assert.IsTrue(this.Manager.HasComponent<TestCreatedEntityTag>(createdEntity), "Created entity should have TestCreatedEntityTag component");

            // Verify targets are set correctly
            var targets = this.Manager.GetComponentData<Targets>(createdEntity);
            Assert.AreEqual(reactionEntity, targets.Target, "Target should be reaction entity (Self)");
            Assert.AreEqual(reactionEntity, targets.Owner, "Owner should be reaction entity");
            Assert.AreEqual(reactionEntity, targets.Source, "Source should be reaction entity");

            // Verify LinkedEntityGroup contains created entity
            linkedGroup = this.Manager.GetBuffer<LinkedEntityGroup>(reactionEntity);
            Assert.AreEqual(2, linkedGroup.Length, "LinkedEntityGroup should contain reaction and created entity");
            Assert.AreEqual(createdEntity, linkedGroup[1].Value, "Created entity should be in LinkedEntityGroup");

            // Act: Deactivate condition to test cleanup
            var falseConditions = new BitArray32(0b00000000); // All conditions false
            var correctedFalseConditions = CorrectUnusedConditions(falseConditions, 0b00000001);
            this.Manager.SetComponentData(reactionEntity, new ConditionActive { Value = correctedFalseConditions });
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            // Run cleanup
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be deactivated and created entity should be marked for destruction
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be disabled when condition is false");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be disabled when condition is not met");

            // Check that created entity is marked for destruction
            Assert.IsTrue(this.Manager.HasComponent<DestroyEntity>(createdEntity),
                "Created entity should have DestroyEntity component");
            Assert.IsTrue(this.Manager.IsComponentEnabled<DestroyEntity>(createdEntity),
                "DestroyEntity should be enabled on created entity");

            // Verify cleanup of tracking components
            linkedGroup = this.Manager.GetBuffer<LinkedEntityGroup>(reactionEntity);
            Assert.AreEqual(1, linkedGroup.Length, "LinkedEntityGroup should only contain reaction entity after cleanup");

            actionCreated = this.Manager.GetBuffer<ActionCreated>(reactionEntity);
            Assert.AreEqual(0, actionCreated.Length, "ActionCreated buffer should be empty after cleanup");
        }

        [Test]
        public void ActionEnableableWorkflow_ConditionActivation_EnablesComponent()
        {
            // Arrange: Create a reaction that enables a component when condition 0 is true
            var reactionEntity = this.CreateReactionEntity();

            // Set up condition: entity activates when condition 0 is true
            var conditions = new BitArray32(0b00000001); // Condition 0 is true
            var correctedConditions = CorrectUnusedConditions(conditions, 0b00000001);
            this.Manager.AddComponentData(reactionEntity, new ConditionActive { Value = correctedConditions });
            this.Manager.AddComponent<ConditionAllActive>(reactionEntity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reactionEntity, false);

            // Add test enableable component (initially disabled)
            this.Manager.AddComponent<TestEnableableComponent>(reactionEntity);
            this.Manager.SetComponentEnabled<TestEnableableComponent>(reactionEntity, false);

            // Set up ActionEnableable: enable TestEnableableComponent when active
            var enableableBuffer = this.Manager.AddBuffer<ActionEnableable>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();
            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Use the existing ReactionEnableables singleton
            var singletonEntity = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ReactionEnableables>()
                .Build(this.Manager).GetSingletonEntity();
            var enableablesMap = this.Manager.GetBuffer<ReactionEnableables>(singletonEntity).AsMap();
            enableablesMap.Add(testTypeHash);

            // Initially not active
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            // Act: Run systems to activate reaction and enable component
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be active and component should be enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be enabled when condition is true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be enabled when condition is met");
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(reactionEntity),
                "TestEnableableComponent should be enabled when reaction is active");

            // Act: Deactivate condition to test component disabling
            var falseConditions = new BitArray32(0b00000000); // All conditions false
            var correctedFalseConditions = CorrectUnusedConditions(falseConditions, 0b00000001);
            this.Manager.SetComponentData(reactionEntity, new ConditionActive { Value = correctedFalseConditions });
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            // Run systems to process deactivation
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be deactivated and component should be disabled
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be disabled when condition is false");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be disabled when condition is not met");
            Assert.IsFalse(this.Manager.IsComponentEnabled<TestEnableableComponent>(reactionEntity),
                "TestEnableableComponent should be disabled when reaction is deactivated");
        }

        [Test]
        public void MultipleConditionsOR_AnyConditionTrue_ActivatesReaction()
        {
            // Arrange: Create a reaction that activates when condition 0 OR 1 is true (either one)
            var reactionEntity = this.CreateReactionEntity();

            // Set up composite OR logic: condition 0 OR condition 1
            var conditions = new BitArray32(0b00000001); // Only condition 0 is true initially
            var correctedConditions = CorrectUnusedConditions(conditions, 0b00000011); // Conditions 0,1 are used

            // Create composite logic for OR operation
            var compositeLogic = this.CreateCompositeConditionEntity(correctedConditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.Or)
                                 .Add(0) // condition 0
                                 .Add(1) // condition 1
                                 .EndGroup();
            });

            // Copy composite setup to our reaction entity
            this.Manager.AddComponentData(reactionEntity, this.Manager.GetComponentData<ConditionActive>(compositeLogic));
            this.Manager.AddComponentData(reactionEntity, this.Manager.GetComponentData<ConditionComposite>(compositeLogic));
            this.Manager.AddComponent<ConditionAllActive>(reactionEntity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reactionEntity, false);

            // Clean up the temporary entity
            this.Manager.DestroyEntity(compositeLogic);

            // Set up action: add a test tag component when active
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Initially not active
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            // Act: Run systems with only condition 0 true (should activate with OR logic)
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be active because condition 0 is true (OR logic)
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be enabled when any condition in OR group is true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be enabled when any required condition is met (OR logic)");
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should be added when reaction is active");

            // Act: Change conditions so only condition 1 is true (condition 0 becomes false)
            var switchedConditions = new BitArray32(0b00000010); // Only condition 1 is true
            var correctedSwitchedConditions = CorrectUnusedConditions(switchedConditions, 0b00000011);
            this.Manager.SetComponentData(reactionEntity, new ConditionActive { Value = correctedSwitchedConditions });
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            // Run systems again
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should still be active because condition 1 is now true (OR logic)
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should remain enabled when different condition in OR group becomes true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should remain enabled when any condition in OR group is true");

            // Act: Set both conditions to false
            var noneConditions = new BitArray32(0b00000000); // Both conditions false
            var correctedNoneConditions = CorrectUnusedConditions(noneConditions, 0b00000011);
            this.Manager.SetComponentData(reactionEntity, new ConditionActive { Value = correctedNoneConditions });

            // Run systems to process deactivation
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should deactivate when ALL conditions in OR group are false
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be disabled when all conditions in OR group are false");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be disabled when no conditions in OR group are true");
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should be removed when reaction deactivates");
        }

        [Test]
        public void CrossTargetWorkflow_ReactionAffectsDifferentEntity_ExecutesCorrectly()
        {
            // Arrange: Create a reaction entity that affects a different target entity
            var reactionEntity = this.CreateReactionEntity();
            var targetEntity = this.Manager.CreateEntity();

            // Add required components to target entity
            this.Manager.AddComponent<Targets>(targetEntity);
            var targetTargets = new Targets
            {
                Target = targetEntity,
                Owner = reactionEntity,
                Source = reactionEntity,
            };
            this.Manager.SetComponentData(targetEntity, targetTargets);

            // Set up condition: reaction activates when condition 0 is true
            var conditions = new BitArray32(0b00000001); // Condition 0 is true
            var correctedConditions = CorrectUnusedConditions(conditions, 0b00000001);
            this.Manager.AddComponentData(reactionEntity, new ConditionActive { Value = correctedConditions });
            this.Manager.AddComponent<ConditionAllActive>(reactionEntity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(reactionEntity, false);

            // Set up action: add a test tag component to the TARGET entity (not self)
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Target, // This targets the different entity
            });

            // Set up targets on reaction entity to point to the target entity
            var reactionTargets = new Targets
            {
                Target = targetEntity,
                Owner = reactionEntity,
                Source = reactionEntity,
            };
            this.Manager.SetComponentData(reactionEntity, reactionTargets);

            // Initially the reaction should not be active
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, false);

            // Act: Run systems to activate reaction and execute cross-target action
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be active
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be enabled when condition 0 is true");
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be enabled when conditions are met");

            // Assert: Tag component should be added to the TARGET entity, not the reaction entity
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "TestTagComponent should NOT be added to the reaction entity");
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity),
                "TestTagComponent should be added to the target entity when reaction is active");

            // Act: Deactivate the condition to test cross-target cleanup
            var falseConditions = new BitArray32(0b00000000); // All conditions false
            var correctedFalseConditions = CorrectUnusedConditions(falseConditions, 0b00000001);
            this.Manager.SetComponentData(reactionEntity, new ConditionActive { Value = correctedFalseConditions });
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);

            // Run systems to process deactivation
            this.UpdateCompleteWorldSystems();

            // Assert: Reaction should be deactivated and tag should be removed from target entity
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(reactionEntity),
                "ConditionAllActive should be disabled when conditions are not met");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(reactionEntity),
                "Active should be disabled when conditions are not met");
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(targetEntity),
                "TestTagComponent should be removed from target entity when reaction is deactivated");
        }

        /// <summary>
        /// Sets up the complete system group hierarchy to match the production environment.
        /// Systems are created in dependency order to ensure singletons exist before dependent systems.
        /// </summary>
        private void SetupSystemsAndGroups()
        {
            // Create system group hierarchy
            var simulationSystem = this.World.CreateSystemManaged<SimulationSystemGroup>();
            var transformSystemGroup = this.World.CreateSystemManaged<TransformSystemGroup>();
            var companionGameObjectUpdateTransformSystem = this.World.GetOrCreateSystem<CompanionGameObjectUpdateTransformSystem>();
            var afterTransformSystemGroup = this.World.GetOrCreateSystemManaged<AfterTransformSystemGroup>();

            // Add transform groups to simulation
            simulationSystem.AddSystemToUpdateList(transformSystemGroup);
            simulationSystem.AddSystemToUpdateList(companionGameObjectUpdateTransformSystem);
            simulationSystem.AddSystemToUpdateList(afterTransformSystemGroup);

            // Create reaction system hierarchy
            var reactionSystemGroup = this.World.GetOrCreateSystemManaged<ReactionSystemGroup>();
            afterTransformSystemGroup.AddSystemToUpdateList(reactionSystemGroup);

            // Create and populate system groups in dependency order
            this.SetupConditionSystems(reactionSystemGroup);
            this.SetupActiveSystems(reactionSystemGroup);
            this.SetupActionSystems();
            this.SetupCoreSystems();

            // Sort all system groups to respect dependencies
            simulationSystem.SortSystems();

            this.initializeSystemGroup = this.World.GetOrCreateSystem<InitializationSystemGroup>();
            this.simulationSystemGroup = this.World.GetOrCreateSystem<SimulationSystemGroup>();
            this.instantiateCommandBufferSystem = this.World.GetOrCreateSystem<InstantiateCommandBufferSystem>();
        }

        /// <summary>
        /// Sets up condition processing systems and their groups.
        /// </summary>
        private void SetupConditionSystems(ComponentSystemGroup reactionSystemGroup)
        {
            var conditionsSystemGroup = this.World.GetOrCreateSystemManaged<ConditionsSystemGroup>();
            var globalConditionsSystemGroup = this.World.GetOrCreateSystemManaged<GlobalConditionsSystemGroup>();
            var conditionWriteEventsGroup = this.World.GetOrCreateSystemManaged<ConditionWriteEventsGroup>();

            // Add condition groups to reaction system group
            reactionSystemGroup.AddSystemToUpdateList(conditionsSystemGroup);

            // Add sub-groups to conditions group
            conditionsSystemGroup.AddSystemToUpdateList(globalConditionsSystemGroup);
            conditionsSystemGroup.AddSystemToUpdateList(conditionWriteEventsGroup);

            // Add individual condition systems to their groups
            var conditionAllActiveSystem = this.World.GetOrCreateSystem<ConditionAllActiveSystem>();
            var conditionEventResetSystem = this.World.GetOrCreateSystem<ConditionEventResetSystem>();
            conditionsSystemGroup.AddSystemToUpdateList(conditionAllActiveSystem);
            conditionsSystemGroup.AddSystemToUpdateList(conditionEventResetSystem);

            // Add event write system to its group
            var conditionEventWriteSystem = this.World.GetOrCreateSystem<ConditionEventWriteSystem>();
            conditionWriteEventsGroup.AddSystemToUpdateList(conditionEventWriteSystem);

            // Sort condition groups
            conditionsSystemGroup.SortSystems();
            globalConditionsSystemGroup.SortSystems();
            conditionWriteEventsGroup.SortSystems();
        }

        /// <summary>
        /// Sets up active state management systems and their groups.
        /// </summary>
        private void SetupActiveSystems(ComponentSystemGroup reactionSystemGroup)
        {
            var activeSystemGroup = this.World.GetOrCreateSystemManaged<ActiveSystemGroup>();
            var activeCancelSystemGroup = this.World.GetOrCreateSystemManaged<ActiveCancelSystemGroup>();
            var activeDisabledSystemGroup = this.World.GetOrCreateSystemManaged<ActiveDisabledSystemGroup>();
            var activeEnabledSystemGroup = this.World.GetOrCreateSystemManaged<ActiveEnabledSystemGroup>();
            var timerSystemGroup = this.World.GetOrCreateSystemManaged<TimerSystemGroup>();

            // Add active groups to reaction system group
            reactionSystemGroup.AddSystemToUpdateList(activeSystemGroup);
            reactionSystemGroup.AddSystemToUpdateList(activeDisabledSystemGroup);
            reactionSystemGroup.AddSystemToUpdateList(activeEnabledSystemGroup);

            // Add cancel group to active group
            activeSystemGroup.AddSystemToUpdateList(timerSystemGroup);
            timerSystemGroup.AddSystemToUpdateList(activeCancelSystemGroup);

            // Add cancel active system to cancel group
            var conditionCancelActiveSystem = this.World.GetOrCreateSystem<ConditionCancelActiveSystem>();
            activeCancelSystemGroup.AddSystemToUpdateList(conditionCancelActiveSystem);

            // Add individual active systems to active group
            var activePreviousSystem = this.World.GetOrCreateSystem<ActivePreviousSystem>();
            var activeSystem = this.World.GetOrCreateSystem<ActiveSystem>();
            var activeTriggerSystem = this.World.GetOrCreateSystem<ActiveTriggerSystem>();
            var activeDurationSystem = this.World.GetOrCreateSystem<ActiveDurationSystem>();
            var activeCancelSystem = this.World.GetOrCreateSystem<ActiveCancelSystem>();
            var activeCooldownSystem = this.World.GetOrCreateSystem<ActiveCooldownSystem>();

            activeSystemGroup.AddSystemToUpdateList(activePreviousSystem);
            activeSystemGroup.AddSystemToUpdateList(activeSystem);
            activeSystemGroup.AddSystemToUpdateList(activeTriggerSystem);
            timerSystemGroup.AddSystemToUpdateList(activeDurationSystem);
            timerSystemGroup.AddSystemToUpdateList(activeCancelSystem);
            timerSystemGroup.AddSystemToUpdateList(activeCooldownSystem);

            // Sort active groups
            activeSystemGroup.SortSystems();
            activeCancelSystemGroup.SortSystems();
            activeDisabledSystemGroup.SortSystems();
            activeEnabledSystemGroup.SortSystems();
            timerSystemGroup.SortSystems();
        }

        /// <summary>
        /// Sets up action systems in dependency order.
        /// CRITICAL: Enabled systems must be created BEFORE deactivated systems
        /// because deactivated systems depend on singletons created by enabled systems.
        /// </summary>
        private void SetupActionSystems()
        {
            var activeDisabledSystemGroup = this.World.GetOrCreateSystemManaged<ActiveDisabledSystemGroup>();
            var activeEnabledSystemGroup = this.World.GetOrCreateSystemManaged<ActiveEnabledSystemGroup>();

            // Create enabled action systems FIRST (these create singletons during OnCreate)
            var actionCreateSystem = this.World.GetOrCreateSystem<ActionCreateSystem>();
            var actionTagSystem = this.World.GetOrCreateSystem<ActionTagSystem>();
            var actionEnableableSystem = this.World.GetOrCreateSystem<ActionEnableableSystem>();

            activeEnabledSystemGroup.AddSystemToUpdateList(actionCreateSystem);
            activeEnabledSystemGroup.AddSystemToUpdateList(actionTagSystem);
            activeEnabledSystemGroup.AddSystemToUpdateList(actionEnableableSystem);

            // Create deactivated action systems AFTER (these depend on singletons from enabled systems)
            var actionCreateDeactivatedSystem = this.World.GetOrCreateSystem<ActionCreateDeactivatedSystem>();
            var actionTagDeactivatedSystem = this.World.GetOrCreateSystem<ActionTagDeactivatedSystem>();
            var actionEnableableDeactivatedSystem = this.World.GetOrCreateSystem<ActionEnableableDeactivatedSystem>();

            activeDisabledSystemGroup.AddSystemToUpdateList(actionCreateDeactivatedSystem);
            activeDisabledSystemGroup.AddSystemToUpdateList(actionTagDeactivatedSystem);
            activeDisabledSystemGroup.AddSystemToUpdateList(actionEnableableDeactivatedSystem);
        }

        /// <summary>
        /// Sets up core support systems that reaction systems depend on.
        /// </summary>
        private void SetupCoreSystems()
        {
            var initializeSystem = this.World.GetOrCreateSystemManaged<InitializeSystemGroup>();

            // Core initialization systems
            var initializeTargetsSystem = this.World.GetOrCreateSystem<InitializeTargetsSystem>();
            var initializeTransformSystem = this.World.GetOrCreateSystem<InitializeTransformSystem>();

            // Other condition systems
            var conditionInitializeSystem = this.World.GetOrCreateSystem<ConditionInitializeSystem>();
            var conditionDestroySystem = this.World.GetOrCreateSystem<ConditionDestroySystem>();

            initializeSystem.AddSystemToUpdateList(initializeTargetsSystem);
            initializeSystem.AddSystemToUpdateList(initializeTransformSystem);
            initializeSystem.AddSystemToUpdateList(conditionInitializeSystem);
            initializeSystem.AddSystemToUpdateList(conditionDestroySystem);
        }

        /// <summary>
        /// Updates the complete world systems exactly as they would run in production.
        /// This replaces individual system updates with proper world simulation.
        /// </summary>
        private void UpdateCompleteWorldSystems()
        {
            this.RunSystems(this.initializeSystemGroup, this.simulationSystemGroup, this.instantiateCommandBufferSystem);
        }

        /// <summary>
        /// Test tag component for integration testing.
        /// </summary>
        private struct TestTagComponent : IComponentData
        {
        }

        /// <summary>
        /// Second test tag component for integration testing.
        /// </summary>
        private struct SecondTestTagComponent : IComponentData
        {
        }

        /// <summary>
        /// Test tag component for entities created via ActionCreate.
        /// </summary>
        private struct TestCreatedEntityTag : IComponentData
        {
        }

        /// <summary>
        /// Test enableable component for integration testing.
        /// </summary>
        private struct TestEnableableComponent : IComponentData, IEnableableComponent
        {
        }
    }
}
