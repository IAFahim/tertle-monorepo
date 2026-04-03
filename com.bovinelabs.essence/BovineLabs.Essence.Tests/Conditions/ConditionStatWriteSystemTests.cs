// <copyright file="ConditionStatWriteSystemTests.cs" company="BovineLabs">
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
    /// Tests for ConditionStatWriteSystem, verifying stat value writes to condition system.
    /// </summary>
    public class ConditionStatWriteSystemTests : EssenceTestsFixture
    {
        private SystemHandle conditionStatWriteSystem;
        private byte statConditionType;

        public override void Setup()
        {
            base.Setup();

            this.conditionStatWriteSystem = this.World.CreateSystem<ConditionStatWriteSystem>();
            this.statConditionType = ConditionTypes.NameToKey(ConditionTypes.StatType);
        }

        [Test]
        public void EntityWithStatConditionDirty_WhenStatSubscriberExists_WritesStatValue()
        {
            // Arrange
            var statKey = (StatKey)100;
            var statValue = 42;
            var conditionIndex = (byte)5;

            var statEntity = this.CreateStatEntityWithSubscriber(CreateStatModifier(statKey, statValue, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            this.AddStatSubscriberWithValue(statEntity, subscriberEntity, statKey, conditionIndex, statValue);
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, true);

            // Act
            this.RunConditionStatWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(statValue, conditionValues[conditionIndex].Value, "Condition value should match stat value");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should be cleared");
        }

        [Test]
        public void EntityWithoutStatConditionDirty_WhenStatSubscriberExists_DoesNotWriteStatValue()
        {
            // Arrange
            var statKey = (StatKey)100;
            var conditionIndex = (byte)3;

            var statEntity = this.CreateStatEntityWithSubscriber(CreateStatModifier(statKey, 25, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            this.AddStatSubscriber(statEntity, subscriberEntity, statKey, conditionIndex);
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, false);

            // Initialize condition value to something different
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            conditionValues[conditionIndex] = new ConditionValues { Value = 999 };

            // Act
            this.RunConditionStatWriteSystem();

            // Assert
            conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(999, conditionValues[conditionIndex].Value, "Condition value should remain unchanged");
        }

        [Test]
        public void EntityWithMultipleStatSubscribers_WhenDirty_WritesAllMatchingStatValues()
        {
            // Arrange
            var statKey1 = (StatKey)100;
            var statKey2 = (StatKey)200;
            var statValue1 = 50;
            var statValue2 = 75;

            var statEntity = this.CreateStatEntityWithSubscriber(
                CreateStatModifier(statKey1, statValue1, StatModifyType.Added),
                CreateStatModifier(statKey2, statValue2, StatModifyType.Added));

            var subscriberEntity = this.CreateConditionSubscriberEntity();

            this.AddStatSubscriberWithValue(statEntity, subscriberEntity, statKey1, conditionIndex: 1, statValue1);
            this.AddStatSubscriberWithValue(statEntity, subscriberEntity, statKey2, conditionIndex: 2, statValue2);
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, true);

            // Act
            this.RunConditionStatWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(statValue1, conditionValues[1].Value, "First condition should match first stat");
            Assert.AreEqual(statValue2, conditionValues[2].Value, "Second condition should match second stat");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should be cleared");
        }

        [Test]
        public void EntityWithNonMatchingSubscriber_WhenDirty_DoesNotWriteValue()
        {
            // Arrange
            var statKey = (StatKey)100;
            var differentStatKey = (StatKey)200;
            var conditionIndex = (byte)4;

            var statEntity = this.CreateStatEntityWithSubscriber(CreateStatModifier(statKey, 30, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Subscribe to different stat key
            this.AddStatSubscriber(statEntity, subscriberEntity, differentStatKey, conditionIndex);
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, true);

            // Initialize condition value
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            conditionValues[conditionIndex] = new ConditionValues { Value = 123 };

            // Act
            this.RunConditionStatWriteSystem();

            // Assert
            conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(123, conditionValues[conditionIndex].Value, "Condition value should remain unchanged for non-matching stat");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should still be cleared");
        }

        [Test]
        public void EntityWithNonStatConditionType_WhenDirty_DoesNotWriteValue()
        {
            // Arrange
            var statKey = (StatKey)100;
            var conditionIndex = (byte)6;

            var statEntity = this.CreateStatEntityWithSubscriber(CreateStatModifier(statKey, 60, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            // Add subscriber with different condition type (not stat type)
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(statEntity);
            var comparisonValues = this.EnsureComparisonValues(statEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });

            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = statKey,
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.EventType), // Different type
                Feature = ConditionFeature.Condition,
                Index = conditionIndex,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = valueIndex },
            });

            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, true);

            // Initialize condition value
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            conditionValues[conditionIndex] = new ConditionValues { Value = 456 };

            // Act
            this.RunConditionStatWriteSystem();

            // Assert
            conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(456, conditionValues[conditionIndex].Value, "Condition value should remain unchanged for non-stat condition type");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should still be cleared");
        }

        [Test]
        public void EntityWithZeroStatValue_WhenDirty_WritesZeroValue()
        {
            // Arrange
            var statKey = (StatKey)100;
            var conditionIndex = (byte)7;

            var statEntity = this.CreateStatEntityWithSubscriber(CreateStatModifier(statKey, 0, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            this.AddStatSubscriber(statEntity, subscriberEntity, statKey, conditionIndex);
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, true);

            // Act
            this.RunConditionStatWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(0, conditionValues[conditionIndex].Value, "Zero stat value should be written correctly");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should be cleared");
        }

        [Test]
        public void MultipleEntitiesWithDirtyStats_WhenProcessed_WritesAllCorrectly()
        {
            // Arrange
            var statKey = (StatKey)100;
            var conditionIndex = (byte)8;

            var statEntity1 = this.CreateStatEntityWithSubscriber(CreateStatModifier(statKey, 10, StatModifyType.Added));
            var statEntity2 = this.CreateStatEntityWithSubscriber(CreateStatModifier(statKey, 20, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            this.AddStatSubscriberWithValue(statEntity1, subscriberEntity, statKey, conditionIndex, 10);
            this.AddStatSubscriberWithValue(statEntity2, subscriberEntity, statKey, (byte)(conditionIndex + 1), 20);

            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity1, true);
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity2, true);

            // Act
            this.RunConditionStatWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(10, conditionValues[conditionIndex].Value, "First entity stat should be written");
            Assert.AreEqual(20, conditionValues[conditionIndex + 1].Value, "Second entity stat should be written");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity1), "First entity dirty flag should be cleared");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity2), "Second entity dirty flag should be cleared");
        }

        [Test]
        public void EntityWithFloatBasedStat_WhenDirty_WritesIntegerValue()
        {
            // Arrange
            var statKey = (StatKey)100;
            var floatValue = 42.75f;
            var expectedIntValue = 42; // Should truncate to int
            var conditionIndex = (byte)9;

            var statEntity = this.CreateStatEntityWithSubscriber(CreateStatModifier(statKey, floatValue, StatModifyType.Added));
            var subscriberEntity = this.CreateConditionSubscriberEntity();

            this.AddStatSubscriberWithValue(statEntity, subscriberEntity, statKey, conditionIndex, (int)floatValue);
            this.Manager.SetComponentEnabled<StatConditionDirty>(statEntity, true);

            // Act
            this.RunConditionStatWriteSystem();

            // Assert
            var conditionValues = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(expectedIntValue, conditionValues[conditionIndex].Value, "Float stat value should be truncated to integer");
            Assert.IsFalse(this.Manager.IsComponentEnabled<StatConditionDirty>(statEntity), "StatConditionDirty should be cleared");
        }

        private void AddStatSubscriber(Entity statEntity, Entity subscriberEntity, StatKey statKey, byte conditionIndex)
        {
            var comparisonValues = this.EnsureComparisonValues(subscriberEntity);
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(statEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = 0 });

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

        private void AddStatSubscriberWithValue(Entity statEntity, Entity subscriberEntity, StatKey statKey, byte conditionIndex, int expectedValue)
        {
            var comparisonValues = this.EnsureComparisonValues(subscriberEntity);
            var eventSubscribers = this.Manager.GetBuffer<EventSubscriber>(statEntity);
            var valueIndex = (byte)comparisonValues.Length;
            comparisonValues.Add(new ConditionComparisonValue { Value = expectedValue });

            eventSubscribers.Add(new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Key = statKey,
                ConditionType = this.statConditionType,
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

        private void RunConditionStatWriteSystem()
        {
            this.RunSystems(this.conditionStatWriteSystem);
        }
    }
}
