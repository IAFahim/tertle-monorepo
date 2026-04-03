// <copyright file="ReactionUtilTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Core
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Reaction.Core;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Tests for ReactionUtil static methods that provide utility functions for target handling and state management.
    /// </summary>
    public class ReactionUtilTests : ReactionTestFixture
    {
        [Test]
        public void GetUniqueTargets_DynamicBuffer_EmptyBuffer_ReturnsEmpty()
        {
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<TestAction>(entity);

            var result = ReactionUtil.GetUniqueTargets(buffer);

            Assert.AreEqual(0, result.Length, "Empty buffer should return empty list");
        }

        [Test]
        public void GetUniqueTargets_DynamicBuffer_SingleTarget_ReturnsOne()
        {
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<TestAction>(entity);
            buffer.Add(new TestAction(Target.Owner));

            var result = ReactionUtil.GetUniqueTargets(buffer);

            Assert.AreEqual(1, result.Length, "Single target should return one entry");
            Assert.AreEqual(Target.Owner, result[0].Target, "Target should match");
            Assert.AreEqual(1, result[0].Count, "Count should be 1");
        }

        [Test]
        public void GetUniqueTargets_DynamicBuffer_DuplicateTargets_CountsCorrectly()
        {
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<TestAction>(entity);
            buffer.Add(new TestAction(Target.Owner));
            buffer.Add(new TestAction(Target.Source));
            buffer.Add(new TestAction(Target.Owner));
            buffer.Add(new TestAction(Target.Owner));

            var result = ReactionUtil.GetUniqueTargets(buffer);

            Assert.AreEqual(2, result.Length, "Should have two unique targets");
            Assert.AreEqual(Target.Owner, result[0].Target, "First target should be Owner (first encountered)");
            Assert.AreEqual(3, result[0].Count, "Owner should have count of 3");
            Assert.AreEqual(Target.Source, result[1].Target, "Second target should be Source (second encountered)");
            Assert.AreEqual(1, result[1].Count, "Source should have count of 1");
        }

        [Test]
        public void GetUniqueTargets_DynamicBuffer_MultipleUniqueTargets_ReturnsAll()
        {
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<TestAction>(entity);
            buffer.Add(new TestAction(Target.None));
            buffer.Add(new TestAction(Target.Target));
            buffer.Add(new TestAction(Target.Owner));
            buffer.Add(new TestAction(Target.Source));
            buffer.Add(new TestAction(Target.Self));

            var result = ReactionUtil.GetUniqueTargets(buffer);

            Assert.AreEqual(5, result.Length, "Should have five unique targets");

            var targets = new Target[5];
            var counts = new byte[5];
            for (int i = 0; i < result.Length; i++)
            {
                targets[i] = result[i].Target;
                counts[i] = result[i].Count;
            }

            Assert.Contains(Target.None, targets, "Should contain None target");
            Assert.Contains(Target.Target, targets, "Should contain Target target");
            Assert.Contains(Target.Owner, targets, "Should contain Owner target");
            Assert.Contains(Target.Source, targets, "Should contain Source target");
            Assert.Contains(Target.Self, targets, "Should contain Self target");

            foreach (var count in counts)
            {
                Assert.AreEqual(1, count, "Each unique target should have count of 1");
            }
        }

        [Test]
        public void GetUniqueTargets_NativeArray_EmptyArray_ReturnsEmpty()
        {
            using var array = new NativeArray<TestAction>(0, Allocator.Temp);

            var result = ReactionUtil.GetUniqueTargets(array);

            Assert.AreEqual(0, result.Length, "Empty array should return empty list");
        }

        [Test]
        public void GetUniqueTargets_NativeArray_SingleTarget_ReturnsOne()
        {
            var array = new NativeArray<TestAction>(1, Allocator.Temp);
            array[0] = new TestAction(Target.Target);

            var result = ReactionUtil.GetUniqueTargets(array);

            Assert.AreEqual(1, result.Length, "Single target should return one entry");
            Assert.AreEqual(Target.Target, result[0].Target, "Target should match");
            Assert.AreEqual(1, result[0].Count, "Count should be 1");

            array.Dispose();
        }

        [Test]
        public void GetUniqueTargets_NativeArray_DuplicateTargets_CountsCorrectly()
        {
            var array = new NativeArray<TestAction>(6, Allocator.Temp);
            array[0] = new TestAction(Target.Self);
            array[1] = new TestAction(Target.Owner);
            array[2] = new TestAction(Target.Self);
            array[3] = new TestAction(Target.Source);
            array[4] = new TestAction(Target.Self);
            array[5] = new TestAction(Target.Owner);

            var result = ReactionUtil.GetUniqueTargets(array);

            (Target Target, byte Count) selfEntry = default;
            (Target Target, byte Count) ownerEntry = default;
            (Target Target, byte Count) sourceEntry = default;

            for (int i = 0; i < result.Length; i++)
            {
                switch (result[i].Target)
                {
                    case Target.Self:
                        selfEntry = result[i];
                        break;
                    case Target.Owner:
                        ownerEntry = result[i];
                        break;
                    case Target.Source:
                        sourceEntry = result[i];
                        break;
                }
            }

            Assert.AreEqual(Target.Self, selfEntry.Target, "Self target should be found");
            Assert.AreEqual(3, selfEntry.Count, "Self should have count of 3");
            Assert.AreEqual(Target.Owner, ownerEntry.Target, "Owner target should be found");
            Assert.AreEqual(2, ownerEntry.Count, "Owner should have count of 2");
            Assert.AreEqual(Target.Source, sourceEntry.Target, "Source target should be found");
            Assert.AreEqual(1, sourceEntry.Count, "Source should have count of 1");

            array.Dispose();
        }

        [Test]
        public void EqualityCheck_Equal_ReturnsCorrectResult()
        {
            var comparisonEntity = this.CreateComparisonValuesEntity(42);
            var values = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);
            var subscriber = new EventSubscriber
            {
                Subscriber = comparisonEntity,
                Operation = Equality.Equal,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 42), "Equal values should return true");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 41), "Unequal values should return false");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 43), "Unequal values should return false");
        }

        [Test]
        public void EqualityCheck_NotEqual_ReturnsCorrectResult()
        {
            var comparisonEntity = this.CreateComparisonValuesEntity(10);
            var values = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);
            var subscriber = new EventSubscriber
            {
                Subscriber = comparisonEntity,
                Operation = Equality.NotEqual,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 10), "Equal values should return false for NotEqual");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 9), "Unequal values should return true for NotEqual");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 11), "Unequal values should return true for NotEqual");
        }

        [Test]
        public void EqualityCheck_GreaterThan_ReturnsCorrectResult()
        {
            var comparisonEntity = this.CreateComparisonValuesEntity(5);
            var values = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);
            var subscriber = new EventSubscriber
            {
                Subscriber = comparisonEntity,
                Operation = Equality.GreaterThan,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 6), "Greater value should return true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 100), "Much greater value should return true");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 5), "Equal value should return false for GreaterThan");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 4), "Lesser value should return false");
        }

        [Test]
        public void EqualityCheck_GreaterThanEqual_ReturnsCorrectResult()
        {
            var comparisonEntity = this.CreateComparisonValuesEntity(15);
            var values = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);
            var subscriber = new EventSubscriber
            {
                Subscriber = comparisonEntity,
                Operation = Equality.GreaterThanEqual,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 15), "Equal value should return true for GreaterThanEqual");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 16), "Greater value should return true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 50), "Much greater value should return true");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 14), "Lesser value should return false");
        }

        [Test]
        public void EqualityCheck_LessThan_ReturnsCorrectResult()
        {
            var comparisonEntity = this.CreateComparisonValuesEntity(20);
            var values = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);
            var subscriber = new EventSubscriber
            {
                Subscriber = comparisonEntity,
                Operation = Equality.LessThan,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 19), "Lesser value should return true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 5), "Much lesser value should return true");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 20), "Equal value should return false for LessThan");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 21), "Greater value should return false");
        }

        [Test]
        public void EqualityCheck_LessThanEqual_ReturnsCorrectResult()
        {
            var comparisonEntity = this.CreateComparisonValuesEntity(8);
            var values = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);
            var subscriber = new EventSubscriber
            {
                Subscriber = comparisonEntity,
                Operation = Equality.LessThanEqual,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 8), "Equal value should return true for LessThanEqual");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 7), "Lesser value should return true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 1), "Much lesser value should return true");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 9), "Greater value should return false");
        }

        [Test]
        public void EqualityCheck_Between_ReturnsCorrectResult()
        {
            var comparisonEntity = this.CreateComparisonValuesEntity(10, 20);
            var values = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);
            var valueIndex = default(ValueIndex);
            valueIndex.Min = 0;
            valueIndex.Max = 1;
            var subscriber = new EventSubscriber
            {
                Subscriber = comparisonEntity,
                Operation = Equality.Between,
                ValueIndex = valueIndex,
            };

            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 10), "Min value should return true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 15), "Middle value should return true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 20), "Max value should return true");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 9), "Below min should return false");
            Assert.IsFalse(ReactionUtil.EqualityCheck(subscriber, values, 21), "Above max should return false");
        }

        [Test]
        public void EqualityCheck_Any_AlwaysReturnsTrue()
        {
            var comparisonEntity = this.CreateComparisonValuesEntity(999);
            var values = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);
            var subscriber = new EventSubscriber
            {
                Subscriber = comparisonEntity,
                Operation = Equality.Any,
            };

            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, int.MinValue), "Any operation should always return true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 0), "Any operation should always return true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, int.MaxValue), "Any operation should always return true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, -500), "Any operation should always return true");
        }

        [Test]
        public void EqualityCheck_InvalidOperation_ReturnsTrue()
        {
            var comparisonEntity = this.CreateComparisonValuesEntity(42);
            var values = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);
            var subscriber = new EventSubscriber
            {
                Subscriber = comparisonEntity,
                Operation = (Equality)255,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 42), "Invalid operation should default to true");
            Assert.IsTrue(ReactionUtil.EqualityCheck(subscriber, values, 100), "Invalid operation should default to true");
        }

        [Test]
        public void WriteState_WithoutValue_OnlyUpdatesCondition()
        {
            var subscriberEntity = this.CreateComparisonValuesEntity(100);
            this.Manager.AddComponent<ConditionActive>(subscriberEntity);
            this.Manager.SetComponentData(subscriberEntity, new ConditionActive { Value = new BitArray32(0) });

            var conditionActives = this.Manager.GetComponentLookup<ConditionActive>();
            var conditionValues = this.Manager.GetBufferLookup<ConditionValues>();
            var comparisonValues = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);

            var subscriber = new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Index = 5,
                Operation = Equality.Equal,
                Feature = ConditionFeature.Condition,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            ReactionUtil.WriteState(subscriber, 100, comparisonValues, conditionActives, conditionValues);

            var activeData = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(activeData.Value[5], "Condition 5 should be set to true (equality match)");

            for (int i = 0; i < 32; i++)
            {
                if (i != 5)
                {
                    Assert.IsFalse(activeData.Value[i], $"Condition {i} should remain false");
                }
            }
        }

        [Test]
        public void WriteState_WithValue_UpdatesConditionAndValue()
        {
            var subscriberEntity = this.CreateComparisonValuesEntity(50);
            this.Manager.AddComponent<ConditionActive>(subscriberEntity);
            this.Manager.SetComponentData(subscriberEntity, new ConditionActive { Value = new BitArray32(0) });
            this.Manager.AddBuffer<ConditionValues>(subscriberEntity);
            var valuesBuffer = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            valuesBuffer.ResizeUninitialized(ConditionActive.MaxConditions);

            var conditionActives = this.Manager.GetComponentLookup<ConditionActive>();
            var conditionValues = this.Manager.GetBufferLookup<ConditionValues>();
            var comparisonValues = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);

            var subscriber = new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Index = 3,
                Operation = Equality.GreaterThan,
                Feature = ConditionFeature.Condition | ConditionFeature.Value,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            ReactionUtil.WriteState(subscriber, 75, comparisonValues, conditionActives, conditionValues);

            var activeData = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(activeData.Value[3], "Condition 3 should be set to true (75 > 50)");

            var values = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(75, values[3].Value, "Value should be stored at index 3");
        }

        [Test]
        public void WriteState_NoConditionFeature_DoesNotUpdateCondition()
        {
            var subscriberEntity = this.CreateComparisonValuesEntity(25);
            this.Manager.AddComponent<ConditionActive>(subscriberEntity);
            this.Manager.SetComponentData(subscriberEntity, new ConditionActive { Value = new BitArray32(0) });
            this.Manager.AddBuffer<ConditionValues>(subscriberEntity);
            var valuesBuffer = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            valuesBuffer.ResizeUninitialized(ConditionActive.MaxConditions);

            var conditionActives = this.Manager.GetComponentLookup<ConditionActive>();
            var conditionValues = this.Manager.GetBufferLookup<ConditionValues>();
            var comparisonValues = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);

            var subscriber = new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Index = 7,
                Operation = Equality.Equal,
                Feature = ConditionFeature.Value,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            ReactionUtil.WriteState(subscriber, 25, comparisonValues, conditionActives, conditionValues);

            var activeData = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsFalse(activeData.Value[7], "Condition 7 should remain false (no condition feature)");

            var values = this.Manager.GetBuffer<ConditionValues>(subscriberEntity);
            Assert.AreEqual(25, values[7].Value, "Value should still be stored at index 7");
        }

        [Test]
        public void WriteState_SameConditionValue_DoesNotUpdate()
        {
            var subscriberEntity = this.CreateComparisonValuesEntity(33);
            this.Manager.AddComponent<ConditionActive>(subscriberEntity);
            var initialConditions = new BitArray32(0) { [10] = true };
            this.Manager.SetComponentData(subscriberEntity, new ConditionActive { Value = initialConditions });

            var conditionActives = this.Manager.GetComponentLookup<ConditionActive>();
            var conditionValues = this.Manager.GetBufferLookup<ConditionValues>();
            var comparisonValues = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);

            var subscriber = new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Index = 10,
                Operation = Equality.Equal,
                Feature = ConditionFeature.Condition,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            ReactionUtil.WriteState(subscriber, 33, comparisonValues, conditionActives, conditionValues);

            var activeData = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsTrue(activeData.Value[10], "Condition 10 should remain true");
        }

        [Test]
        public void WriteState_DifferentConditionValue_UpdatesCorrectly()
        {
            var subscriberEntity = this.CreateComparisonValuesEntity(77);
            this.Manager.AddComponent<ConditionActive>(subscriberEntity);
            var initialConditions = new BitArray32(0) { [2] = true };
            this.Manager.SetComponentData(subscriberEntity, new ConditionActive { Value = initialConditions });

            var conditionActives = this.Manager.GetComponentLookup<ConditionActive>();
            var conditionValues = this.Manager.GetBufferLookup<ConditionValues>();
            var comparisonValues = this.Manager.GetBufferLookup<ConditionComparisonValue>(true);

            var subscriber = new EventSubscriber
            {
                Subscriber = subscriberEntity,
                Index = 2,
                Operation = Equality.Equal,
                Feature = ConditionFeature.Condition,
                ValueIndex = new ValueIndex { Value = 0 },
            };

            ReactionUtil.WriteState(subscriber, 88, comparisonValues, conditionActives, conditionValues);

            var activeData = this.Manager.GetComponentData<ConditionActive>(subscriberEntity);
            Assert.IsFalse(activeData.Value[2], "Condition 2 should be set to false (88 != 77)");
        }

        private Entity CreateComparisonValuesEntity(params int[] values)
        {
            var entity = this.Manager.CreateEntity();
            var buffer = this.Manager.AddBuffer<ConditionComparisonValue>(entity);

            foreach (var value in values)
            {
                buffer.Add(new ConditionComparisonValue { Value = value });
            }

            return entity;
        }

        /// <summary>
        /// Test struct that implements IActionWithTarget for testing purposes.
        /// </summary>
        private struct TestAction : IBufferElementData, IActionWithTarget
        {
            public TestAction(Target target)
            {
                this.Target = target;
            }

            public Target Target { get; }
        }
    }
}
