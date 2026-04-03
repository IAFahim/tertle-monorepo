// <copyright file="ConditionIntrinsicWriteSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>
//
namespace BovineLabs.Essence.Tests.Conditions
{
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;

    /// <summary>
    /// Tests for ConditionIntrinsicWriteSystem, verifying intrinsic value writes to condition system.
    /// </summary>
    public class ConditionIntrinsicWriteSystemTests : EssenceTestsFixture
    {
        private SystemHandle conditionIntrinsicWriteSystem;
        private byte intrinsicConditionType;

        public override void Setup()
        {
            base.Setup();
            this.conditionIntrinsicWriteSystem = this.World.CreateSystem<ConditionIntrinsicWriteSystem>();
            this.intrinsicConditionType = ConditionTypes.NameToKey(ConditionTypes.IntrinsicType);
        }

        [Test]
        public void EntityWithIntrinsicConditionDirty_WhenIntrinsicSubscriberExists_WritesIntrinsicValue()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var intrinsicValue = 42;
            var conditionIndex = (byte)5;
            var intrinsicEntity = this.CreateIntrinsicEntityWithSubscriber(CreateIntrinsicDefault(intrinsicKey, intrinsicValue));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.AddIntrinsicSubscriberWithValue(intrinsicEntity, subscriberEntity, intrinsicKey, conditionIndex, intrinsicValue);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, true);

