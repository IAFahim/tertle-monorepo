// <copyright file="EventIntegrationTests.cs" company="BovineLabs">
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
    using Unity.Entities;

    /// <summary>
    /// Integration tests for end-to-end event workflows, covering event triggering, propagation, and cleanup.
    /// </summary>
    public class EventIntegrationTests : ReactionTestFixture
    {
        private SystemHandle eventWriteSystem;
        private SystemHandle eventResetSystem;
        private byte eventConditionType;
        private ConditionEventWriter.Lookup eventWriterLookup;

        public override void Setup()
        {
            base.Setup();

            this.eventWriteSystem = this.World.CreateSystem<ConditionEventWriteSystem>();
            this.eventResetSystem = this.World.CreateSystem<ConditionEventResetSystem>();
            this.eventConditionType = ConditionTypes.NameToKey(ConditionTypes.EventType);

            // Initialize event writer lookup
            var state = this.WorldUnmanaged.GetExistingSystemState<ConditionEventWriteSystem>();
            this.eventWriterLookup.Create(ref state);
        }

        [Test]
        public void CompleteEventWorkflow_FromTriggerToReset_WorksCorrectly()
        {
            // Arrange - Create a complete event pipeline
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntityWithReset();

            var eventKey = (ushort)100;
            var eventValue = 42;
            var conditionIndex = (byte)3;

            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey, conditionIndex, Equality.Equal, eventValue);

            // Act - Trigger event through ConditionEventWriter
            var eventWriter = this.GetEventWriter(publisherEntity);
            eventWriter.Trigger(new ConditionKey { Value = eventKey }, eventValue);

            // Verify event was written
            Assert.IsTrue(this.Manager.IsComponentEnabled<EventsDirty>(publisherEntity), "EventsDirty should be enabled after trigger");

            // Run event processing
            this.RunEventWriteSystem();

            // Assert - Event should be processed and condition set
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[conditionIndex], "Condition should be set after event processing");
            Assert.IsFalse(this.Manager.IsComponentEnabled<EventsDirty>(publisherEntity), "EventsDirty should be disabled after processing");

            // Act - Run reset system
            this.RunEventResetSystem();

            // Assert - Condition should be reset based on reset mask
            var resetConditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            var resetMask = this.Manager.GetComponentData<ConditionReset>(subscriberEntity);
            var expectedValue = conditionActive.Value.BitAnd(resetMask.Value);
            Assert.AreEqual(expectedValue.Data, resetConditionActive.Value.Data, "Condition should be reset according to reset mask");
        }

        [Test]
        public void MultipleEventsWorkflow_WithDifferentSubscribers_ProcessesAllCorrectly()
        {
            // Arrange - Create multiple publishers and subscribers
            var publisher1 = this.CreateEventPublisherEntity();
            var publisher2 = this.CreateEventPublisherEntity();
            var subscriber1 = this.CreateEventSubscriberEntity();
            var subscriber2 = this.CreateEventSubscriberEntity();

            // Set up event subscriptions
            this.AddEventSubscriber(publisher1, subscriber1, eventKey: 100, conditionIndex: 1, Equality.Equal, value: 10);
            this.AddEventSubscriber(publisher1, subscriber2, eventKey: 200, conditionIndex: 2, Equality.GreaterThan, value: 5);
            this.AddEventSubscriber(publisher2, subscriber1, eventKey: 300, conditionIndex: 3, Equality.LessThan, value: 50);

            // Act - Trigger multiple events
            var eventWriter1 = this.GetEventWriter(publisher1);
            var eventWriter2 = this.GetEventWriter(publisher2);

            eventWriter1.Trigger(new ConditionKey { Value = 100 }, 10); // Should match subscriber1
            eventWriter1.Trigger(new ConditionKey { Value = 200 }, 8);  // Should match subscriber2 (8 > 5)
            eventWriter2.Trigger(new ConditionKey { Value = 300 }, 25); // Should match subscriber1 (25 < 50)

            this.RunEventWriteSystem();

            // Assert - All conditions should be set correctly
            var condition1 = this.Manager.GetComponentData<ConditionActive>(subscriber1);
            var condition2 = this.Manager.GetComponentData<ConditionActive>(subscriber2);

            Assert.IsTrue(condition1.Value[1], "Subscriber1 condition 1 should be set");
            Assert.IsTrue(condition1.Value[3], "Subscriber1 condition 3 should be set");
            Assert.IsTrue(condition2.Value[2], "Subscriber2 condition 2 should be set");
        }

        [Test]
        public void EventPropagationChain_WithConditionalTriggers_WorksEndToEnd()
        {
            // Arrange - Create a chain where events trigger other events
            var primaryPublisher = this.CreateEventPublisherEntity();
            var secondaryPublisher = this.CreateEventPublisherEntity();
            var finalSubscriber = this.CreateEventSubscriberEntity();

            // Secondary publisher also needs to be able to receive conditions (act as subscriber)
            this.Manager.AddComponent<ConditionActive>(secondaryPublisher);
            this.Manager.SetComponentData(secondaryPublisher, new ConditionActive { Value = new BitArray32(0) });

            // Primary event triggers secondary event
            this.AddEventSubscriber(primaryPublisher, secondaryPublisher, eventKey: 100, conditionIndex: 0, Equality.Equal, value: 1);

            // Secondary event triggers final condition
            this.AddEventSubscriber(secondaryPublisher, finalSubscriber, eventKey: 200, conditionIndex: 5, Equality.Equal, value: 2);

            // Act - Trigger primary event
            var primaryWriter = this.GetEventWriter(primaryPublisher);
            primaryWriter.Trigger(new ConditionKey { Value = 100 }, 1);

            // Process first stage
            this.RunEventWriteSystem();

            // Verify first stage worked
            var secondaryCondition = this.Manager.GetComponentData<ConditionActive>(secondaryPublisher);
            Assert.IsTrue(secondaryCondition.Value[0], "Secondary publisher should have condition set");

            // Trigger secondary event based on condition
            if (secondaryCondition.Value[0])
            {
                var secondaryWriter = this.GetEventWriter(secondaryPublisher);
                secondaryWriter.Trigger(new ConditionKey { Value = 200 }, 2);
            }

            // Process second stage
            this.RunEventWriteSystem();

            // Assert - Final subscriber should have condition set
            var finalCondition = this.Manager.GetComponentData<ConditionActive>(finalSubscriber);
            Assert.IsTrue(finalCondition.Value[5], "Final subscriber should have condition set through propagation chain");
        }

        [Test]
        public void EventWithFeatures_AccumulateAndValue_IntegratesCorrectly()
        {
            // Arrange - Create subscriber with value and accumulate features
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntityWithFeature(conditionIndex: 4);

            this.AddEventSubscriberWithFeature(publisherEntity, subscriberEntity, eventKey: 300, conditionIndex: 4,
                ConditionFeature.Value | ConditionFeature.Accumulate, operation: Equality.Equal, value: 100);

            // Pre-populate accumulation value
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            conditionValues[4] = new ConditionValues { Value = 25 };

            // Act - Trigger event that should accumulate
            var eventWriter = this.GetEventWriter(publisherEntity);
            eventWriter.Trigger(new ConditionKey { Value = 300 }, 75); // 75 + 25 = 100, should match

            this.RunEventWriteSystem();

            // Assert - Condition should be set and value should be reset due to accumulate match
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[4], "Condition should be set after accumulate match");

            var finalValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(0, finalValues[4].Value, "Value should be reset to 0 after accumulate match");
        }

        [Test]
        public void EventCleanup_AfterProcessing_ClearsAllData()
        {
            // Arrange - Create entities with multiple events
            var publisherEntity = this.CreateEventPublisherEntity();
            var subscriberEntity = this.CreateEventSubscriberEntity();

            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 100, conditionIndex: 1, Equality.Equal, value: 10);
            this.AddEventSubscriber(publisherEntity, subscriberEntity, eventKey: 200, conditionIndex: 2, Equality.Equal, value: 20);

            var eventWriter = this.GetEventWriter(publisherEntity);
            eventWriter.Trigger(new ConditionKey { Value = 100 }, 10);
            eventWriter.Trigger(new ConditionKey { Value = 200 }, 20);

            // Verify events are present before processing
            var eventBuffer = this.Manager.GetBuffer<ConditionEvent>(publisherEntity).AsMap();
            Assert.AreEqual(2, eventBuffer.Count, "Should have 2 events before processing");
            Assert.IsTrue(this.Manager.IsComponentEnabled<EventsDirty>(publisherEntity), "EventsDirty should be enabled");

            // Act - Process events
            this.RunEventWriteSystem();

            // Assert - All event data should be cleaned up
            eventBuffer = this.Manager.GetBuffer<ConditionEvent>(publisherEntity).AsMap();
            Assert.AreEqual(0, eventBuffer.Count, "Event buffer should be cleared after processing");
            Assert.IsFalse(this.Manager.IsComponentEnabled<EventsDirty>(publisherEntity), "EventsDirty should be disabled");

            // Verify conditions were processed correctly
            var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(conditionActive.Value[1], "Condition 1 should be set");
            Assert.IsTrue(conditionActive.Value[2], "Condition 2 should be set");
        }

        [Test]
        public void EventSystemPerformance_HighVolumeIntegration_ProcessesEfficiently()
        {
            // Arrange - Create high-volume scenario
            const int publisherCount = 5;
            const int subscribersPerPublisher = 10;
            const int eventsPerPublisher = 20;

            var publishers = new Entity[publisherCount];
            var allSubscribers = new Entity[publisherCount * subscribersPerPublisher];

            for (int p = 0; p < publisherCount; p++)
            {
                publishers[p] = this.CreateEventPublisherEntity();

                for (int s = 0; s < subscribersPerPublisher; s++)
                {
                    var subscriberIndex = (p * subscribersPerPublisher) + s;
                    allSubscribers[subscriberIndex] = this.CreateEventSubscriberEntity();

                    this.AddEventSubscriber(publishers[p], allSubscribers[subscriberIndex],
                        eventKey: (ushort)(s * 10), conditionIndex: (byte)(s % 32),
                        operation: Equality.Equal, value: s * 5);
                }
            }

            // Trigger many events
            for (int p = 0; p < publisherCount; p++)
            {
                var eventWriter = this.GetEventWriter(publishers[p]);
                for (int e = 0; e < eventsPerPublisher; e++)
                {
                    eventWriter.Trigger(new ConditionKey { Value = (ushort)(e * 10) }, e * 5);
                }
            }

            // Act - Measure processing time
            var startTime = System.DateTime.UtcNow;
            this.RunEventWriteSystem();
            var endTime = System.DateTime.UtcNow;

            // Assert - Performance and correctness
            var processingTime = endTime - startTime;
            Assert.Less(processingTime.TotalMilliseconds, 500, "High volume processing should complete within reasonable time");

            // Verify some conditions were processed
            int processedSubscribers = 0;
            foreach (var subscriber in allSubscribers)
            {
                var conditionActive = this.Manager.GetComponentData<ConditionActive>(subscriber);
                if (conditionActive.Value.Data != 0)
                {
                    processedSubscribers++;
                }
            }

            Assert.Greater(processedSubscribers, 0, "Some subscribers should have processed conditions");

            // Verify cleanup happened
            foreach (var publisher in publishers)
            {
                var eventBuffer = this.Manager.GetBuffer<ConditionEvent>(publisher).AsMap();
                Assert.AreEqual(0, eventBuffer.Count, "All event buffers should be cleared");
                Assert.IsFalse(this.Manager.IsComponentEnabled<EventsDirty>(publisher), "EventsDirty should be disabled");
            }
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
                typeof(ConditionActive));

            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0) });

            return entity;
        }

        private Entity CreateEventSubscriberEntityWithReset()
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionActive),
                typeof(ConditionReset));

            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0) });

            // Set reset mask to reset all conditions except specific ones (for testing)
            this.Manager.SetComponentData(entity, new ConditionReset { Value = new BitArray32(0xFFFFFFF0) }); // Keep bits 0-3, reset others

            return entity;
        }

        private Entity CreateEventSubscriberEntityWithFeature(byte conditionIndex)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionActive),
                typeof(ConditionValues));

            var entity = this.Manager.CreateEntity(archetype);
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0) });

            var conditionValues = this.Manager.GetBuffer<ConditionValues>(entity);
            for (int i = 0; i <= conditionIndex; i++)
            {
                conditionValues.Add(new ConditionValues { Value = 0 });
            }

            return entity;
        }

        private void AddEventSubscriber(Entity publisherEntity, Entity subscriberEntity, ushort eventKey, byte conditionIndex, Equality operation, int value)
        {
            var comparisonValues = this.Manager.GetBuffer<ConditionComparisonValue>(publisherEntity);
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

        private void AddEventSubscriberWithFeature(Entity publisherEntity, Entity subscriberEntity, ushort eventKey, byte conditionIndex, ConditionFeature feature, Equality operation, int value)
        {
            var comparisonValues = this.Manager.GetBuffer<ConditionComparisonValue>(publisherEntity);
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

        private ConditionEventWriter GetEventWriter(Entity publisherEntity)
        {
            // Update lookup before using
            var state = this.WorldUnmanaged.GetExistingSystemState<ConditionEventWriteSystem>();
            this.eventWriterLookup.Update(ref state);

            return this.eventWriterLookup[publisherEntity];
        }

        private void RunEventWriteSystem()
        {
            this.eventWriteSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }

        private void RunEventResetSystem()
        {
            this.eventResetSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }
    }
}
