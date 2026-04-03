// <copyright file="ConditionCompositeAuthoringTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Conditions
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Reaction.Authoring.Conditions;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class ConditionCompositeAuthoringTests : ReactionTestFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<ConditionAllActiveSystem>();
        }

        [Test]
        public void ExpressionParser_SimpleAnd_ParsesCorrectly()
        {
            // Arrange: "0 & 1"
            var authoring = new ConditionCompositeAuthoring();
            ReflectionTestHelper.SetPrivateField(authoring, "expression", "0 & 1");

            var entity = this.Manager.CreateEntity();
            var builder = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);;

            // Act
            authoring.Bake(ref builder);

            // Assert
            Assert.IsTrue(this.Manager.HasComponent<ConditionComposite>(entity));

            // Test with conditions: 0=true, 1=true -> should be true
            var conditions = new BitArray32(0b00000011);
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);

            // Test with conditions: 0=true, 1=false -> should be false
            conditions = new BitArray32(0b00000001);
            this.SetConditionsAndTest(entity, conditions, expectedResult: false);
        }

        [Test]
        public void ExpressionParser_SimpleOr_ParsesCorrectly()
        {
            // Arrange: "0 | 1"
            var authoring = new ConditionCompositeAuthoring();
            ReflectionTestHelper.SetPrivateField(authoring, "expression", "0 | 1");

            var entity = this.Manager.CreateEntity();
            var builder = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);

            // Act
            authoring.Bake(ref builder);

            // Assert
            Assert.IsTrue(this.Manager.HasComponent<ConditionComposite>(entity));

            // Test with conditions: 0=true, 1=false -> should be true
            var conditions = new BitArray32(0b00000001);
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);

            // Test with conditions: 0=false, 1=false -> should be false
            conditions = new BitArray32(0b00000000);
            this.SetConditionsAndTest(entity, conditions, expectedResult: false);
        }

        [Test]
        public void ExpressionParser_SimpleNot_ParsesCorrectly()
        {
            // Arrange: "!0"
            var authoring = new ConditionCompositeAuthoring();
            ReflectionTestHelper.SetPrivateField(authoring, "expression", "!0");

            var entity = this.Manager.CreateEntity();
            var builder = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);;

            // Act
            authoring.Bake(ref builder);

            // Assert
            Assert.IsTrue(this.Manager.HasComponent<ConditionComposite>(entity));

            // Test with condition 0=false -> should be true
            var conditions = new BitArray32(0b00000000);
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);

            // Test with condition 0=true -> should be false
            conditions = new BitArray32(0b00000001);
            this.SetConditionsAndTest(entity, conditions, expectedResult: false);
        }

        [Test]
        public void ExpressionParser_SimpleXor_ParsesCorrectly()
        {
            // Arrange: "0 ^ 1"
            var authoring = new ConditionCompositeAuthoring();
            ReflectionTestHelper.SetPrivateField(authoring, "expression", "0 ^ 1");

            var entity = this.Manager.CreateEntity();
            var builder = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);;

            // Act
            authoring.Bake(ref builder);

            // Assert
            Assert.IsTrue(this.Manager.HasComponent<ConditionComposite>(entity));

            // Test with conditions: 0=true, 1=false -> should be true (exactly one is true)
            var conditions = new BitArray32(0b00000001);
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);

            // Test with conditions: 0=false, 1=true -> should be true (exactly one is true)
            conditions = new BitArray32(0b00000010);
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);

            // Test with conditions: 0=true, 1=true -> should be false (both are true)
            conditions = new BitArray32(0b00000011);
            this.SetConditionsAndTest(entity, conditions, expectedResult: false);

            // Test with conditions: 0=false, 1=false -> should be false (none are true)
            conditions = new BitArray32(0b00000000);
            this.SetConditionsAndTest(entity, conditions, expectedResult: false);
        }

        [Test]
        public void ExpressionParser_MultipleXor_ParsesCorrectly()
        {
            // Arrange: "0 ^ 1 ^ 2"
            var authoring = new ConditionCompositeAuthoring();
            ReflectionTestHelper.SetPrivateField(authoring, "expression", "0 ^ 1 ^ 2");

            var entity = this.Manager.CreateEntity();
            var builder = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);;

            // Act
            authoring.Bake(ref builder);

            // Assert
            Assert.IsTrue(this.Manager.HasComponent<ConditionComposite>(entity));

            // Test with conditions: 0=true, 1=false, 2=false -> should be true (odd number true: 1)
            var conditions = new BitArray32(0b00000001);
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);

            // Test with conditions: 0=true, 1=true, 2=false -> should be false (even number true: 2)
            conditions = new BitArray32(0b00000011);
            this.SetConditionsAndTest(entity, conditions, expectedResult: false);

            // Test with conditions: 0=true, 1=true, 2=true -> should be true (odd number true: 3)
            conditions = new BitArray32(0b00000111);
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);

            // Test with conditions: 0=false, 1=false, 2=false -> should be false (even number true: 0)
            conditions = new BitArray32(0b00000000);
            this.SetConditionsAndTest(entity, conditions, expectedResult: false);
        }

        [Test]
        public void ExpressionParser_ComplexExpression_ParsesCorrectly()
        {
            // Arrange: "(0 & 1) | (!2 & 3)"
            // Conditions: 0=true, 1=false, 2=false, 3=true
            // Left: (true & false) = false
            // Right: (!false & true) = (true & true) = true
            // Result: false | true = true
            var authoring = new ConditionCompositeAuthoring();
            ReflectionTestHelper.SetPrivateField(authoring, "expression", "(0 & 1) | (!2 & 3)");

            var entity = this.Manager.CreateEntity();
            var builder = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);;

            // Act
            authoring.Bake(ref builder);

            // Assert
            Assert.IsTrue(this.Manager.HasComponent<ConditionComposite>(entity));

            // Test with conditions: 0=true, 1=false, 2=false, 3=true
            var conditions = new BitArray32(0b00001001); // bits 0 and 3 set
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);
        }

        [Test]
        public void ExpressionParser_ComplexExpressionWithXor_ParsesCorrectly()
        {
            // Arrange: "(0 & 1) ^ 2"
            var authoring = new ConditionCompositeAuthoring();
            ReflectionTestHelper.SetPrivateField(authoring, "expression", "(0 & 1) ^ 2");

            var entity = this.Manager.CreateEntity();
            var builder = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);;

            // Act
            authoring.Bake(ref builder);

            // Assert
            Assert.IsTrue(this.Manager.HasComponent<ConditionComposite>(entity));

            // Test with conditions: 0=true, 1=true, 2=false -> should be true
            // (0 & 1) = true, 2 = false, so true ^ false = true
            var conditions = new BitArray32(0b00000011); // bits 0 and 1 set
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);

            // Test with conditions: 0=true, 1=true, 2=true -> should be false
            // (0 & 1) = true, 2 = true, so true ^ true = false
            conditions = new BitArray32(0b00000111); // bits 0, 1, and 2 set
            this.SetConditionsAndTest(entity, conditions, expectedResult: false);

            // Test with conditions: 0=false, 1=true, 2=true -> should be true
            // (0 & 1) = false, 2 = true, so false ^ true = true
            conditions = new BitArray32(0b00000110); // bits 1 and 2 set
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);

            // Test with conditions: 0=false, 1=false, 2=false -> should be false
            // (0 & 1) = false, 2 = false, so false ^ false = false
            conditions = new BitArray32(0b00000000); // no bits set
            this.SetConditionsAndTest(entity, conditions, expectedResult: false);
        }

        [Test]
        public void ExpressionParser_DeeplyNestedExpression_ParsesCorrectly()
        {
            // Arrange: "(((0 & 1) | 2) & 4) | (5 & (6 | 7))"
            // Same as one of our nested logic tests
            var authoring = new ConditionCompositeAuthoring();
            ReflectionTestHelper.SetPrivateField(authoring, "expression", "(((0 & 1) | 2) & 4) | (5 & (6 | 7))");

            var entity = this.Manager.CreateEntity();
            var builder = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);;

            // Act
            authoring.Bake(ref builder);

            // Assert
            Assert.IsTrue(this.Manager.HasComponent<ConditionComposite>(entity));

            // Test with conditions: 0=true, 1=false, 2=true, 4=true, 5=false, 6=true, 7=false
            // Left: (((true & false) | true) & true) = ((false | true) & true) = (true & true) = true
            // Right: (false & (true | false)) = (false & true) = false
            // Result: true | false = true
            var conditions = new BitArray32(0b01010101); // bits 0, 2, 4, 6 set
            this.SetConditionsAndTest(entity, conditions, expectedResult: true);
        }

        [Test]
        public void ExpressionParser_InvalidExpression_ThrowsException()
        {
            // Arrange
            var authoring = new ConditionCompositeAuthoring();
            ReflectionTestHelper.SetPrivateField(authoring, "expression", "0 & & 1"); // Invalid syntax

            var entity = this.Manager.CreateEntity();
            var builder = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);;

            // Expect the error log message
            LogAssert.Expect(LogType.Error, "Failed to parse expression '0 & & 1': Unexpected character '&' at position 2");

            // Act & Assert - should log error and not add component
            authoring.Bake(ref builder);

            // Verify no component was added due to parse error
            Assert.IsFalse(this.Manager.HasComponent<ConditionComposite>(entity));
        }

        private void SetConditionsAndTest(Entity entity, BitArray32 conditions, bool expectedResult)
        {
            // Set all unused conditions to true (as required by the system)
            var conditionComposite = this.Manager.GetComponentData<ConditionComposite>(entity);
            uint usedConditionsMask = ExtractUsedConditionsMask(ref conditionComposite.Logic.Value);
            var correctedConditions = CorrectUnusedConditions(conditions, usedConditionsMask);

            // Add/update condition components
            if (!this.Manager.HasComponent<ConditionActive>(entity))
            {
                this.Manager.AddComponentData(entity, new ConditionActive { Value = correctedConditions });
                this.Manager.AddComponent<ConditionAllActive>(entity);
                this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            }
            else
            {
                this.Manager.SetComponentData(entity, new ConditionActive { Value = correctedConditions });
            }

            // Run system and check result
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(expectedResult, this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }
    }
}
