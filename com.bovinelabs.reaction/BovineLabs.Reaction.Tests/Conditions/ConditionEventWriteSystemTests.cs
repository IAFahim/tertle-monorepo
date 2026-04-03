// <copyright file="ConditionEventWriteSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>
//
namespace BovineLabs.Reaction.Tests.Conditions
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Tests for ConditionEventWriteSystem, verifying event processing, condition state updates, and value management.
    /// </summary>
    public class ConditionEventWriteSystemTests : ReactionTestFixture
    {
        private SystemHandle eventWriteSystem;
        private byte eventConditionType;

        public override void Setup()
        {
            base.Setup();
            this.eventWriteSystem = this.World.CreateSystem<ConditionEventWriteSystem>();
            this.eventConditionType = ConditionTypes.NameToKey(ConditionTypes.EventType);
        }

        [Test]
        public void EntityWithEventSubscriber_WhenMatchingEventExists_SetsConditionBit()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 100, conditionIndex: 5, operation: Equality.Equal, value: 42);
            this.AddConditionEvent(publisherEntity, eventKey: 100, eventValue: 42);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[5], "Condition bit 5 should be set when event matches");
        }

        [Test]
        public void EntityWithEventSubscriber_WhenEventDoesNotMatch_DoesNotSetConditionBit()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 100, conditionIndex: 3, operation: Equality.Equal, value: 42);
            this.AddConditionEvent(publisherEntity, eventKey: 100, eventValue: 99); // Different value

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsFalse(conditionActive.Value[3], "Condition bit 3 should not be set when event doesn't match");
        }

        [Test]
        public void EntityWithEventSubscriber_WhenNoMatchingEventExists_DoesNotSetConditionBit()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 100, conditionIndex: 2, operation: Equality.Equal, value: 42);
            this.AddConditionEvent(publisherEntity, eventKey: 200, eventValue: 42); // Different key

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsFalse(conditionActive.Value[2], "Condition bit 2 should not be set when no matching event exists");
        }

        [Test]
        public void EventWithEqualityEqual_WithMatchingValue_SetsConditionBit()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 50, conditionIndex: 1, operation: Equality.Equal, value: 25);
            this.AddConditionEvent(publisherEntity, eventKey: 50, eventValue: 25);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[1], "Condition should be set for equal values");
        }

        [Test]
        public void EventWithEqualityNotEqual_WithDifferentValue_SetsConditionBit()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 50, conditionIndex: 1, operation: Equality.NotEqual, value: 25);
            this.AddConditionEvent(publisherEntity, eventKey: 50, eventValue: 30);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[1], "Condition should be set for not equal values");
        }

        [Test]
        public void EventWithEqualityGreaterThan_WithGreaterValue_SetsConditionBit()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 50, conditionIndex: 1, operation: Equality.GreaterThan, value: 25);
            this.AddConditionEvent(publisherEntity, eventKey: 50, eventValue: 30);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[1], "Condition should be set for greater than comparison");
        }

        [Test]
        public void EventWithEqualityLessThan_WithSmallerValue_SetsConditionBit()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 50, conditionIndex: 1, operation: Equality.LessThan, value: 25);
            this.AddConditionEvent(publisherEntity, eventKey: 50, eventValue: 20);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[1], "Condition should be set for less than comparison");
        }

        [Test]
        public void EventWithEqualityBetween_WithValueInRange_SetsConditionBit()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriberWithRange(publisherEntity, subscriberEntity, eventKey: 50, conditionIndex: 1, operation: Equality.Between, minValue: 20, maxValue: 30);
            this.AddConditionEvent(publisherEntity, eventKey: 50, eventValue: 25);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[1], "Condition should be set for value within range");
        }

        [Test]
        public void EntityWithValueFeature_WhenEventMatches_StoresValueInConditionValues()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntityWithFeature(conditionIndex: 4);
            this.AddEventSubscriberWithFeature(publisherEntity, subscriberEntity, eventKey: 100, conditionIndex: 4, ConditionFeature.Value | ConditionFeature.Condition, operation: Equality.Equal, value: 150);
            this.AddConditionEvent(publisherEntity, eventKey: 100, eventValue: 150);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(150, conditionValues[4].Value, "Value should be stored in ConditionValues");
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[4], "Condition bit should also be set");
        }

        [Test]
        public void EntityWithAccumulateFeature_WhenMultipleEventsMatch_AccumulatesValues()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntityWithFeature(conditionIndex: 6);
            this.AddEventSubscriberWithFeature(publisherEntity, subscriberEntity, eventKey: 200, conditionIndex: 6, ConditionFeature.Accumulate, operation: Equality.Equal, value: 15);

            // Pre-set an existing value in ConditionValues to test accumulation
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            conditionValues[6] = new ConditionValues { Value = 5 };
            this.AddConditionEvent(publisherEntity, eventKey: 200, eventValue: 10);

            // Act
            this.RunEventWriteSystem();

            // Assert
            conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(0, conditionValues[6].Value, "Accumulate should reset value to 0 after match (per system behavior)");
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[6], "Condition bit should be set after accumulation match");
        }

        [Test]
        public void EntityWithValueOnlyFeature_WhenEventExists_StoresValueButDoesNotSetCondition()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntityWithFeature(conditionIndex: 7);
            this.AddEventSubscriberWithFeature(publisherEntity, subscriberEntity, eventKey: 300, conditionIndex: 7, ConditionFeature.Value, operation: Equality.Equal, value: 75);
            this.AddConditionEvent(publisherEntity, eventKey: 300, eventValue: 75);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(75, conditionValues[7].Value, "Value should be stored even without condition feature");
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsFalse(conditionActive.Value[7], "Condition bit should not be set for Value-only feature");
        }

        [Test]
        public void EntityWithMultipleSubscribers_WhenDifferentEventsMatch_UpdatesCorrectConditions()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();

            // Add multiple subscribers to same publisher
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 400, conditionIndex: 2, operation: Equality.Equal, value: 100);
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 500, conditionIndex: 8, operation: Equality.Equal, value: 200);

            // Add events that match both subscribers
            this.AddConditionEvent(publisherEntity, eventKey: 400, eventValue: 100);
            this.AddConditionEvent(publisherEntity, eventKey: 500, eventValue: 200);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[2], "First condition should be set");
            Assert.IsTrue(conditionActive.Value[8], "Second condition should be set");
        }

        [Test]
        public void EntityWithNonEventConditionType_ShouldBeSkipped()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();

            // Create subscriber with different condition type (not event type)
            var comparisonValues = this.Manager.GetBuffer<ConditionComparisonValue>(publisherEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 50 });

            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(publisherEntity);
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = 100,
                ConditionType = (byte)(this.eventConditionType + 1), // Different type
                Feature = ConditionFeature.Condition,
                Index = 1,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });
            this.AddConditionEvent(publisherEntity, eventKey: 100, eventValue: 50);

            // Act
            this.RunEventWriteSystem();

            // Assert
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsFalse(conditionActive.Value[1], "Condition should not be set for non-event condition type");
        }

        [Test]
        public void AfterProcessing_EventsDirtyFlagShouldBeDisabled()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 100, conditionIndex: 1, operation: Equality.Equal, value: 50);
            this.AddConditionEvent(publisherEntity, eventKey: 100, eventValue: 50);

            // Ensure EventsDirty is initially enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<EventsDirty>(publisherEntity), "EventsDirty should be enabled before processing");

            // Act
            this.RunEventWriteSystem();

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<EventsDirty>(publisherEntity), "EventsDirty should be disabled after processing");
        }

        [Test]
        public void AfterProcessing_ConditionEventBufferShouldBeCleared()
        {
            // Arrange
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 100, conditionIndex: 1, operation: Equality.Equal, value: 50);
            this.AddConditionEvent(publisherEntity, eventKey: 100, eventValue: 50);
            var conditionEventBuffer = this.Manager.GetBuffer<ConditionEvent>(publisherEntity).AsMap();
            Assert.IsTrue(conditionEventBuffer.Count > 0, "ConditionEvent buffer should have events before processing");

            // Act
            this.RunEventWriteSystem();

            // Assert
            conditionEventBuffer = this.Manager.GetBuffer<ConditionEvent>(publisherEntity).AsMap();
            Assert.AreEqual(0, conditionEventBuffer.Count, "ConditionEvent buffer should be cleared after processing");
        }

        [Test]
        public void SystemPerformance_WithManySubscribersAndEvents_ProcessesEfficiently()
        {
            // Arrange - Create multiple publishers and subscribers
            const int publisherCount = 10;
            const int subscribersPerPublisher = 5;
            const int eventsPerPublisher = 3;
            for (int p = 0; p < publisherCount; p++)
            {
                var publisherEntity = this.CreateEventPublisherEntity();
                for (int s = 0; s < subscribersPerPublisher; s++)
                {
                    var subscriberEntity = this.CreateEventSubscriberEntity();
                    this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: (ushort)(s * 10), conditionIndex: (byte)s, operation: Equality.Equal, value: s * 5);
                }

                for (int e = 0; e < eventsPerPublisher; e++)
                {
                    this.AddConditionEvent(publisherEntity, eventKey: (ushort)(e * 10), eventValue: e * 5);
                }
            }

            // Act
            var startTime = System.DateTime.UtcNow;
            this.RunEventWriteSystem();
            var endTime = System.DateTime.UtcNow;

            // Assert - Should complete in reasonable time (basic performance check)
            var processingTime = endTime - startTime;
            Assert.Less(processingTime.TotalMilliseconds, 100, "System should process efficiently");

            // Verify some conditions were set correctly
            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ConditionActive>()
                .Build(this.Manager);
            var entities = query.ToEntityArray(Allocator.Temp);
            var processedCount = 0;
            foreach (var entity in entities)
            {
                var conditionActive = this.Manager.GetComponentData<ConditionActive>(entity);
                if (conditionActive.Value.Data != 0)
                {
                    processedCount++;
                }
            }

            Assert.Greater(processedCount, 0, "Some conditions should have been processed and set");
        }

        private Entity CreateEventPublisherEntity()
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(EventSubscriber),
                typeof(ConditionEvent),
                typeof(ConditionComparisonValue),
                typeof(EventsDirty));
            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.GetBuffer<ConditionEvent>(entity).Initialize();
            this.Manager.SetComponentEnabled<EventsDirty>(entity, false);
            return entity;
        }

        private Entity CreateEventSubscriberEntity()
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionActive),
                typeof(ConditionComparisonValue));
            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0) });
            return entity;
        }

        private Entity CreateEventSubscriberEntityWithFeature(byte conditionIndex)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionActive),
                typeof(ConditionComparisonValue),
                typeof(ConditionValues));
            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0) });

            // Initialize ConditionValues buffer with enough capacity
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(entity);
            for (int i = 0; i <= conditionIndex; i++)
            {
                conditionValues.Add(new ConditionValues { Value = 0 });
            }

            return entity;
        }

        private void AddEventSubscriber(Entity publisherEntity, Entity subscriberEntity, ushort eventKey, byte conditionIndex, Equality operation, int value)
        {
            var comparisonValues = this.Manager.GetBuffer<ConditionComparisonValue>(subscriberEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = value });

            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(publisherEntity);
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = eventKey,
                ConditionType = this.eventConditionType,
                Feature = ConditionFeature.Condition,
                Index = conditionIndex,
                Operation = operation,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });
        }

        private void AddEventSubscriberWithRange(Entity publisherEntity, Entity subscriberEntity, ushort eventKey, byte conditionIndex, Equality operation, int minValue, int maxValue)
        {
            var comparisonValues = this.Manager.GetBuffer<ConditionComparisonValue>(subscriberEntity);
            var minIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = minValue });
            var maxIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = maxValue });

            var valueIndex = default(ValueIndex);
            valueIndex.Min = minIndex;
            valueIndex.Max = maxIndex;

            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(publisherEntity);
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = eventKey,
                ConditionType = this.eventConditionType,
                Feature = ConditionFeature.Condition,
                Index = conditionIndex,
                Operation = operation,
                ValueIndex = valueIndex,
            });
        }

        private void AddEventSubscriberWithFeature(Entity publisherEntity, Entity subscriberEntity, ushort eventKey, byte conditionIndex, ConditionFeature feature, Equality operation, int value)
        {
            var comparisonValues = this.Manager.GetBuffer<ConditionComparisonValue>(subscriberEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = value });

            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(publisherEntity);
            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = eventKey,
                ConditionType = this.eventConditionType,
                Feature = feature,
                Index = conditionIndex,
                Operation = operation,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });
        }

        private void AddConditionEvent(Entity publisherEntity, ushort eventKey, int eventValue)
        {
            var conditionEventBuffer = this.Manager.GetBuffer<ConditionEvent>(publisherEntity).AsMap();
            conditionEventBuffer.TryAdd(new ConditionKey { Value = eventKey }, eventValue);
            this.Manager.SetComponentEnabled<EventsDirty>(publisherEntity, true);
        }

        private void RunEventWriteSystem()
        {
            this.eventWriteSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }
    }
}
