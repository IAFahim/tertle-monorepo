// <copyright file="ConditionSubscribedSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>
//
namespace BovineLabs.Essence.Tests.Conditions
{
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Builders;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;

    /// <summary>
    /// Tests for <see cref="ConditionSubscribedSystem"/>, verifying subscription-based monitoring and dirty flag management.
    /// </summary>
    public class ConditionSubscribedSystemTests : EssenceTestsFixture
    {
        private SystemHandle conditionSubscribedStatSystem;
        private byte statConditionType;
        private byte intrinsicConditionType;

        public override void Setup()
        {
            base.Setup();
            this.conditionSubscribedStatSystem = this.World.CreateSystem<ConditionSubscribedSystem>();
            this.statConditionType = ConditionTypes.NameToKey(ConditionTypes.StatType);
            this.intrinsicConditionType = ConditionTypes.NameToKey(ConditionTypes.IntrinsicType);
        }

        [Test]
        public void EntityWithStatBuffer_WhenStatSubscriberAdded_EnablesStatConditionDirty()
        {
            // Arrange
            var statKey = (StatKey)100;
            var statEntity = this.CreateStatEntity(CreateStatModifier(statKey, 42, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Ensure StatConditionDirty is initially disabled
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, false);

            // Act - Add a stat condition subscriber (this should trigger the change filter)
            this.AddStatSubscriber(statEntity, subscriberEntity, statKey, conditionIndex: 1);
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should be enabled when stat subscriber is added");
        }

        [Test]
        public void EntityWithIntrinsicBuffer_WhenIntrinsicSubscriberAdded_EnablesIntrinsicConditionDirty()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var intrinsicEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(intrinsicKey, 42));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Ensure IntrinsicConditionDirty is initially disabled
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, false);

