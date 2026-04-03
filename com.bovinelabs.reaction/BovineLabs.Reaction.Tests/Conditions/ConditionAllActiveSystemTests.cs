// <copyright file="ConditionAllActiveSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Conditions
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Builders;
    using BovineLabs.Reaction.Data.Conditions;
    using NUnit.Framework;
    using Unity.Entities;

    public class ConditionAllActiveSystemTests : ReactionTestFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<ConditionAllActiveSystem>();
        }

        [Test]
        public void SimpleAndLogic_AllConditionsTrue_EnablesConditionAllActive()
        {
            // Arrange
            var entity = this.CreateSimpleConditionEntity(new BitArray32(0b11111111)); // All 8 conditions true

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void SimpleAndLogic_SomeConditionsFalse_DisablesFromTrue()
        {
            // Arrange - Start with enabled state to test the system actually evaluates
            var entity = this.CreateSimpleConditionEntity(new BitArray32(0b11110111)); // One condition false
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);

            // Act
            this.RunConditionAllActiveSystem();

            // Assert - System should disable it because not all conditions are met
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void SimpleAndLogic_NoConditions_RemainsFalse()
        {
            // Arrange - Start with enabled state to test the system actually evaluates
            var entity = this.CreateSimpleConditionEntity(BitArray32.None);
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);

            // Act
            this.RunConditionAllActiveSystem();

            // Assert - System should disable it due to no conditions being met
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void SimpleAndLogic_PartialConditions_DisablesFromTrue()
        {
            // Arrange - Start with enabled state to test the system actually evaluates
            var entity = this.CreateSimpleConditionEntity(new BitArray32(0b00000001)); // Only first condition true
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);

            // Act
            this.RunConditionAllActiveSystem();

            // Assert - System should disable it because not all conditions are met
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void ChanceLogic_100Percent_AlwaysEvaluatesTrue()
        {
            // Arrange: 100% chance (10000 out of 10000)
            var entity = this.CreateSimpleConditionChanceEntity(new BitArray32(0b11111111), 10000);

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void ChanceLogic_0Percent_AlwaysEvaluatesFalse()
        {
            // Arrange: 0% chance
            var entity = this.CreateSimpleConditionChanceEntity(new BitArray32(0b11111111), 0);

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void ChanceLogic_EdgeCases_EvaluateCorrectly()
        {
            // Test edge cases for chance values
            var testCases = new[]
            {
                (ushort)0, // 0% - should always be false
                (ushort)1, // 0.01% - very low chance
                (ushort)9999, // 99.99% - very high chance
                (ushort)10000, // 100% - should always be true
            };

            foreach (var chanceValue in testCases)
            {
                // Arrange
                var entity = this.CreateSimpleConditionChanceEntity(new BitArray32(0b11111111), chanceValue);

                // Act
                this.system.Update(this.WorldUnmanaged);
                this.Manager.CompleteAllTrackedJobs();

                // Assert based on known chance values
                if (chanceValue == 0)
                {
                    Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(entity), "0% chance should always be false");
                }
                else if (chanceValue == 10000)
                {
                    Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity), "100% chance should always be true");
                }

                // For other values, we can't predict the exact result due to random generation
                // Cleanup
                this.Manager.DestroyEntity(entity);
            }
        }

        [Test]
        public void EdgeCase_MaxConditions_EvaluatesCorrectly()
        {
            // Arrange: All 32 conditions true
            var conditions = new BitArray32(0xFFFFFFFF);
            var entity = this.CreateConditionEntity(conditions);

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void ChangeFilter_NoChange_SystemDoesNotRun()
        {
            // Arrange
            var entity = this.CreateSimpleConditionEntity(new BitArray32(0b11111111));

            // First update
            this.RunConditionAllActiveSystem();
            var initialResult = this.Manager.IsComponentEnabled<ConditionAllActive>(entity);

            // Act - Second update without changing ConditionActive
            this.RunConditionAllActiveSystem();

            // Assert - Result should remain the same
            var secondResult = this.Manager.IsComponentEnabled<ConditionAllActive>(entity);
            Assert.AreEqual(initialResult, secondResult);
        }

        [Test]
        public void CompositeLogic_SingleAndGroup_EvaluatesCorrectly()
        {
            // Arrange: (condition0 AND condition1)
            var conditions = new BitArray32(0b00000011); // conditions 0 and 1 true
            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup(); // Both conditions required
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void CompositeLogic_SingleOrGroup_EvaluatesCorrectly()
        {
            // Arrange: (condition0 OR condition1) where only condition0 is true
            var conditions = new BitArray32(0b00000001); // Only condition 0 true
            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.Or).Add(0).Add(1).EndGroup(); // Either condition sufficient
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void CompositeLogic_NotGroup_EvaluatesCorrectly()
        {
            // Arrange: (NOT condition0) where condition0 is false
            var conditions = new BitArray32(0b00000000); // condition 0 false
            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.And).AddNot(0).EndGroup(); // condition 0 must be false
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void CompositeLogic_MultipleGroupsAndCombination_EvaluatesCorrectly()
        {
            // Arrange: (condition0 AND condition1) AND (condition2 OR condition3)
            var conditions = new BitArray32(0b00001011); // conditions 0, 1, 3 true; 2 false
            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.And) // Root combination: AND
                    .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // conditions 0 AND 1
                    .BeginGroup(LogicOperation.Or).Add(2).Add(3).EndGroup() // conditions 2 OR 3
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void CompositeLogic_MultipleGroupsOrCombination_EvaluatesCorrectly()
        {
            // Arrange: (condition0 AND condition1) OR (condition2 AND condition3)
            var conditions = new BitArray32(0b00000011); // conditions 0, 1 true; 2, 3 false
            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.Or) // Root combination: OR
                    .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // conditions 0 AND 1 (true)
                    .BeginGroup(LogicOperation.And).Add(2).Add(3).EndGroup() // conditions 2 AND 3 (false)
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void NestedLogic_SimpleNesting_EvaluatesCorrectly()
        {
            // Arrange: ((condition0 AND condition1) OR condition2)
            // Inner: (condition0 AND condition1) = true (both set)
            // Outer: inner OR condition2 = true (inner is true)
            var conditions = new BitArray32(0b00000011); // conditions 0, 1 true; 2 false

            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.Or) // Root OR combination
                    .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // (condition0 AND condition1)
                    .Add(2) // OR condition2
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void NestedLogic_ComplexNesting_EvaluatesCorrectly()
        {
            // Arrange: ((A & B) | C) & (D | (E & F))
            // Conditions: A=true, B=true, C=false, D=false, E=true, F=true
            // Left side: ((true & true) | false) = (true | false) = true
            // Right side: (false | (true & true)) = (false | true) = true
            // Final: true & true = true
            var conditions = new BitArray32(0b00110011); // A=0, B=1, C=2, D=3, E=4, F=5 -> bits 0,1,4,5 set

            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.And) // Root AND: ((A & B) | C) & (D | (E & F))
                    .BeginGroup(LogicOperation.Or) // Left side: ((A & B) | C)
                        .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // (A & B)
                        .Add(2) // | C
                        .EndGroup()
                    .BeginGroup(LogicOperation.Or) // Right side: (D | (E & F))
                        .Add(3) // D
                        .BeginGroup(LogicOperation.And).Add(4).Add(5).EndGroup() // | (E & F)
                        .EndGroup()
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void NestedLogic_NotOfNestedExpression_EvaluatesCorrectly()
        {
            // Arrange: NOT((condition0 AND condition1))
            // Inner: (condition0 AND condition1) = false (only condition0 is true)
            // Outer: NOT(false) = true
            var conditions = new BitArray32(0b00000001); // Only condition 0 true, condition 1 false

            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.Not) // NOT
                    .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // (condition0 AND condition1)
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void NestedLogic_DeeplyNested_EvaluatesCorrectly()
        {
            // Arrange: (((A & B) | C) & D) | (E & (F | G))
            // Conditions: A=true, B=false, C=true, D=true, E=false, F=true, G=false
            // Inner most: (A & B) = (true & false) = false
            // Next level: ((A & B) | C) = (false | true) = true
            // Next level: (((A & B) | C) & D) = (true & true) = true
            // Right side: (F | G) = (true | false) = true
            // Right side: (E & (F | G)) = (false & true) = false
            // Final: true | false = true
            var conditions = new BitArray32(0b00101101); // A=0, B=1, C=2, D=3, E=4, F=5, G=6 -> bits 0,2,3,5 set

            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.Or) // Root OR: (((A & B) | C) & D) | (E & (F | G))
                    .BeginGroup(LogicOperation.And) // Left side: (((A & B) | C) & D)
                        .BeginGroup(LogicOperation.Or) // ((A & B) | C)
                            .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // (A & B)
                            .Add(2) // | C
                            .EndGroup()
                        .Add(3) // & D
                        .EndGroup()
                    .BeginGroup(LogicOperation.And) // Right side: (E & (F | G))
                        .Add(4) // E
                        .BeginGroup(LogicOperation.Or).Add(5).Add(6).EndGroup() // & (F | G)
                        .EndGroup()
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void NestedLogic_MultipleNOTOperations_EvaluatesCorrectly()
        {
            // Arrange: NOT(NOT(A & B) | C) & (D | NOT(E))
            // Conditions: A=true, B=true, C=false, D=false, E=true
            // Inner: (A & B) = (true & true) = true
            // NOT(A & B) = NOT(true) = false
            // NOT(A & B) | C = false | false = false
            // NOT(NOT(A & B) | C) = NOT(false) = true
            // Right: NOT(E) = NOT(true) = false
            // Right: (D | NOT(E)) = (false | false) = false
            // Final: true & false = false
            var conditions = new BitArray32(0b00010011); // A=0, B=1, C=2, D=3, E=4 -> bits 0,1,4 set

            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.And) // Root AND: NOT(NOT(A & B) | C) & (D | NOT(E))
                    .BeginGroup(LogicOperation.Not) // Left: NOT(NOT(A & B) | C)
                        .BeginGroup(LogicOperation.Or) // (NOT(A & B) | C)
                            .BeginGroup(LogicOperation.Not) // NOT(A & B)
                                .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // (A & B)
                                .EndGroup()
                            .Add(2) // | C
                            .EndGroup()
                        .EndGroup()
                    .BeginGroup(LogicOperation.Or) // Right: (D | NOT(E))
                        .Add(3) // D
                        .AddNot(4) // | NOT(E)
                        .EndGroup()
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void NestedLogic_ComplexMixedOperations_EvaluatesCorrectly()
        {
            // Arrange: ((A | B) & NOT(C)) | (NOT(D & E) & (F | G | H))
            // Conditions: A=false, B=true, C=false, D=true, E=false, F=false, G=true, H=false
            // Left side: (A | B) = (false | true) = true
            // Left side: NOT(C) = NOT(false) = true
            // Left side: ((A | B) & NOT(C)) = (true & true) = true
            // Right side: (D & E) = (true & false) = false
            // Right side: NOT(D & E) = NOT(false) = true
            // Right side: (F | G | H) = (false | true | false) = true
            // Right side: (NOT(D & E) & (F | G | H)) = (true & true) = true
            // Final: true | true = true
            var conditions = new BitArray32(0b01001010); // A=0, B=1, C=2, D=3, E=4, F=5, G=6, H=7 -> bits 1,3,6 set

            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.Or) // Root OR: ((A | B) & NOT(C)) | (NOT(D & E) & (F | G | H))
                    .BeginGroup(LogicOperation.And) // Left: ((A | B) & NOT(C))
                        .BeginGroup(LogicOperation.Or).Add(0).Add(1).EndGroup() // (A | B)
                        .AddNot(2) // & NOT(C)
                        .EndGroup()
                    .BeginGroup(LogicOperation.And) // Right: (NOT(D & E) & (F | G | H))
                        .BeginGroup(LogicOperation.Not) // NOT(D & E)
                            .BeginGroup(LogicOperation.And).Add(3).Add(4).EndGroup() // (D & E)
                            .EndGroup()
                        .BeginGroup(LogicOperation.Or).Add(5).Add(6).Add(7).EndGroup() // & (F | G | H)
                        .EndGroup()
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void NestedLogic_EmptyNestedLogic_EvaluatesCorrectly()
        {
            // Arrange: (A & B) | (empty nested logic)
            // Empty nested logic should evaluate to false
            // Conditions: A=false, B=false
            // (A & B) = false, empty = false
            // Final: false | false = false
            var conditions = new BitArray32(0b00000000); // All false

            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.Or) // Root OR: (A & B) | (empty group)
                    .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // (A & B)
                    .BeginGroup(LogicOperation.And).EndGroup() // Empty group (should evaluate to false)
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void NestedLogic_SelfReferentialStyle_EvaluatesCorrectly()
        {
            // Arrange: ((A & B) | C) & ((A & B) | D)
            // This tests reusing the same nested logic pattern
            // Conditions: A=true, B=true, C=false, D=false
            // Left: ((A & B) | C) = (true | false) = true
            // Right: ((A & B) | D) = (true | false) = true
            // Final: true & true = true
            var conditions = new BitArray32(0b00000011); // A=0, B=1 -> bits 0,1 set

            var entity = this.CreateCompositeConditionEntity(conditions, (ref ConditionCompositeBuilder builder) =>
            {
                builder = builder.BeginGroup(LogicOperation.And) // Root AND: ((A & B) | C) & ((A & B) | D
                    .BeginGroup(LogicOperation.Or) // Left: ((A & B) | C)
                        .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // (A & B)
                        .Add(2) // | C
                        .EndGroup()
                    .BeginGroup(LogicOperation.Or) // Right: ((A & B) | D)
                        .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // (A & B) repeated
                        .Add(3) // | D
                        .EndGroup()
                    .EndGroup();
            });

            // Act
            this.RunConditionAllActiveSystem();

            // Assert
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity));
        }

        [Test]
        public void GroupLevelNot_NotAndGroup_EvaluatesCorrectly()
        {
            // Test !(A & B) using group-level NOT operation
            // When A=true, B=true: (A & B) = true, so !(A & B) = false
            // When A=true, B=false: (A & B) = false, so !(A & B) = true
            // When A=false, B=true: (A & B) = false, so !(A & B) = true
            // When A=false, B=false: (A & B) = false, so !(A & B) = true
            var entity = this.Manager.CreateEntity();
            this.Manager.AddComponent<ConditionActive>(entity);
            this.Manager.AddComponent<ConditionAllActive>(entity);
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);

            // Build !(A & B) using group-level NOT
            using var builder = CreateCompositeBuilder()
                .BeginGroup(LogicOperation.Not) // NOT group combination
                .BeginGroup(LogicOperation.And).Add(0).Add(1).EndGroup() // (A & B)
                .EndGroup();
            var logic = builder.CreateBlobAsset();

            this.Manager.AddComponentData(entity, new ConditionComposite { Logic = logic });

            // Test case 1: A=true, B=true -> (A & B) = true -> !(A & B) = false
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0b11111111) }); // All conditions true
            this.RunConditionAllActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<ConditionAllActive>(entity), "!(true & true) should be false");

            // Test case 2: A=true, B=false -> (A & B) = false -> !(A & B) = true
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0b11111101) }); // Condition 1 false, others true
            this.RunConditionAllActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity), "!(true & false) should be true");

            // Test case 3: A=false, B=true -> (A & B) = false -> !(A & B) = true
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0b11111110) }); // Condition 0 false, others true
            this.RunConditionAllActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity), "!(false & true) should be true");

            // Test case 4: A=false, B=false -> (A & B) = false -> !(A & B) = true
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0b11111100) }); // Conditions 0,1 false, others true
            this.RunConditionAllActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<ConditionAllActive>(entity), "!(false & false) should be true");

            logic.Dispose();
        }

        private void RunConditionAllActiveSystem()
        {
            this.RunSystems(this.system);
        }
    }
}
