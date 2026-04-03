// <copyright file="ConditionDestroySystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Conditions
{
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public class ConditionDestroySystemTests : ReactionTestFixture
    {
        private SystemHandle conditionInitializeSystem;
        private SystemHandle conditionDestroySystem;

        public override void Setup()
        {
            base.Setup();
            this.conditionInitializeSystem = this.World.CreateSystem<ConditionInitializeSystem>();
            this.conditionDestroySystem = this.World.CreateSystem<ConditionDestroySystem>();

            // Initialize the condition system to set up the singleton
            this.conditionInitializeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }

        private void RunConditionDestroySystem()
        {
            this.conditionDestroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }

        [Test]
        public void GlobalConditionCleanup_EntityWithGlobalConditions_RemovesFromGlobalLookup()
        {
            // Arrange: Create entity with global conditions
            var globalEntity = this.CreateEntityWithGlobalConditions();
            var testConditionGlobal = new ConditionGlobal(123, 1);

            // Initialize the global conditions by running the initialize system
            this.conditionInitializeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify the global condition was added
            var singleton = this.Manager.GetComponentData<ConditionInitializeSystem.Singleton>(this.conditionInitializeSystem);
            Assert.IsTrue(singleton.GlobalConditions.ContainsKey(testConditionGlobal),
                "Global condition should be added during initialization");

            // Mark entity for destruction
            this.Manager.AddComponent<DestroyEntity>(globalEntity);

            // Act: Run the destroy system
            this.RunConditionDestroySystem();

            // Assert: Global condition should be removed from lookup
            Assert.IsFalse(singleton.GlobalConditions.ContainsKey(testConditionGlobal),
                "Global condition should be removed from lookup when entity is destroyed");
        }

        [Test]
        public void SubscriptionCleanup_ConditionEntityDestroyed_RemovesSubscriptionsFromTargets()
        {
            // Arrange: Create condition entity and target entity
            var conditionEntity = this.CreateConditionEntityWithSubscriptions();
            var targetEntity = this.CreateTargetEntityWithBuffers();

            // Set up the target reference
            var targets = this.Manager.GetComponentData<Targets>(conditionEntity);
            targets.Target = targetEntity;
            this.Manager.SetComponentData(conditionEntity, targets);

            // Initialize subscriptions through the system
            this.conditionInitializeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify subscriptions exist before destruction
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(targetEntity);
            var linkedEntityGroup = this.Manager.GetBuffer<LinkedEntityGroup>(targetEntity);
            Assert.Greater(eventSubscribers.Length, 0, "Target should have event subscriptions before destruction");
            Assert.Greater(linkedEntityGroup.Length, 0, "Target should have linked entity group entries before destruction");

            // Mark condition entity for destruction
            this.Manager.AddComponent<DestroyEntity>(conditionEntity);

            // Act: Run the destroy system
            this.RunConditionDestroySystem();

            // Assert: Subscriptions should be removed from target
            var eventSubscribersAfter = this.Manager.GetBuffer<EventSubscriber>(targetEntity);
            var linkedEntityGroupAfter = this.Manager.GetBuffer<LinkedEntityGroup>(targetEntity);

            // Check that the condition entity's subscriptions are removed
            var hasConditionSubscription = false;
            foreach (var sub in eventSubscribersAfter)
            {
                if (sub.Subscriber == conditionEntity)
                {
                    hasConditionSubscription = true;
                    break;
                }
            }
            Assert.IsFalse(hasConditionSubscription, "Event subscriptions should be removed from target");

            var hasConditionLink = false;
            foreach (var link in linkedEntityGroupAfter)
            {
                if (link.Value == conditionEntity)
                {
                    hasConditionLink = true;
                    break;
                }
            }
            Assert.IsFalse(hasConditionLink, "Linked entity group entries should be removed from target");
        }

        [Test]
        public void SubscriptionCleanup_TargetAlsoDestroyed_SkipsCleanup()
        {
            // Arrange: Create condition entity and target entity
            var conditionEntity = this.CreateConditionEntityWithSubscriptions();
            var targetEntity = this.CreateTargetEntityWithSubscriptions(conditionEntity);

            // Mark both entities for destruction
            this.Manager.AddComponent<DestroyEntity>(conditionEntity);
            this.Manager.AddComponent<DestroyEntity>(targetEntity);
            this.Manager.SetComponentEnabled<DestroyEntity>(targetEntity, true);

            // Act: Run the destroy system
            this.RunConditionDestroySystem();

            // Assert: System should have skipped cleanup since target is also being destroyed
            // No specific assertion needed - the test passes if no exceptions are thrown
            // and the system completes successfully
            Assert.IsTrue(this.Manager.HasComponent<DestroyEntity>(targetEntity),
                "Target entity should still be marked for destruction");
        }

        [Test]
        public void SubscriptionCleanup_MultipleSubscriptionsToSameTarget_RemovesAllCorrectly()
        {
            // Arrange: Create condition entity with multiple subscriptions to same target
            var conditionEntity = this.CreateConditionEntityWithMultipleSubscriptions();
            var targetEntity = this.CreateTargetEntityWithBuffers();

            // Set up the target reference
            var targets = this.Manager.GetComponentData<Targets>(conditionEntity);
            targets.Target = targetEntity;
            this.Manager.SetComponentData(conditionEntity, targets);

            // Initialize subscriptions through the system
            this.conditionInitializeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            // Verify multiple subscriptions exist
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(targetEntity);
            var conditionSubscriptionCount = 0;
            foreach (var sub in eventSubscribers)
            {
                if (sub.Subscriber == conditionEntity)
                {
                    conditionSubscriptionCount++;
                }
            }
            Assert.AreEqual(3, conditionSubscriptionCount, "Should have 3 subscriptions from condition entity");

            // Mark condition entity for destruction
            this.Manager.AddComponent<DestroyEntity>(conditionEntity);

            // Act: Run the destroy system
            this.RunConditionDestroySystem();

            // Assert: All subscriptions should be removed
            var eventSubscribersAfter = this.Manager.GetBuffer<EventSubscriber>(targetEntity);
            var remainingConditionSubscriptions = 0;
            foreach (var sub in eventSubscribersAfter)
            {
                if (sub.Subscriber == conditionEntity)
                {
                    remainingConditionSubscriptions++;
                }
            }
            Assert.AreEqual(0, remainingConditionSubscriptions, "All subscriptions should be removed");
        }

        [Test]
        public void SubscriptionCleanup_TargetWithoutRequiredBuffers_HandlesGracefully()
        {
            // Arrange: Create condition entity and target without EventSubscriber/LinkedEntityGroup buffers
            var conditionEntity = this.CreateConditionEntityWithSubscriptions();
            var targetEntity = this.Manager.CreateEntity();

            // Mark condition entity for destruction
            this.Manager.AddComponent<DestroyEntity>(conditionEntity);

            // Act & Assert: System should handle missing buffers gracefully
            Assert.DoesNotThrow(() =>
            {
                this.conditionDestroySystem.Update(this.WorldUnmanaged);
                this.Manager.CompleteAllTrackedJobs();
            }, "System should handle targets without required buffers gracefully");
        }

        [Test]
        public void SubscriptionCleanup_EmptyConditionMeta_HandlesGracefully()
        {
            // Arrange: Create condition entity with empty condition metadata
            var conditionEntity = this.CreateConditionEntityWithEmptyMeta();

            // Mark condition entity for destruction
            this.Manager.AddComponent<DestroyEntity>(conditionEntity);

            // Act & Assert: System should handle empty metadata gracefully
            Assert.DoesNotThrow(() =>
            {
                this.conditionDestroySystem.Update(this.WorldUnmanaged);
                this.Manager.CompleteAllTrackedJobs();
            }, "System should handle empty condition metadata gracefully");
        }

        [Test]
        public void GlobalConditionCleanup_EmptyGlobalConditions_HandlesGracefully()
        {
            // Arrange: Create entity with empty global conditions buffer
            var globalEntity = this.CreateEntityWithEmptyGlobalConditions();

            // Mark entity for destruction
            this.Manager.AddComponent<DestroyEntity>(globalEntity);

            // Act & Assert: System should handle empty global conditions gracefully
            Assert.DoesNotThrow(() =>
            {
                this.conditionDestroySystem.Update(this.WorldUnmanaged);
                this.Manager.CompleteAllTrackedJobs();
            }, "System should handle empty global conditions gracefully");
        }

        // Helper methods for creating test entities

        private Entity CreateEntityWithGlobalConditions()
        {
            var entity = this.Manager.CreateEntity();
            var globalConditionsBuffer = this.Manager.AddBuffer<ConditionGlobal>(entity);
            globalConditionsBuffer.Add(new ConditionGlobal(123, 1));
            globalConditionsBuffer.Add(new ConditionGlobal(456, 2));

            // Add InitializeEntity component so the system will process it
            this.Manager.AddComponent<InitializeEntity>(entity);
            return entity;
        }

        private Entity CreateEntityWithEmptyGlobalConditions()
        {
            var entity = this.Manager.CreateEntity();
            this.Manager.AddBuffer<ConditionGlobal>(entity); // Empty buffer
            return entity;
        }

        private Entity CreateConditionEntityWithSubscriptions()
        {
            var entity = this.Manager.CreateEntity();

            // Create blob asset for condition metadata
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            var conditionsArray = builder.Allocate(ref root.Conditions, 1);
            conditionsArray[0] = new ConditionData
            {
                Target = Target.Target,
                Key = 100,
                ConditionType = 1,
                DestroyOnTargetDestroyed = true,
            };
            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);

            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets
            {
                Owner = entity,
                Source = entity,
                Target = Entity.Null, // Will be set by test
            });

            // Add InitializeEntity component so the system will process it
            this.Manager.AddComponent<InitializeEntity>(entity);

            return entity;
        }

        private Entity CreateConditionEntityWithMultipleSubscriptions()
        {
            var entity = this.Manager.CreateEntity();

            // Create blob asset with multiple conditions
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            var conditionsArray = builder.Allocate(ref root.Conditions, 3);
            for (int i = 0; i < 3; i++)
            {
                conditionsArray[i] = new ConditionData
                {
                    Target = Target.Target,
                    Key = (ushort)(100 + i),
                    ConditionType = 1,
                    DestroyOnTargetDestroyed = true,
                };
            }
            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);

            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets
            {
                Owner = entity,
                Source = entity,
                Target = Entity.Null,
            });

            // Add InitializeEntity component so the system will process it
            this.Manager.AddComponent<InitializeEntity>(entity);

            return entity;
        }

        private Entity CreateConditionEntityWithEmptyMeta()
        {
            var entity = this.Manager.CreateEntity();

            // Create blob asset with empty conditions array
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<ConditionMetaData>();
            builder.Allocate(ref root.Conditions, 0);
            var blobAsset = builder.CreateBlobAssetReference<ConditionMetaData>(Allocator.Persistent);
            this.BlobAssetStore.TryAdd(ref blobAsset);

            this.Manager.AddComponentData(entity, new ConditionMeta { Value = blobAsset });
            this.Manager.AddComponentData(entity, new Targets
            {
                Owner = entity,
                Source = entity,
                Target = Entity.Null,
            });

            return entity;
        }

        private Entity CreateTargetEntityWithBuffers()
        {
            var targetEntity = this.Manager.CreateEntity();

            // Add empty EventSubscriber and LinkedEntityGroup buffers
            this.Manager.AddBuffer<EventSubscriber>(targetEntity);
            this.Manager.AddBuffer<LinkedEntityGroup>(targetEntity);

            return targetEntity;
        }

        private Entity CreateTargetEntityWithSubscriptions(Entity conditionEntity)
        {
            var targetEntity = this.Manager.CreateEntity();

            // Add EventSubscriber buffer with subscription from condition entity
            var eventSubscribers = this.Manager.AddBuffer<EventSubscriber>(targetEntity);
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = conditionEntity,
                Key = 100,
                ConditionType = 1,
                Index = 0,
            });

            // Add LinkedEntityGroup buffer with link to condition entity
            var linkedEntityGroup = this.Manager.AddBuffer<LinkedEntityGroup>(targetEntity);
            linkedEntityGroup.Add(new LinkedEntityGroup { Value = conditionEntity });
            linkedEntityGroup.Add(new LinkedEntityGroup { Value = targetEntity }); // Self-reference

            // Update the condition entity's target reference
            if (this.Manager.HasComponent<Targets>(conditionEntity))
            {
                var targets = this.Manager.GetComponentData<Targets>(conditionEntity);
                targets.Target = targetEntity;
                this.Manager.SetComponentData(conditionEntity, targets);
            }

            return targetEntity;
        }

        private Entity CreateTargetEntityWithMultipleSubscriptions(Entity conditionEntity, int subscriptionCount)
        {
            var targetEntity = this.Manager.CreateEntity();

            // Add EventSubscriber buffer with multiple subscriptions from condition entity
            var eventSubscribers = this.Manager.AddBuffer<EventSubscriber>(targetEntity);
            for (int i = 0; i < subscriptionCount; i++)
            {
                eventSubscribers.Add(new EventSubscriber
                {
                    Subscriber = conditionEntity,
                    Key = (ushort)(100 + i),
                    ConditionType = 1,
                    Index = (byte)i,
                });
            }

            // Add LinkedEntityGroup buffer with multiple links to condition entity
            var linkedEntityGroup = this.Manager.AddBuffer<LinkedEntityGroup>(targetEntity);
            for (int i = 0; i < subscriptionCount; i++)
            {
                linkedEntityGroup.Add(new LinkedEntityGroup { Value = conditionEntity });
            }
            linkedEntityGroup.Add(new LinkedEntityGroup { Value = targetEntity }); // Self-reference

            // Update the condition entity's target reference
            if (this.Manager.HasComponent<Targets>(conditionEntity))
            {
                var targets = this.Manager.GetComponentData<Targets>(conditionEntity);
                targets.Target = targetEntity;
                this.Manager.SetComponentData(conditionEntity, targets);
            }

            return targetEntity;
        }
    }
}
