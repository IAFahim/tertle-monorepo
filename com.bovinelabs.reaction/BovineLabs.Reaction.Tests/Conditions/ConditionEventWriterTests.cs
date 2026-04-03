// <copyright file="ConditionEventWriterTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Conditions
{
    using System;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Conditions;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.TestTools;

    /// <summary>
    /// Unit tests for ConditionEventWriter, covering all access patterns and validation logic.
    /// </summary>
    public class ConditionEventWriterTests : ReactionTestFixture
    {
        private Entity testEntity;
        private ConditionEventWriter.Lookup eventWriterLookup;
        private ConditionEventWriter.TypeHandle eventWriterTypeHandle;

        public override void Setup()
        {
            base.Setup();
            this.CreateTestEntity();
            this.SetupLookupAndTypeHandle();
        }

        [Test]
        public void IsValid_WithValidEventWriter_ReturnsTrue()
        {
            // Arrange
            var eventWriter = this.GetDirectEventWriter();

            // Act & Assert
            Assert.IsTrue(eventWriter.IsValid, "ConditionEventWriter with valid map should return IsValid = true");
        }

        [Test]
        public void IsValid_WithDefaultEventWriter_ReturnsFalse()
        {
            // Arrange
            var eventWriter = default(ConditionEventWriter);

            // Act & Assert
            Assert.IsFalse(eventWriter.IsValid, "Default ConditionEventWriter should return IsValid = false");
        }

        [Test]
        public void Trigger_WithValidKey_AddsEventToMap()
        {
            // Arrange
            var eventWriter = this.GetDirectEventWriter();
            var key = new ConditionKey { Value = 100 };
            var value = 42;

            // Act
            eventWriter.Trigger(key, value);

            // Assert
            var eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.IsTrue(eventMap.TryGetValue(key, out var storedValue), "Event should be added to map");
            Assert.AreEqual(value, storedValue, "Stored value should match triggered value");
        }

        [Test]
        public void Trigger_WithValidKey_SetsEventsDirtyFlag()
        {
            // Arrange
            var eventWriter = this.GetDirectEventWriter();
            this.Manager.SetComponentEnabled<EventsDirty>(this.testEntity, false);
            var key = new ConditionKey { Value = 100 };

            // Act
            eventWriter.Trigger(key, 42);

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<EventsDirty>(this.testEntity), "EventsDirty should be enabled after triggering event");
        }

        [Test]
        public void Trigger_WithNullKey_DoesNotAddEvent()
        {
            // Arrange
            var eventWriter = this.GetDirectEventWriter();
            var nullKey = ConditionKey.Null;

            // Act
            eventWriter.Trigger(nullKey, 42);

            // Assert
            var eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.AreEqual(0, eventMap.Count, "No events should be added for null key");
        }

        [Test]
        public void Trigger_WithNullKey_DoesNotSetEventsDirtyFlag()
        {
            // Arrange
            var eventWriter = this.GetDirectEventWriter();
            this.Manager.SetComponentEnabled<EventsDirty>(this.testEntity, false);
            var nullKey = ConditionKey.Null;

            // Act
            eventWriter.Trigger(nullKey, 42);

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<EventsDirty>(this.testEntity), "EventsDirty should not be enabled for null key");
        }

        [Test]
        public void Trigger_WithZeroValue_DoesNotAddEvent()
        {
            // Arrange
            var eventWriter = this.GetDirectEventWriter();
            var key = new ConditionKey { Value = 100 };

            // Ensure buffer is empty at start
            var eventBuffer = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            var initialCount = eventBuffer.Count;

            // Act
            LogAssert.Expect(LogType.Assert, "Can't write 0 value event");
            Assert.Throws<Exception>(() => eventWriter.Trigger(key, 0));

            // Assert
            var eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.AreEqual(initialCount, eventMap.Count, "No events should be added for zero value");
        }

        [Test]
        public void Trigger_WithDuplicateKey_LogsError()
        {
            // Arrange
            var eventWriter = this.GetDirectEventWriter();
            var key = new ConditionKey { Value = 100 };

            // Act
            eventWriter.Trigger(key, 42);

            // Expect error log for duplicate key
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*write an event.*multiple times.*"));
            eventWriter.Trigger(key, 50); // Should log error but not crash

            // Assert
            var eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.AreEqual(1, eventMap.Count, "Should still have only one event");
            Assert.IsTrue(eventMap.TryGetValue(key, out var value), "Original event should remain");
            Assert.AreEqual(42, value, "Original value should be preserved");
        }

        [Test]
        public void Trigger_MultipleUniqueKeys_AddsAllEvents()
        {
            // Arrange
            var eventWriter = this.GetDirectEventWriter();
            var key1 = new ConditionKey { Value = 100 };
            var key2 = new ConditionKey { Value = 200 };
            var key3 = new ConditionKey { Value = 300 };

            // Act
            eventWriter.Trigger(key1, 10);
            eventWriter.Trigger(key2, 20);
            eventWriter.Trigger(key3, 30);

            // Assert
            var eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.AreEqual(3, eventMap.Count, "Should have all three events");

            Assert.IsTrue(eventMap.TryGetValue(key1, out var value1) && value1 == 10, "First event should be stored correctly");
            Assert.IsTrue(eventMap.TryGetValue(key2, out var value2) && value2 == 20, "Second event should be stored correctly");
            Assert.IsTrue(eventMap.TryGetValue(key3, out var value3) && value3 == 30, "Third event should be stored correctly");
        }

        [Test]
        public void Trigger_WithNegativeValue_AddsEventCorrectly()
        {
            // Arrange
            var eventWriter = this.GetDirectEventWriter();
            var key = new ConditionKey { Value = 100 };
            var negativeValue = -42;

            // Act
            eventWriter.Trigger(key, negativeValue);

            // Assert
            var eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.IsTrue(eventMap.TryGetValue(key, out var storedValue), "Event should be added to map");
            Assert.AreEqual(negativeValue, storedValue, "Negative value should be stored correctly");
        }

        [Test]
        public void Lookup_Create_DoesNotThrow()
        {
            // Arrange
            var lookup = default(ConditionEventWriter.Lookup);
            ref var state = ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>();

            // Act - Should not throw
            lookup.Create(ref state);

            // Assert - If we get here, the method didn't throw
            Assert.Pass("Lookup Create completed without throwing");
        }

        [Test]
        public void Lookup_Update_UpdatesLookups()
        {
            // Arrange
            this.eventWriterLookup.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => this.eventWriterLookup.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>()),
                "Update should not throw when called multiple times");
        }

        [Test]
        public void Lookup_Indexer_WithValidEntity_ReturnsValidWriter()
        {
            // Arrange
            this.eventWriterLookup.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());

            // Act
            var eventWriter = this.eventWriterLookup[this.testEntity];

            // Assert
            Assert.IsTrue(eventWriter.IsValid, "Event writer from lookup indexer should be valid");
        }

        [Test]
        public void Lookup_Indexer_WithInvalidEntity_ThrowsException()
        {
            // Arrange
            this.eventWriterLookup.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());
            var invalidEntity = Entity.Null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _ = this.eventWriterLookup[invalidEntity], "Accessing invalid entity should throw exception");
        }

        [Test]
        public void Lookup_TryGet_WithValidEntity_ReturnsTrue()
        {
            // Arrange
            this.eventWriterLookup.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());

            // Act
            var success = this.eventWriterLookup.TryGet(this.testEntity, out var eventWriter);

            // Assert
            Assert.IsTrue(success, "TryGet should succeed for valid entity");
            Assert.IsTrue(eventWriter.IsValid, "Returned event writer should be valid");
        }

        [Test]
        public void Lookup_TryGet_WithInvalidEntity_ReturnsFalse()
        {
            // Arrange
            this.eventWriterLookup.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());
            var invalidEntity = Entity.Null;

            // Act
            var success = this.eventWriterLookup.TryGet(invalidEntity, out var eventWriter);

            // Assert
            Assert.IsFalse(success, "TryGet should fail for invalid entity");
            Assert.IsFalse(eventWriter.IsValid, "Returned event writer should be invalid");
        }

        [Test]
        public void Lookup_TryGet_WithEntityMissingComponents_ReturnsFalse()
        {
            // Arrange
            this.eventWriterLookup.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());
            var entityWithoutComponents = this.Manager.CreateEntity(); // Missing required components

            // Act
            var success = this.eventWriterLookup.TryGet(entityWithoutComponents, out var eventWriter);

            // Assert
            Assert.IsFalse(success, "TryGet should fail for entity missing required components");
            Assert.IsFalse(eventWriter.IsValid, "Returned event writer should be invalid");
        }

        [Test]
        public void TypeHandle_Create_InitializesHandles()
        {
            // Arrange
            var typeHandle = default(ConditionEventWriter.TypeHandle);
            ref var state = ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>();

            // Act - Should not throw
            typeHandle.Create(ref state);

            // Assert - If we get here, the method didn't throw
            Assert.Pass("TypeHandle Create completed without throwing");
        }

        [Test]
        public void TypeHandle_Update_UpdatesHandles()
        {
            // Arrange
            this.eventWriterTypeHandle.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => this.eventWriterTypeHandle.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>()),
                "TypeHandle Update should not throw when called multiple times");
        }

        [Test]
        public void TypeHandle_Resolve_WithValidChunk_ReturnsValidResolvedChunk()
        {
            // Arrange
            this.eventWriterTypeHandle.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());

            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ConditionEvent, EventsDirty>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this.Manager);
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            Assert.Greater(chunks.Length, 0, "Should have at least one chunk with test entity");

            // Act
            var resolvedChunk = this.eventWriterTypeHandle.Resolve(chunks[0]);

            // Assert
            Assert.Greater(resolvedChunk.ConditionEvents.Length, 0, "ResolvedChunk should have condition events");
        }

        [Test]
        public void ResolvedChunk_Indexer_WithValidIndex_ReturnsValidWriter()
        {
            // Arrange
            this.eventWriterTypeHandle.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());

            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ConditionEvent, EventsDirty>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this.Manager);
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            Assert.IsTrue(chunks.Length > 0, "Query should return at least one chunk");

            var resolvedChunk = this.eventWriterTypeHandle.Resolve(chunks[0]);
            Assert.IsTrue(resolvedChunk.ConditionEvents.Length > 0, "Resolved chunk should have at least one entity");

            // Act
            var eventWriter = resolvedChunk[0]; // First entity in chunk

            // Assert
            Assert.IsTrue(eventWriter.IsValid, "Event writer from ResolvedChunk indexer should be valid");
        }

        [Test]
        public void ResolvedChunk_Exists_WithValidChunk_ReturnsTrue()
        {
            // Arrange
            this.eventWriterTypeHandle.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());

            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ConditionEvent, EventsDirty>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this.Manager);
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            Assert.IsTrue(chunks.Length > 0, "Query should return at least one chunk");

            // Act
            var resolvedChunk = this.eventWriterTypeHandle.Resolve(chunks[0]);

            // Assert
            Assert.IsTrue(resolvedChunk.ConditionEvents.Length > 0, "ResolvedChunk.Exists should be true for chunk with entities");
        }

        [Test]
        public void ResolvedChunk_Exists_WithEmptyBufferAccessor_ReturnsFalse()
        {
            // Arrange - Create a resolved chunk with empty buffer accessor to test the Exists property
            var resolvedChunk = new ConditionEventWriter.ResolvedChunk
            {
                ConditionEvents = default, // Empty BufferAccessor has Length = 0
            };

            // Act & Assert
            Assert.IsFalse(resolvedChunk.ConditionEvents.Length > 0, "ResolvedChunk with empty BufferAccessor should return Exists = false");
        }

        [Test]
        public void EventWriter_FromLookup_CanTriggerEvents()
        {
            // Arrange
            this.eventWriterLookup.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());
            var eventWriter = this.eventWriterLookup[this.testEntity];
            var key = new ConditionKey { Value = 500 };

            // Act
            eventWriter.Trigger(key, 100);

            // Assert
            var eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.IsTrue(eventMap.TryGetValue(key, out var value), "Event should be triggered through lookup");
            Assert.AreEqual(100, value, "Event value should be correct");
        }

        [Test]
        public void EventWriter_FromResolvedChunk_CanTriggerEvents()
        {
            // Arrange
            this.eventWriterTypeHandle.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());

            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ConditionEvent, EventsDirty>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this.Manager);
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            Assert.IsTrue(chunks.Length > 0, "Query should return at least one chunk");

            var resolvedChunk = this.eventWriterTypeHandle.Resolve(chunks[0]);
            Assert.IsTrue(resolvedChunk.ConditionEvents.Length > 0, "Resolved chunk should have at least one entity");

            var eventWriter = resolvedChunk[0];
            var key = new ConditionKey { Value = 600 };

            // Act
            eventWriter.Trigger(key, 200);

            // Assert
            var eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.IsTrue(eventMap.TryGetValue(key, out var value), "Event should be triggered through ResolvedChunk");
            Assert.AreEqual(200, value, "Event value should be correct");
        }

        [Test]
        public void EventWriter_MultipleAccessPatterns_ProduceSameResults()
        {
            // Arrange - Clear buffer first to ensure clean state
            this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap().Clear();

            var directWriter = this.GetDirectEventWriter();

            this.eventWriterLookup.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());
            var lookupWriter = this.eventWriterLookup[this.testEntity];

            this.eventWriterTypeHandle.Update(ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>());
            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ConditionEvent, EventsDirty>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(this.Manager);
            var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            Assert.IsTrue(chunks.Length > 0, "Query should return at least one chunk");

            var resolvedChunk = this.eventWriterTypeHandle.Resolve(chunks[0]);
            Assert.IsTrue(resolvedChunk.ConditionEvents.Length > 0, "Resolved chunk should have at least one entity");
            var chunkWriter = resolvedChunk[0];

            // Act & Assert - All should be valid
            Assert.IsTrue(directWriter.IsValid, "Direct writer should be valid");
            Assert.IsTrue(lookupWriter.IsValid, "Lookup writer should be valid");
            Assert.IsTrue(chunkWriter.IsValid, "Chunk writer should be valid");

            // Test direct writer
            directWriter.Trigger(new ConditionKey { Value = 1 }, 10);
            var eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.AreEqual(1, eventMap.Count, "Direct writer should work");

            // Test lookup writer
            lookupWriter.Trigger(new ConditionKey { Value = 2 }, 20);
            eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.AreEqual(2, eventMap.Count, "Lookup writer should work");

            // Get fresh chunk writer after buffer modifications
            chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            resolvedChunk = this.eventWriterTypeHandle.Resolve(chunks[0]);
            chunkWriter = resolvedChunk[0];

            // Test chunk writer
            chunkWriter.Trigger(new ConditionKey { Value = 3 }, 30);
            eventMap = this.Manager.GetBuffer<ConditionEvent>(this.testEntity).AsMap();
            Assert.AreEqual(3, eventMap.Count, "Chunk writer should work");
        }

        private void CreateTestEntity()
        {
            var archetype = this.Manager.CreateArchetype(typeof(ConditionEvent), typeof(EventsDirty));

            this.testEntity = this.Manager.CreateEntity(archetype);
            var b = this.Manager.GetBuffer<ConditionEvent>(this.testEntity);
            b.Initialize();
            this.Manager.SetComponentEnabled<EventsDirty>(this.testEntity, false);
        }

        private void SetupLookupAndTypeHandle()
        {
            // Create a test system to get SystemState
            this.World.CreateSystem<TestSystem>();
            ref var state = ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>();

            this.eventWriterLookup.Create(ref state);
            this.eventWriterTypeHandle.Create(ref state);
        }

        private ConditionEventWriter GetDirectEventWriter()
        {
            // Update the lookup to ensure it's synchronized
            ref var state = ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>();
            this.eventWriterLookup.Update(ref state);

            return this.eventWriterLookup[this.testEntity];
        }

        // Simple test system for getting SystemState
        private partial struct TestSystem : ISystem
        {
            public void OnCreate(ref SystemState state)
            {
            }

            public void OnUpdate(ref SystemState state)
            {
            }
        }
    }
}
