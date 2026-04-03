// <copyright file="InitializeTargetsSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Core
{
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Core;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;

    /// <summary>
    /// Tests for the InitializeTargetsSystem that handles target entity initialization for newly created reaction entities.
    /// </summary>
    [TestFixture]
    public class InitializeTargetsSystemTests : ReactionTestFixture
    {
        private Entity initializeTargetSingleton;
        private ComponentSystemGroup systemGroup;

        [SetUp]
        public void SetUp()
        {
            // Create the InitializeTarget singleton entity with buffer
            this.initializeTargetSingleton = this.Manager.CreateEntity(typeof(InitializeTarget));
            this.Manager.GetBuffer<InitializeTarget>(this.initializeTargetSingleton).Initialize();

            // Create and configure the system group
            this.systemGroup = this.World.CreateSystemManaged<InitializeSystemGroup>();
            var system = this.World.CreateSystem<InitializeTargetsSystem>();
            this.systemGroup.AddSystemToUpdateList(system);
        }

        [Test]
        public void InitializeTargetsSystem_WithValidObjectId_SetsTargetCorrectly()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var targetEntity = this.Manager.CreateEntity();
            var sourceEntity = this.CreateInitializationEntity(objectId, target: targetEntity);

            this.AddInitializeTargetData(objectId, Target.Target);

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var updatedTargets = this.Manager.GetComponentData<Targets>(sourceEntity);
            Assert.AreEqual(targetEntity, updatedTargets.Target, "Target should be set to the resolved entity");
        }

        [Test]
        public void InitializeTargetsSystem_WithInvalidObjectId_DoesNotModifyTarget()
        {
            // Arrange
            var objectId = new ObjectId(999); // Non-existent ObjectId
            var originalTarget = this.Manager.CreateEntity();
            var sourceEntity = this.CreateInitializationEntity(objectId, target: originalTarget);

            // Set up InitializeTarget buffer with different ObjectId
            this.AddInitializeTargetData(new ObjectId(42), Target.Target); // Different ObjectId

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var updatedTargets = this.Manager.GetComponentData<Targets>(sourceEntity);
            Assert.AreEqual(originalTarget, updatedTargets.Target, "Target should remain unchanged when ObjectId not found");
        }

        [Test]
        public void InitializeTargetsSystem_WhenTargetResolutionFails_RestoresPreviousTarget()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var originalTarget = this.Manager.CreateEntity();
            var sourceEntity = this.CreateInitializationEntity(objectId, target: originalTarget);

            // Set up InitializeTarget buffer that will resolve to Entity.Null
            this.AddInitializeTargetData(objectId, Target.None); // This will resolve to Entity.Null

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var updatedTargets = this.Manager.GetComponentData<Targets>(sourceEntity);
            Assert.AreEqual(originalTarget, updatedTargets.Target, "Target should be restored to previous value when resolution fails");
        }

        [Test]
        public void InitializeTargetsSystem_WithMultipleEntities_ProcessesAllCorrectly()
        {
            // Arrange
            var objectId1 = new ObjectId(1);
            var objectId2 = new ObjectId(2);
            var targetEntity1 = this.Manager.CreateEntity();
            var targetEntity2 = this.Manager.CreateEntity();

            var sourceEntity1 = this.CreateInitializationEntity(objectId1, target: targetEntity1);
            var sourceEntity2 = this.CreateInitializationEntity(objectId2, target: targetEntity2);

            // Set up InitializeTarget buffer
            this.AddInitializeTargetData(objectId1, Target.Target);
            this.AddInitializeTargetData(objectId2, Target.Target);

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var updatedTargets1 = this.Manager.GetComponentData<Targets>(sourceEntity1);
            var updatedTargets2 = this.Manager.GetComponentData<Targets>(sourceEntity2);

            Assert.AreEqual(targetEntity1, updatedTargets1.Target, "First entity target should be correctly set");
            Assert.AreEqual(targetEntity2, updatedTargets2.Target, "Second entity target should be correctly set");
        }

        [Test]
        public void InitializeTargetsSystem_WithDifferentTargetTypes_ResolvesCorrectly()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var ownerEntity = this.Manager.CreateEntity();
            var sourceEntity = this.CreateInitializationEntity(objectId, owner: ownerEntity);

            // Test with Target.Owner which should resolve to ownerEntity
            this.AddInitializeTargetData(objectId, Target.Owner);

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var updatedTargets = this.Manager.GetComponentData<Targets>(sourceEntity);
            Assert.AreEqual(ownerEntity, updatedTargets.Target, "Target should resolve to Owner entity");
        }

        [Test]
        public void InitializeTargetsSystem_WithSelfTarget_ResolvesSelf()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var sourceEntity = this.CreateInitializationEntity(objectId);

            // Test with Target.Self which should resolve to sourceEntity itself
            this.AddInitializeTargetData(objectId, Target.Self);

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var updatedTargets = this.Manager.GetComponentData<Targets>(sourceEntity);
            Assert.AreEqual(sourceEntity, updatedTargets.Target, "Target should resolve to self entity");
        }

        [Test]
        public void InitializeTargetsSystem_WithoutInitializeTargetSingleton_DoesNotUpdate()
        {
            // Arrange
            this.Manager.DestroyEntity(this.initializeTargetSingleton);

            var objectId = new ObjectId(42);
            var originalTarget = this.Manager.CreateEntity();
            var sourceEntity = this.CreateInitializationEntity(objectId, target: originalTarget);

            // Act - system should not run without the required singleton
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var updatedTargets = this.Manager.GetComponentData<Targets>(sourceEntity);
            Assert.AreEqual(originalTarget, updatedTargets.Target, "Target should remain unchanged when singleton is missing");
        }

        private void AddInitializeTargetData(ObjectId objectId, Target target)
        {
            var buffer = this.Manager.GetBuffer<InitializeTarget>(this.initializeTargetSingleton).AsMap();
            buffer.Add(objectId, new InitializeTarget.Data { Target = target });
        }

    }
}