            // Act - Add an intrinsic condition subscriber
            this.AddIntrinsicSubscriber(intrinsicEntity, subscriberEntity, intrinsicKey, conditionIndex: 1);
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should be enabled when intrinsic subscriber is added");
        }

        [Test]
        public void EntityWithStatConditionDirtyAlreadyEnabled_WhenStatSubscriberAdded_RemainsEnabled()
        {
            // Arrange
            var statKey = (StatKey)100;
            var statEntity = this.CreateStatEntity(CreateStatModifier(statKey, 42, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Pre-enable StatConditionDirty
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, true);

            // Act - Add a stat condition subscriber
            this.AddStatSubscriber(statEntity, subscriberEntity, statKey, conditionIndex: 1);
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should remain enabled");
        }

        [Test]
        public void EntityWithIntrinsicConditionDirtyAlreadyEnabled_WhenIntrinsicSubscriberAdded_RemainsEnabled()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var intrinsicEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(intrinsicKey, 42));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Pre-enable IntrinsicConditionDirty
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, true);

            // Act - Add an intrinsic condition subscriber
            this.AddIntrinsicSubscriber(intrinsicEntity, subscriberEntity, intrinsicKey, conditionIndex: 1);
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should remain enabled");
        }

        [Test]
        public void EntityWithoutStatBuffer_WhenStatSubscriberAdded_DoesNotCrash()
        {
            // Arrange
            var statKey = (StatKey)100;
            var entityWithoutStats = this.Manager.CreateEntity();
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Add EventSubscriber buffer but no Stat buffer
            this.Manager.AddBuffer<EventSubscriber>(entityWithoutStats);
            this.AddStatSubscriber(entityWithoutStats, subscriberEntity, statKey, conditionIndex: 1);

            // Act & Assert - Should not crash due to WithAll filter
            Assert.DoesNotThrow(() => this.RunSystem(this.conditionSubscribedStatSystem));
        }

        [Test]
        public void EntityWithoutIntrinsicBuffer_WhenIntrinsicSubscriberAdded_DoesNotCrash()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var entityWithoutIntrinsics = this.Manager.CreateEntity();
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Add EventSubscriber buffer but no Intrinsic buffer
            this.Manager.AddBuffer<EventSubscriber>(entityWithoutIntrinsics);
            this.AddIntrinsicSubscriber(entityWithoutIntrinsics, subscriberEntity, intrinsicKey, conditionIndex: 1);

            // Act & Assert - Should not crash due to WithAll filter
            Assert.DoesNotThrow(() => this.RunSystem(this.conditionSubscribedStatSystem));
        }

        [Test]
        public void EntityWithNonStatConditionSubscriber_WhenAdded_DoesNotEnableStatDirty()
        {
            // Arrange
            var statKey = (StatKey)100;
            var statEntity = this.CreateStatEntityWithEventSubscriber(CreateStatModifier(statKey, 42, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, false);

            // Act - Add a non-stat condition subscriber (event type)
            var comparisonValues = this.EnsureComparisonValues(statEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });

            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(statEntity);
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = statKey,
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.EventType), // Event type, not stat
                Feature = ConditionFeature.Condition,
                Index = 1,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should not be enabled for non-stat condition types");
        }

        [Test]
        public void EntityWithNonIntrinsicConditionSubscriber_WhenAdded_DoesNotEnableIntrinsicDirty()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var intrinsicEntity = this.CreateIntrinsicEntityWithEventSubscriber(CreateIntrinsicDefault(intrinsicKey, 42));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, false);

            // Act - Add a non-intrinsic condition subscriber (stat type)
            var comparisonValues = this.EnsureComparisonValues(intrinsicEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });

            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(intrinsicEntity);
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = intrinsicKey,
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.StatType), // Stat type, not intrinsic
                Feature = ConditionFeature.Condition,
                Index = 1,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should not be enabled for non-intrinsic condition types");
        }

        [Test]
        public void EntityWithMultipleStatSubscribers_WhenOneStatSubscriberAdded_EnablesStatDirty()
        {
            // Arrange
            var statKey1 = (StatKey)100;
            var statKey2 = (StatKey)200;
            var statEntity = this.CreateStatEntityWithEventSubscriber(
                CreateStatModifier(statKey1, 10, StatModifyType.Added),
                CreateStatModifier(statKey2, 20, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, false);

            // Act - Add multiple subscribers, but only one is stat type
            var comparisonValues = this.EnsureComparisonValues(statEntity);
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(statEntity);

            var eventValueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = statKey1,
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.EventType), // Event type
                Feature = ConditionFeature.Condition,
                Index = 1,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = eventValueIndex },
            });

            var statValueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = statKey2,
                ConditionType = this.statConditionType, // Stat type
                Feature = ConditionFeature.Condition,
                Index = 2,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = statValueIndex },
            });
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should be enabled when at least one stat subscriber is present");
        }

        [Test]
        public void EntityWithBothStatAndIntrinsicBuffers_WhenBothSubscribersAdded_EnablesBothDirtyFlags()
        {
            // Arrange
            var statKey = (StatKey)100;
            var intrinsicKey = (IntrinsicKey)200;
            var combinedEntity = this.CreateCombinedEntity(
                new[] { CreateStatModifier(statKey, 10, StatModifyType.Added) },
                new[] { CreateIntrinsicDefault(intrinsicKey, 20) });
            var subscriberEntity = this.CreateConditionSubscriberEntity(); // Works for both since it has ConditionValues
            this.Manager.SetComponentEnabled<StatConditionDirty>(combinedEntity, false);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(combinedEntity, false);

            // Act - Add both stat and intrinsic subscribers
            this.AddStatSubscriber(combinedEntity, subscriberEntity, statKey, conditionIndex: 1);
            this.AddIntrinsicSubscriber(combinedEntity, subscriberEntity, intrinsicKey, conditionIndex: 2);
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatConditionDirty>(combinedEntity), "StatConditionDirty should be enabled");
            Assert.IsTrue(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(combinedEntity), "IntrinsicConditionDirty should be enabled");
        }

        [Test]
        public void MultipleEntitiesWithNewSubscribers_WhenProcessed_EnablesAppropriateFlags()
        {
            // Arrange
            var statKey = (StatKey)100;
            var intrinsicKey = (IntrinsicKey)200;
            var statEntity = this.CreateStatEntity(CreateStatModifier(statKey, 10, StatModifyType.Added));
            var intrinsicEntity = this.CreateIntrinsicEntity(CreateIntrinsicDefault(intrinsicKey, 20));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, false);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, false);

            // Act - Add subscribers to both entities
            this.AddStatSubscriber(statEntity, subscriberEntity, statKey, conditionIndex: 1);
            this.AddIntrinsicSubscriber(intrinsicEntity, subscriberEntity, intrinsicKey, conditionIndex: 2);
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should be enabled on stat entity");
            Assert.IsTrue(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should be enabled on intrinsic entity");
        }

        [Test]
        public void EntityWithExistingEventSubscribers_WhenNoChanges_DoesNotTriggerDirtyFlags()
        {
            // Arrange
            var statKey = (StatKey)100;
            var statEntity = this.CreateStatEntity(CreateStatModifier(statKey, 42, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Add subscriber initially and run system
            this.AddStatSubscriber(statEntity, subscriberEntity, statKey, conditionIndex: 1);
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Reset the dirty flag
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, false);

            // Act - Run system again without changes to EventSubscriber buffer
            this.RunSystem(this.conditionSubscribedStatSystem);

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should not be enabled when no changes occur to EventSubscriber buffer");
        }

        private void AddStatSubscriber(Entity statEntity, Entity subscriberEntity, StatKey statKey, byte conditionIndex)
        {
            if (!this.Manager.HasBuffer<EventSubscriber>(statEntity))
            {
                this.Manager.AddBuffer<EventSubscriber>(statEntity);
            }

            var comparisonValues = this.EnsureComparisonValues(statEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });

            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(statEntity);
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = statKey,
                ConditionType = this.statConditionType,
                Feature = ConditionFeature.Condition,
                Index = conditionIndex,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });
        }

        private void AddIntrinsicSubscriber(Entity intrinsicEntity, Entity subscriberEntity, IntrinsicKey intrinsicKey, byte conditionIndex)
        {
            if (!this.Manager.HasBuffer<EventSubscriber>(intrinsicEntity))
            {
                this.Manager.AddBuffer<EventSubscriber>(intrinsicEntity);
            }

            var comparisonValues = this.EnsureComparisonValues(intrinsicEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });

            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(intrinsicEntity);
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = intrinsicKey,
                ConditionType = this.intrinsicConditionType,
                Feature = ConditionFeature.Condition,
                Index = conditionIndex,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });
        }

        private Entity CreateStatEntityWithEventSubscriber(params StatModifier[] stats)
        {
            var entity = this.CreateStatEntity(stats);
            this.Manager.AddBuffer<EventSubscriber>(entity);
            this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            return entity;
        }

        private Entity CreateIntrinsicEntityWithEventSubscriber(params IntrinsicBuilder.Default[] intrinsics)
        {
            var entity = this.CreateIntrinsicEntity(intrinsics);
            this.Manager.AddBuffer<EventSubscriber>(entity);
            this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            return entity;
        }

        private DynamicBuffer<ConditionComparisonValue> EnsureComparisonValues(Entity entity)
        {
            return this.Manager.HasBuffer<ConditionComparisonValue>(entity)
                ? this.Manager.GetBuffer<ConditionComparisonValue>(entity)
                : this.Manager.AddBuffer<ConditionComparisonValue>(entity);
        }
    }
}