            // Act
            this.RunConditionIntrinsicWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(intrinsicValue, conditionValues[conditionIndex].Value, "Condition value should match intrinsic value");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should be cleared");
        }

        [Test]
        public void EntityWithoutIntrinsicConditionDirty_WhenIntrinsicSubscriberExists_DoesNotWriteIntrinsicValue()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var conditionIndex = (byte)3;
            var intrinsicEntity = this.CreateIntrinsicEntityWithSubscriber(CreateIntrinsicDefault(intrinsicKey, 25));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.AddIntrinsicSubscriber(intrinsicEntity, subscriberEntity, intrinsicKey, conditionIndex);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, false);

            // Initialize condition value to something different
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            conditionValues[conditionIndex] = new ConditionValues { Value = 999 };

            // Act
            this.RunConditionIntrinsicWriteSystem();

            // Assert
            conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(999, conditionValues[conditionIndex].Value, "Condition value should remain unchanged");
        }

        [Test]
        public void EntityWithMultipleIntrinsicSubscribers_WhenDirty_WritesAllMatchingIntrinsicValues()
        {
            // Arrange
            var intrinsicKey1 = (IntrinsicKey)100;
            var intrinsicKey2 = (IntrinsicKey)200;
            var intrinsicValue1 = 50;
            var intrinsicValue2 = 75;
            var intrinsicEntity = this.CreateIntrinsicEntityWithSubscriber(
                CreateIntrinsicDefault(intrinsicKey1, intrinsicValue1),
                CreateIntrinsicDefault(intrinsicKey2, intrinsicValue2));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.AddIntrinsicSubscriberWithValue(intrinsicEntity, subscriberEntity, intrinsicKey1, conditionIndex: 1, intrinsicValue1);
            this.AddIntrinsicSubscriberWithValue(intrinsicEntity, subscriberEntity, intrinsicKey2, conditionIndex: 2, intrinsicValue2);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, true);

            // Act
            this.RunConditionIntrinsicWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(intrinsicValue1, conditionValues[1].Value, "First condition should match first intrinsic");
            Assert.AreEqual(intrinsicValue2, conditionValues[2].Value, "Second condition should match second intrinsic");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should be cleared");
        }

        [Test]
        public void EntityWithNonMatchingSubscriber_WhenDirty_DoesNotWriteValue()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var differentIntrinsicKey = (IntrinsicKey)200;
            var conditionIndex = (byte)4;
            var intrinsicEntity = this.CreateIntrinsicEntityWithSubscriber(CreateIntrinsicDefault(intrinsicKey, 30));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Subscribe to different intrinsic key
            this.AddIntrinsicSubscriber(intrinsicEntity, subscriberEntity, differentIntrinsicKey, conditionIndex);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, true);

            // Initialize condition value
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            conditionValues[conditionIndex] = new ConditionValues { Value = 123 };

            // Act
            this.RunConditionIntrinsicWriteSystem();

            // Assert
            conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(123, conditionValues[conditionIndex].Value, "Condition value should remain unchanged for non-matching intrinsic");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should still be cleared");
        }

        [Test]
        public void EntityWithNonIntrinsicConditionType_WhenDirty_DoesNotWriteValue()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var conditionIndex = (byte)6;
            var intrinsicEntity = this.CreateIntrinsicEntityWithSubscriber(CreateIntrinsicDefault(intrinsicKey, 60));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Add subscriber with different condition type (not intrinsic type)
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(intrinsicEntity);
            var comparisonValues = this.EnsureComparisonValues(intrinsicEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });

            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = intrinsicKey,
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.StatType), // Different type
                Feature = ConditionFeature.Condition,
                Index = conditionIndex,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, true);

            // Initialize condition value
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            conditionValues[conditionIndex] = new ConditionValues { Value = 456 };

            // Act
            this.RunConditionIntrinsicWriteSystem();

            // Assert
            conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(456, conditionValues[conditionIndex].Value, "Condition value should remain unchanged for non-intrinsic condition type");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should still be cleared");
        }

        [Test]
        public void EntityWithZeroIntrinsicValue_WhenDirty_WritesZeroValue()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var conditionIndex = (byte)7;
            var intrinsicEntity = this.CreateIntrinsicEntityWithSubscriber(CreateIntrinsicDefault(intrinsicKey, 0));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.AddIntrinsicSubscriber(intrinsicEntity, subscriberEntity, intrinsicKey, conditionIndex);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, true);

            // Act
            this.RunConditionIntrinsicWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(0, conditionValues[conditionIndex].Value, "Zero intrinsic value should be written correctly");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should be cleared");
        }

        [Test]
        public void MultipleEntitiesWithDirtyIntrinsics_WhenProcessed_WritesAllCorrectly()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var conditionIndex = (byte)8;
            var intrinsicEntity1 = this.CreateIntrinsicEntityWithSubscriber(CreateIntrinsicDefault(intrinsicKey, 10));
            var intrinsicEntity2 = this.CreateIntrinsicEntityWithSubscriber(CreateIntrinsicDefault(intrinsicKey, 20));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.AddIntrinsicSubscriberWithValue(intrinsicEntity1, subscriberEntity, intrinsicKey, conditionIndex, 10);
            this.AddIntrinsicSubscriberWithValue(intrinsicEntity2, subscriberEntity, intrinsicKey, (byte)(conditionIndex + 1), 20);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity1, true);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity2, true);

            // Act
            this.RunConditionIntrinsicWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(10, conditionValues[conditionIndex].Value, "First entity intrinsic should be written");
            Assert.AreEqual(20, conditionValues[conditionIndex + 1].Value, "Second entity intrinsic should be written");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity1), "First entity dirty flag should be cleared");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity2), "Second entity dirty flag should be cleared");
        }

        [Test]
        public void EntityWithMissingIntrinsic_WhenDirty_WritesDefaultValue()
        {
            // Arrange
            var existingIntrinsicKey = (IntrinsicKey)100;
            var missingIntrinsicKey = (IntrinsicKey)200;
            var conditionIndex = (byte)9;
            var intrinsicEntity = this.CreateIntrinsicEntityWithSubscriber(CreateIntrinsicDefault(existingIntrinsicKey, 42));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Subscribe to a missing intrinsic key
            this.AddIntrinsicSubscriber(intrinsicEntity, subscriberEntity, missingIntrinsicKey, conditionIndex);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, true);

            // Act
            this.RunConditionIntrinsicWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(0, conditionValues[conditionIndex].Value, "Missing intrinsic should write default value (0)");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should be cleared");
        }

        [Test]
        public void EntityWithNegativeIntrinsicValue_WhenDirty_WritesNegativeValue()
        {
            // Arrange
            var intrinsicKey = (IntrinsicKey)100;
            var negativeValue = -15;
            var conditionIndex = (byte)10;
            var intrinsicEntity = this.CreateIntrinsicEntityWithSubscriber(CreateIntrinsicDefault(intrinsicKey, negativeValue));
            var subscriberEntity = this.CreateConditionSubscriberEntity();
            this.AddIntrinsicSubscriberWithValue(intrinsicEntity, subscriberEntity, intrinsicKey, conditionIndex, negativeValue);
            this.Manager.SetComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity, true);

            // Act
            this.RunConditionIntrinsicWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(negativeValue, conditionValues[conditionIndex].Value, "Negative intrinsic value should be written correctly");
            Assert.IsFalse(this.Manager.IsComponentEnabled<IntrinsicConditionDirty>(intrinsicEntity), "IntrinsicConditionDirty should be cleared");
        }

        private void AddIntrinsicSubscriber(Entity intrinsicEntity, Entity subscriberEntity, IntrinsicKey intrinsicKey, byte conditionIndex)
        {
            var comparisonValues = this.EnsureComparisonValues(subscriberEntity);
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(intrinsicEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });

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

        private void AddIntrinsicSubscriberWithValue(Entity intrinsicEntity, Entity subscriberEntity, IntrinsicKey intrinsicKey, byte conditionIndex, int expectedValue)
        {
            var comparisonValues = this.EnsureComparisonValues(subscriberEntity);
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(intrinsicEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = expectedValue });

            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = intrinsicKey,
                ConditionType = this.intrinsicConditionType,
                Feature = ConditionFeature.Value,
                Index = conditionIndex,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });
        }

        private DynamicBuffer<ConditionComparisonValue> EnsureComparisonValues(Entity entity)
        {
            return this.Manager.HasBuffer<ConditionComparisonValue>(entity)
                ? this.Manager.GetBuffer<ConditionComparisonValue>(entity)
                : this.Manager.AddBuffer<ConditionComparisonValue>(entity);
        }

        private void RunConditionIntrinsicWriteSystem()
        {
            this.RunSystem(this.conditionIntrinsicWriteSystem);
        }
    }
}
