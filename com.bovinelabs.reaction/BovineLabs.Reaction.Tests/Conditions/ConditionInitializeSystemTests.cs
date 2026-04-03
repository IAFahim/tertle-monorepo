// <copyright file="ConditionInitializeSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>
//
namespace BovineLabs.Reaction.Tests.Conditions
{
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class ConditionInitializeSystemTests : ReactionTestFixture
    {
        private SystemHandle conditionInitializeSystem;

        public override void Setup()
        {
            base.Setup();
            this.conditionInitializeSystem = this.World.CreateSystem<ConditionInitializeSystem>();
        }

        [Test]
        public void GlobalConditionRegistration_ValidGlobalConditions_RegistersInLookup()
        {
            // Arrange: Create entity with global conditions
            var globalEntity = this.CreateEntityWithGlobalConditions();
            this.Manager.AddComponent<InitializeEntity>(globalEntity);

            // Act: Run initialize system
            this.RunConditionInitializeSystem();

            // Assert: Verify global conditions were registered
            var singleton = this.Manager.GetComponentData<ConditionInitializeSystem.Singleton>(this.conditionInitializeSystem);
            Assert.IsTrue(singleton.GlobalConditions.TryGetValue(new ConditionGlobal(123, 1), out var registeredEntity1));
            Assert.AreEqual(globalEntity, registeredEntity1);
            Assert.IsTrue(singleton.GlobalConditions.TryGetValue(new ConditionGlobal(456, 2), out var registeredEntity2));
            Assert.AreEqual(globalEntity, registeredEntity2);
        }

        [Test]
        public void GlobalConditionRegistration_InitializeSubSceneEntity_RegistersInLookup()
        {
            // Arrange: Create entity with global conditions using InitializeSubSceneEntity
            var globalEntity = this.CreateEntityWithGlobalConditions();
            this.Manager.AddComponent<InitializeSubSceneEntity>(globalEntity);

            // Act: Run initialize system
            this.RunConditionInitializeSystem();

            // Assert: Verify global conditions were registered
            var singleton = this.Manager.GetComponentData<ConditionInitializeSystem.Singleton>(this.conditionInitializeSystem);
            Assert.IsTrue(singleton.GlobalConditions.TryGetValue(new ConditionGlobal(123, 1), out var registeredEntity));
            Assert.AreEqual(globalEntity, registeredEntity);
        }

        [Test]
        public void GlobalConditionRegistration_EmptyGlobalConditions_HandlesGracefully()
        {
            // Arrange: Create entity with empty global conditions buffer
            var globalEntity = this.CreateEntityWithEmptyGlobalConditions();
            this.Manager.AddComponent<InitializeEntity>(globalEntity);

            // Act: Run initialize system
            this.RunConditionInitializeSystem();

            // Assert: System should handle empty buffers gracefully
            var singleton = this.Manager.GetComponentData<ConditionInitializeSystem.Singleton>(this.conditionInitializeSystem);
            Assert.AreEqual(0, singleton.GlobalConditions.Count);
        }

        [Test]
        public void ConditionSubscription_LocalTargetCondition_CreatesEventSubscriber()
        {
            // Arrange: Create target entity and condition entity
            var targetEntity = this.Manager.CreateEntity();
            this.Manager.AddBuffer<EventSubscriber>(targetEntity);
            this.Manager.AddBuffer<ConditionComparisonValue>(targetEntity);
            this.Manager.AddBuffer<LinkedEntityGroup>(targetEntity);
            var conditionEntity = this.CreateConditionEntityWithLocalTarget(targetEntity);
            this.Manager.AddComponent<InitializeEntity>(conditionEntity);

            // Act: Run initialize system
            this.RunConditionInitializeSystem();

            // Assert: Verify EventSubscriber was created on target
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(targetEntity);
            Assert.AreEqual(1, eventSubscribers.Length);
            var subscription = eventSubscribers[0];
            Assert.AreEqual(conditionEntity, subscription.Subscriber);
            Assert.AreEqual(123, subscription.Key);
            Assert.AreEqual(1, subscription.ConditionType);
            Assert.AreEqual(0, subscription.Index);
        }

        [Test]
        public void ConditionSubscription_GlobalTargetCondition_CreatesEventSubscriber()
        {
            // Arrange: Create global condition entity and initialize global lookup
            var globalEntity = this.CreateEntityWithGlobalConditions();
            this.Manager.AddComponent<InitializeEntity>(globalEntity);
            this.Manager.AddBuffer<EventSubscriber>(globalEntity);
            this.Manager.AddBuffer<ConditionComparisonValue>(globalEntity);
            this.Manager.AddBuffer<LinkedEntityGroup>(globalEntity);

            // Create condition entity that references global condition (before initializing)
            var conditionEntity = this.CreateConditionEntityWithGlobalTarget();
            this.Manager.AddComponent<InitializeEntity>(conditionEntity);

            // Act: Run initialize system once for both global registration and subscriptions
            this.RunConditionInitializeSystem();

            // Assert: Verify EventSubscriber was created on global entity
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(globalEntity);
            Assert.AreEqual(1, eventSubscribers.Length);
            var subscription = eventSubscribers[0];
            Assert.AreEqual(conditionEntity, subscription.Subscriber);
            Assert.AreEqual(123, subscription.Key);
            Assert.AreEqual(1, subscription.ConditionType);
        }

        [Test]
        public void ConditionSubscription_DestroyOnTargetDestroyed_AddsToLinkedEntityGroup()
        {
            // Arrange: Create target entity and condition entity with DestroyOnTargetDestroyed enabled
            var targetEntity = this.Manager.CreateEntity();
            this.Manager.AddBuffer<EventSubscriber>(targetEntity);
            this.Manager.AddBuffer<ConditionComparisonValue>(targetEntity);
            this.Manager.AddBuffer<LinkedEntityGroup>(targetEntity);
            var conditionEntity = this.CreateConditionEntityWithDestroyOnTargetDestroyed(targetEntity);
            this.Manager.AddComponent<InitializeEntity>(conditionEntity);

            // Act: Run initialize system
            this.RunConditionInitializeSystem();

            // Assert: Verify entity was added to LinkedEntityGroup
            var linkedEntities = this.Manager.GetBuffer<LinkedEntityGroup>(targetEntity);
            Assert.AreEqual(1, linkedEntities.Length);
            Assert.AreEqual(conditionEntity, linkedEntities[0].Value);
        }

        [Test]
        public void ConditionSubscription_MultipleConditions_CreatesMultipleSubscriptions()
        {
            // Arrange: Create target entity and condition entity with multiple conditions
            var targetEntity = this.Manager.CreateEntity();
            this.Manager.AddBuffer<EventSubscriber>(targetEntity);
            this.Manager.AddBuffer<ConditionComparisonValue>(targetEntity);
            this.Manager.AddBuffer<LinkedEntityGroup>(targetEntity);
            var conditionEntity = this.CreateConditionEntityWithMultipleConditions(targetEntity);
            this.Manager.AddComponent<InitializeEntity>(conditionEntity);

            // Act: Run initialize system
            this.RunConditionInitializeSystem();

            // Assert: Verify multiple EventSubscribers were created
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(targetEntity);
            Assert.AreEqual(3, eventSubscribers.Length);

            // Verify each subscription has correct index
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(conditionEntity, eventSubscribers[i].Subscriber);
                Assert.AreEqual(i, eventSubscribers[i].Index);
            }
        }

