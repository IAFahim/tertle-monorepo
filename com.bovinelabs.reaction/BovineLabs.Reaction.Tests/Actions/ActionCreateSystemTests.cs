// <copyright file="ActionCreateSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Actions
{
    using BovineLabs.Core;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Actions;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Tests for ActionCreateSystem, verifying entity instantiation, target assignment, and lifecycle management.
    /// </summary>
    public class ActionCreateSystemTests : ReactionTestFixture
    {
        private SystemHandle createSystem;
        private SystemHandle deactivatedSystem;
        private SystemHandle instantiateCommandBufferSystem;
        private Entity testPrefab;
        private ObjectId testPrefabId;

        private NativeHashMap<ObjectId, Entity> objectMap;

        public override void Setup()
        {
            base.Setup();

            // Create required command buffer system
            this.instantiateCommandBufferSystem = this.World.CreateSystem<InstantiateCommandBufferSystem>();

            this.createSystem = this.World.CreateSystem<ActionCreateSystem>();
            this.deactivatedSystem = this.World.CreateSystem<ActionCreateDeactivatedSystem>();

            // Create a test prefab entity for instantiation
            this.testPrefab = this.CreateTestPrefab();
            this.testPrefabId = new ObjectId(1);

            // Register the prefab in the ObjectDefinitionRegistry
            this.objectMap = this.SetupObjectRegistry((this.testPrefabId, this.testPrefab));
        }

        public override void TearDown()
        {
            base.TearDown();

            this.objectMap.Dispose();
        }

        [Test]
        public void NewlyActivatedEntity_WithActionCreate_InstantiatesEntity()
        {
            // Arrange
            var reactionEntity = this.CreateSingleReactionEntity(Target.None, destroyOnDisabled: false);

            // Act
            this.RunActionCreateSystem();

            // Assert
            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Targets, CreateEntityTag>()
                .Build(this.Manager);
            var createdEntity = query.GetSingletonEntity();
            Assert.AreNotEqual(reactionEntity, createdEntity, "Created entity should be different from reaction entity");
        }

        [Test]
        public void ActionCreate_WithTargetNone_CreatesEntityWithNullTarget()
        {
            // Arrange
            var reactionEntity = this.CreateSingleReactionEntity(Target.None, destroyOnDisabled: false);

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            var createdEntity = query.GetSingletonEntity();
            var targets = this.Manager.GetComponentData<Targets>(createdEntity);

            Assert.AreEqual(Entity.Null, targets.Target, "Target should be null");
            Assert.AreEqual(reactionEntity, targets.Owner, "Owner should be reaction entity");
            Assert.AreEqual(reactionEntity, targets.Source, "Source should be reaction entity");
        }

        [Test]
        public void ActionCreate_WithTargetSelf_CreatesEntityWithSelfAsTarget()
        {
            // Arrange
            var reactionEntity = this.CreateSingleReactionEntity(Target.Self, destroyOnDisabled: false);

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            var createdEntity = query.GetSingletonEntity();
            var targets = this.Manager.GetComponentData<Targets>(createdEntity);

            Assert.AreEqual(reactionEntity, targets.Target, "Target should be reaction entity (self)");
            Assert.AreEqual(reactionEntity, targets.Owner, "Owner should be reaction entity");
            Assert.AreEqual(reactionEntity, targets.Source, "Source should be reaction entity");
        }

        [Test]
        public void ActionCreate_WithTargetOwner_CreatesEntityWithOwnerAsTarget()
        {
            // Arrange
            var ownerEntity = this.Manager.CreateEntity();
            var reactionEntity =
                this.CreateReactionEntityWithTargets(Target.Owner, false, ownerEntity, this.Manager.CreateEntity(), this.Manager.CreateEntity());

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            var createdEntity = query.GetSingletonEntity();
            var targets = this.Manager.GetComponentData<Targets>(createdEntity);

            Assert.AreEqual(ownerEntity, targets.Target, "Target should be owner entity");
            Assert.AreEqual(ownerEntity, targets.Owner, "Owner should be preserved");
            Assert.AreEqual(reactionEntity, targets.Source, "Source should be reaction entity");
        }

        [Test]
        public void ActionCreate_WithTargetSource_CreatesEntityWithSourceAsTarget()
        {
            // Arrange
            var sourceEntity = this.Manager.CreateEntity();
            var reactionEntity =
                this.CreateReactionEntityWithTargets(Target.Source, false, this.Manager.CreateEntity(), sourceEntity, this.Manager.CreateEntity());

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            var createdEntity = query.GetSingletonEntity();
            var targets = this.Manager.GetComponentData<Targets>(createdEntity);

            Assert.AreEqual(sourceEntity, targets.Target, "Target should be source entity");
            Assert.AreEqual(reactionEntity, targets.Source, "Source should be reaction entity");
        }

        [Test]
        public void ActionCreate_WithTargetTarget_CreatesEntityWithTargetAsTarget()
        {
            // Arrange
            var targetEntity = this.Manager.CreateEntity();
            this.CreateReactionEntityWithTargets(Target.Target, false, this.Manager.CreateEntity(), this.Manager.CreateEntity(), targetEntity);

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            var createdEntity = query.GetSingletonEntity();
            var targets = this.Manager.GetComponentData<Targets>(createdEntity);

            Assert.AreEqual(targetEntity, targets.Target, "Target should be preserved from original target");
        }

        [Test]
        public void ActionCreate_WithCustomTarget0_CreatesEntityWithCustomTargetAsTarget()
        {
            // Arrange
            var customTarget = this.Manager.CreateEntity();
            this.CreateReactionEntityWithCustomTargets(Target.Custom0, false, customTarget, this.Manager.CreateEntity());

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            var createdEntity = query.GetSingletonEntity();
            var targets = this.Manager.GetComponentData<Targets>(createdEntity);

            Assert.AreEqual(customTarget, targets.Target, "Target should be custom target 0");
        }

        [Test]
        public void ActionCreate_WithCustomTarget1_CreatesEntityWithCustomTargetAsTarget()
        {
            // Arrange
            var customTarget = this.Manager.CreateEntity();
            this.CreateReactionEntityWithCustomTargets(Target.Custom1, false, this.Manager.CreateEntity(), customTarget);

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            var createdEntity = query.GetSingletonEntity();
            var targets = this.Manager.GetComponentData<Targets>(createdEntity);

            Assert.AreEqual(customTarget, targets.Target, "Target should be custom target 1");
        }

        [Test]
        public void ActionCreate_WithDestroyOnDisabled_AddsToLinkedEntityGroup()
        {
            // Arrange
            var reactionEntity = this.CreateSingleReactionEntity(Target.None, destroyOnDisabled: true);

            // Act
            this.RunActionCreateSystem();

            // Assert
            Assert.IsTrue(this.Manager.HasBuffer<LinkedEntityGroup>(reactionEntity), "Should have LinkedEntityGroup buffer");
            Assert.IsTrue(this.Manager.HasBuffer<ActionCreated>(reactionEntity), "Should have ActionCreated buffer");

            var linkedGroup = this.Manager.GetBuffer<LinkedEntityGroup>(reactionEntity);
            var actionCreated = this.Manager.GetBuffer<ActionCreated>(reactionEntity);

            Assert.AreEqual(2, linkedGroup.Length, "LinkedEntityGroup should contain reaction entity and created entity");
            Assert.AreEqual(1, actionCreated.Length, "ActionCreated should contain one created entity");

            var createdEntity = actionCreated[0].Value;
            Assert.AreEqual(createdEntity, linkedGroup[1].Value, "Created entity should be in LinkedEntityGroup");
        }

        [Test]
        public void ActionCreate_WithoutDestroyOnDisabled_DoesNotAddToLinkedEntityGroup()
        {
            // reactionEntity
            var reactionEntity = this.CreateSingleReactionEntity(Target.None, destroyOnDisabled: false);
            this.Manager.AddBuffer<LinkedEntityGroup>(reactionEntity).Add(new LinkedEntityGroup { Value = reactionEntity });
            this.Manager.AddBuffer<ActionCreated>(reactionEntity);

            // Act
            this.RunActionCreateSystem();

            // Assert
            Assert.AreEqual(1, this.Manager.GetBuffer<LinkedEntityGroup>(reactionEntity).Length, "LinkedEntityGroup should only contain reaction entity");
            Assert.AreEqual(0, this.Manager.GetBuffer<ActionCreated>(reactionEntity).Length, "ActionCreated should be empty");
        }

        [Test]
        public void ActionCreate_MultipleActions_CreatesMultipleEntities()
        {
            // Arrange
            var reactionEntity = this.CreateReactionEntityWithMultipleActions();

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            Assert.AreEqual(2, query.CalculateEntityCount(), "Should have created two entities");

            var actionCreated = this.Manager.GetBuffer<ActionCreated>(reactionEntity);
            Assert.AreEqual(2, actionCreated.Length, "ActionCreated should contain two created entities");
        }

        [Test]
        public void AlreadyProcessedEntity_WithActivePrevious_DoesNotCreateNewEntities()
        {
            // Arrange
            var reactionEntity = this.CreateSingleReactionEntity(Target.None, destroyOnDisabled: false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true); // Mark as already processed

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            Assert.AreEqual(0, query.CalculateEntityCount(), "Should not have created any entities");
        }

        [Test]
        public void EntityWithoutActive_DoesNotCreateEntities()
        {
            // Arrange
            var entity = this.CreateSingleReactionEntity(Target.None, false);
            this.Manager.SetComponentEnabled<Active>(entity, false);

            // Act
            this.RunActionCreateSystem();

            // Assert
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CreateEntityTag, Targets>()
                .Build(this.Manager);
            Assert.AreEqual(0, query.CalculateEntityCount(), "Should not have created any entities");
        }

        [Test]
        public void DeactivatedSystem_WithActionCreated_DestroysCreatedEntities()
        {
            // Arrange - First create entities
            var reactionEntity = this.CreateSingleReactionEntity(Target.None, destroyOnDisabled: true);
            this.RunActionCreateSystem();

            var actionCreated = this.Manager.GetBuffer<ActionCreated>(reactionEntity);
            var createdEntity = actionCreated[0].Value;

            // Verify entity was created
            Assert.IsTrue(this.Manager.Exists(createdEntity), "Created entity should exist");

            // Arrange - Mark reaction as deactivated
            this.Manager.SetComponentEnabled<ActivePrevious>(reactionEntity, true);
            this.Manager.SetComponentEnabled<Active>(reactionEntity, false);

            // Act
            this.deactivatedSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<DestroyEntity>(createdEntity), "Created entity should have DestroyEntity component");

            var linkedGroup = this.Manager.GetBuffer<LinkedEntityGroup>(reactionEntity);
            Assert.AreEqual(1, linkedGroup.Length, "LinkedEntityGroup should only contain reaction entity");

            var actionCreatedBuffer = this.Manager.GetBuffer<ActionCreated>(reactionEntity);
            Assert.AreEqual(0, actionCreatedBuffer.Length, "ActionCreated buffer should be empty");
        }

        [Test]
        public void DeactivatedSystem_WithNestedActiveReactions_PropagatesDeactivation()
        {
            // Arrange - Create reaction and its created entity with active reaction
            var parentReaction = this.CreateSingleReactionEntity(Target.None, destroyOnDisabled: true);
            this.RunActionCreateSystem();

            var actionCreated = this.Manager.GetBuffer<ActionCreated>(parentReaction);
            var childEntity = actionCreated[0].Value;

            // Make child entity an active reaction
            this.Manager.AddComponent<Active>(childEntity);
            this.Manager.SetComponentEnabled<Active>(childEntity, true);

            // Deactivate parent
            this.Manager.SetComponentEnabled<ActivePrevious>(parentReaction, true);
            this.Manager.SetComponentEnabled<Active>(parentReaction, false);

            // Act
            this.deactivatedSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Assert
            Assert.IsTrue(this.Manager.HasComponent<Active>(childEntity), "Child should still have Active component");
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(childEntity), "Child Active component should be disabled");
        }

        private Entity CreateTestPrefab()
        {
            var archetype = this.Manager.CreateArchetype(typeof(CreateEntityTag), typeof(Targets), typeof(Prefab), typeof(DestroyEntity));
            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentEnabled<DestroyEntity>(entity, false);
            return entity;
        }

        private void RunActionCreateSystem()
        {
            this.RunSystems(this.createSystem, this.instantiateCommandBufferSystem);
        }

        private Entity CreateActionReactionEntity(bool destroyOnDisabled)
        {
            // Start with base reaction entity
            var entity = this.CreateReactionEntity();

            // Add ActionCreate-specific components
            if (destroyOnDisabled)
            {
                this.Manager.AddComponent<LinkedEntityGroup>(entity);
                this.Manager.AddComponent<ActionCreated>(entity);

                // Initialize LinkedEntityGroup with self
                var linkedGroup = this.Manager.GetBuffer<LinkedEntityGroup>(entity);
                linkedGroup.Add(new LinkedEntityGroup { Value = entity });
            }

            // Add ActionCreate buffer
            this.Manager.AddBuffer<ActionCreate>(entity);

            return entity;
        }

        private Entity CreateSingleReactionEntity(Target target, bool destroyOnDisabled)
        {
            var entity = this.CreateActionReactionEntity(destroyOnDisabled);

            // Set up ActionCreate buffer
            var actionBuffer = this.Manager.GetBuffer<ActionCreate>(entity);
            actionBuffer.Add(new ActionCreate
            {
                Id = this.testPrefabId,
                Target = target,
                DestroyOnDisabled = destroyOnDisabled,
            });

            return entity;
        }

        private Entity CreateReactionEntityWithTargets(Target target, bool destroyOnDisabled, Entity owner, Entity source, Entity targetEntity)
        {
            // Start with base ActionCreate reaction entity
            var entity = this.CreateActionReactionEntity(destroyOnDisabled);

            // Set up ActionCreate buffer
            var actionBuffer = this.Manager.GetBuffer<ActionCreate>(entity);
            actionBuffer.Add(new ActionCreate
            {
                Id = this.testPrefabId,
                Target = target,
                DestroyOnDisabled = destroyOnDisabled,
            });

            // Update targets with specific entities using base method approach
            this.Manager.SetComponentData(entity, new Targets
            {
                Owner = owner,
                Source = source,
                Target = targetEntity,
            });

            return entity;
        }

        private void CreateReactionEntityWithCustomTargets(Target target, bool destroyOnDisabled, Entity custom0, Entity custom1)
        {
            var entity = this.CreateSingleReactionEntity(target, destroyOnDisabled);

            // Add custom targets component
            this.Manager.AddComponent<TargetsCustom>(entity);
            this.Manager.SetComponentData(entity, new TargetsCustom
            {
                Target0 = custom0,
                Target1 = custom1,
            });
        }

        private Entity CreateReactionEntityWithMultipleActions()
        {
            var entity = this.CreateActionReactionEntity(destroyOnDisabled: true);

            // Set up ActionCreate buffer with multiple actions
            var actionBuffer = this.Manager.GetBuffer<ActionCreate>(entity);
            actionBuffer.Add(new ActionCreate
            {
                Id = this.testPrefabId,
                Target = Target.None,
                DestroyOnDisabled = true,
            });

            actionBuffer.Add(new ActionCreate
            {
                Id = this.testPrefabId,
                Target = Target.Self,
                DestroyOnDisabled = true,
            });

            return entity;
        }

        private struct CreateEntityTag : IComponentData
        {
        }
    }
}