        [Test]
        public void ConditionSubscription_InvalidGlobalCondition_LogsError()
        {
            // Arrange: Create condition entity referencing non-existent global condition
            var conditionEntity = this.CreateConditionEntityWithInvalidGlobalTarget();
            this.Manager.AddComponent<InitializeEntity>(conditionEntity);

            // Expect the error log message
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@".*Trying to use global condition \(999, 99\) that hasn't been setup.*"));

            // Act: Run initialize system
            this.RunConditionInitializeSystem();
        }

        [Test]
        public void ConditionSubscription_NullTarget_LogsError()
        {
            // Arrange: Create condition entity with target that resolves to Entity.Null
            var conditionEntity = this.CreateConditionEntityWithNullTarget();
            this.Manager.AddComponent<InitializeEntity>(conditionEntity);

            // Expect the error log message - BurstCompile systems need different expectation format
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@".*Trying to use target .* that resulted in a Entity\.Null.*"));

            // Act: Run initialize system
            this.RunConditionInitializeSystem();
        }

        [Test]
        public void ConditionSubscription_EmptyConditionMeta_HandlesGracefully()
        {
            // Arrange: Create entity with empty condition metadata
            var conditionEntity = this.CreateConditionEntityWithEmptyMeta();
            this.Manager.AddComponent<InitializeEntity>(conditionEntity);

            // Act & Assert: System should handle empty conditions gracefully
            Assert.DoesNotThrow(this.RunConditionInitializeSystem);
        }

        private Entity CreateEntityWithGlobalConditions()
        {
            var entity = this.Manager.CreateEntity();
            var globalConditionsBuffer = this.Manager.AddBuffer<ConditionGlobal>(entity);
            globalConditionsBuffer.Add(new ConditionGlobal(123, 1));
            globalConditionsBuffer.Add(new ConditionGlobal(456, 2));
            return entity;
        }

        private Entity CreateEntityWithEmptyGlobalConditions()
        {
            var entity = this.Manager.CreateEntity();
            this.Manager.AddBuffer<ConditionGlobal>(entity); // Empty buffer
            return entity;
        }

        private Entity CreateConditionEntityWithLocalTarget(Entity targetEntity)
        {
            var entity = this.Manager.CreateEntity();
            var comparisonValues = this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 5 });
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            var conditionsArray = builder.Allocate(ref root.Conditions, 1);
            conditionsArray[0] = new ConditionData
            {
                Key = 123,
                ConditionType = 1,
                Target = Target.Custom0,
                Feature = ConditionFeature.Invalid,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
                DestroyOnTargetDestroyed = false,
            };
            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);
            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets { Owner = entity });
            this.Manager.AddComponentData(entity, new TargetsCustom { Target0 = targetEntity });
            return entity;
        }

        private Entity CreateConditionEntityWithGlobalTarget()
        {
            var entity = this.Manager.CreateEntity();
            var comparisonValues = this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 5 });
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            var conditionsArray = builder.Allocate(ref root.Conditions, 1);
            conditionsArray[0] = new ConditionData
            {
                Key = 123,
                ConditionType = 1,
                Target = Target.None, // Global target
                Feature = ConditionFeature.Invalid,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
                DestroyOnTargetDestroyed = false,
            };
            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);
            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets { Owner = entity });
            return entity;
        }

        private Entity CreateConditionEntityWithDestroyOnTargetDestroyed(Entity targetEntity)
        {
            var entity = this.Manager.CreateEntity();
            var comparisonValues = this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 5 });
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            var conditionsArray = builder.Allocate(ref root.Conditions, 1);
            conditionsArray[0] = new ConditionData
            {
                Key = 123,
                ConditionType = 1,
                Target = Target.Custom0,
                Feature = ConditionFeature.Invalid,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
                DestroyOnTargetDestroyed = true, // Enable destroy on target destroyed
            };
            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);
            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets { Owner = entity });
            this.Manager.AddComponentData(entity, new TargetsCustom { Target0 = targetEntity });
            return entity;
        }

        private Entity CreateConditionEntityWithMultipleConditions(Entity targetEntity)
        {
            var entity = this.Manager.CreateEntity();
            var comparisonValues = this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            var valueIndices = new byte[3];
            for (int i = 0; i < valueIndices.Length; i++)
            {
                valueIndices[i] = (byte)comparisonValues.Length;
                comparisonValues.Add(new ConditionComparisonValue { Value = i + 1 });
            }

            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            var conditionsArray = builder.Allocate(ref root.Conditions, 3);
            for (int i = 0; i < 3; i++)
            {
                conditionsArray[i] = new ConditionData
                {
                    Key = (ushort)(100 + i),
                    ConditionType = (byte)(i + 1),
                    Target = Target.Custom0,
                    Feature = ConditionFeature.Invalid,
                    Operation = Equality.Equal,
                    ValueIndex = new ValueIndex { Value = valueIndices[i] },
                    DestroyOnTargetDestroyed = false,
                };
            }

            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);
            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets { Owner = entity });
            this.Manager.AddComponentData(entity, new TargetsCustom { Target0 = targetEntity });
            return entity;
        }

        private Entity CreateConditionEntityWithInvalidGlobalTarget()
        {
            var entity = this.Manager.CreateEntity();
            var comparisonValues = this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 5 });
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            var conditionsArray = builder.Allocate(ref root.Conditions, 1);
            conditionsArray[0] = new ConditionData
            {
                Key = 999, // Non-existent global condition
                ConditionType = 99,
                Target = Target.None, // Global target
                Feature = ConditionFeature.Invalid,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
                DestroyOnTargetDestroyed = false,
            };
            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);
            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets { Owner = entity });
            return entity;
        }

        private Entity CreateConditionEntityWithNullTarget()
        {
            var entity = this.Manager.CreateEntity();
            var comparisonValues = this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 5 });
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            var conditionsArray = builder.Allocate(ref root.Conditions, 1);
            conditionsArray[0] = new ConditionData
            {
                Key = 123,
                ConditionType = 1,
                Target = Target.Custom0, // Target0 will be Entity.Null
                Feature = ConditionFeature.Invalid,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
                DestroyOnTargetDestroyed = false,
            };
            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);
            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets { Owner = entity });
            this.Manager.AddComponentData(entity, new TargetsCustom { Target0 = Entity.Null });
            return entity;
        }

        private Entity CreateConditionEntityWithEmptyMeta()
        {
            var entity = this.Manager.CreateEntity();
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            builder.Allocate(ref root.Conditions, 0); // Empty conditions array
            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);
            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets { Owner = entity });
            return entity;
        }

        private void RunConditionInitializeSystem()
        {
            this.RunSystems(this.conditionInitializeSystem);
        }
    }
}
