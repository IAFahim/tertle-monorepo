// <copyright file="ConditionCompositeBuilderTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Conditions
{
    using System;
    using BovineLabs.Reaction.Data.Conditions;
    using NUnit.Framework;

    public class ConditionCompositeBuilderTests : ReactionTestFixture
    {
        [Test]
        public void BasicAndGroup_SingleCondition_CreatesCorrectBlobAsset()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                .Add(5)
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(LogicOperation.And, blobAsset.Value.GroupCombination);
            Assert.AreEqual(1, blobAsset.Value.Groups.Length);
            Assert.AreEqual(LogicOperation.And, blobAsset.Value.Groups[0].Logic);
            Assert.AreEqual(-1, blobAsset.Value.Groups[0].NestedLogicIndex);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[5]);
            Assert.AreEqual(0, blobAsset.Value.NestedLogics.Length);

            blobAsset.Dispose();
        }

        [Test]
        public void BasicOrGroup_MultipleConditions_CreatesCorrectBlobAsset()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.Or)
                .Add(1)
                .Add(3)
                .Add(7)
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(LogicOperation.Or, blobAsset.Value.GroupCombination);
            Assert.AreEqual(1, blobAsset.Value.Groups.Length);
            Assert.AreEqual(LogicOperation.Or, blobAsset.Value.Groups[0].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[1]);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[3]);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[7]);

            blobAsset.Dispose();
        }

        [Test]
        public void AddNot_SingleCondition_CreatesNotGroup()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                .AddNot(2)
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(1, blobAsset.Value.Groups.Length);
            Assert.AreEqual(LogicOperation.Not, blobAsset.Value.Groups[0].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[2]);

            blobAsset.Dispose();
        }

        [Test]
        public void AddXor_SingleCondition_CreatesXorGroup()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                .AddXor(5)
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(1, blobAsset.Value.Groups.Length);
            Assert.AreEqual(LogicOperation.Xor, blobAsset.Value.Groups[0].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[5]);

            blobAsset.Dispose();
        }

        [Test]
        public void AddXor_MultipleConditions_CreatesXorGroup()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                .AddXor(1)
                .AddXor(3)
                .AddXor(7)
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(1, blobAsset.Value.Groups.Length);
            Assert.AreEqual(LogicOperation.Xor, blobAsset.Value.Groups[0].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[1]);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[3]);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[7]);

            blobAsset.Dispose();
        }

        [Test]
        public void MixedLogicOperations_SwitchingBetweenAndAndNot_CreatesSeparateGroups()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act - Add normal conditions, then NOT conditions, then normal again
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                .Add(0) // A (AND group)
                .Add(1) // B (Same AND group)
                .AddNot(2) // !C (NOT group, new)
                .AddNot(3) // !D (Same NOT group)
                .Add(4) // E (AND group, new)
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(3, blobAsset.Value.Groups.Length);

            // First group: AND conditions 0,1 (A,B)
            Assert.AreEqual(LogicOperation.And, blobAsset.Value.Groups[0].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[0]);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[1]);

            // Second group: NOT conditions 2,3 (!C,!D)
            Assert.AreEqual(LogicOperation.Not, blobAsset.Value.Groups[1].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[1].Mask[2]);
            Assert.IsTrue(blobAsset.Value.Groups[1].Mask[3]);

            // Third group: AND condition 4 (E)
            Assert.AreEqual(LogicOperation.And, blobAsset.Value.Groups[2].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[2].Mask[4]);

            blobAsset.Dispose();
        }

        [Test]
        public void MixedLogicOperations_IncludingXor_CreatesSeparateGroups()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act - Add AND, XOR, NOT, then XOR again
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                .Add(0) // A (AND group)
                .Add(1) // B (Same AND group)
                .AddXor(2) // C (XOR group, new)
                .AddXor(3) // D (Same XOR group)
                .AddNot(4) // !E (NOT group, new)
                .AddXor(5) // F (XOR group, new)
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(4, blobAsset.Value.Groups.Length);

            // First group: AND conditions 0,1 (A,B)
            Assert.AreEqual(LogicOperation.And, blobAsset.Value.Groups[0].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[0]);
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[1]);

            // Second group: XOR conditions 2,3 (C^D)
            Assert.AreEqual(LogicOperation.Xor, blobAsset.Value.Groups[1].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[1].Mask[2]);
            Assert.IsTrue(blobAsset.Value.Groups[1].Mask[3]);

            // Third group: NOT condition 4 (!E)
            Assert.AreEqual(LogicOperation.Not, blobAsset.Value.Groups[2].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[2].Mask[4]);

            // Fourth group: XOR condition 5 (F)
            Assert.AreEqual(LogicOperation.Xor, blobAsset.Value.Groups[3].Logic);
            Assert.IsTrue(blobAsset.Value.Groups[3].Mask[5]);

            blobAsset.Dispose();
        }

        [Test]
        public void NestedGroups_SimpleNesting_CreatesCorrectStructure()
        {
            // Arrange - (A|B) & (C&D)
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                    .BeginGroup(LogicOperation.Or)
                        .Add(0) // A
                        .Add(1) // B
                    .EndGroup()
                    .BeginGroup(LogicOperation.And)
                        .Add(2) // C
                        .Add(3) // D
                    .EndGroup()
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            ref var root = ref blobAsset.Value;
            Assert.AreEqual(LogicOperation.And, root.GroupCombination);
            Assert.AreEqual(2, root.Groups.Length);
            Assert.AreEqual(2, root.NestedLogics.Length);

            // Both groups should reference nested logic
            Assert.AreEqual(0, root.Groups[0].NestedLogicIndex);
            Assert.AreEqual(1, root.Groups[1].NestedLogicIndex);

            // First nested logic: OR group (A|B)
            ref var firstNested = ref root.NestedLogics[0];
            Assert.AreEqual(LogicOperation.Or, firstNested.GroupCombination);
            Assert.AreEqual(1, firstNested.Groups.Length);
            Assert.AreEqual(LogicOperation.Or, firstNested.Groups[0].Logic);
            Assert.IsTrue(firstNested.Groups[0].Mask[0]);
            Assert.IsTrue(firstNested.Groups[0].Mask[1]);

            // Second nested logic: AND group (C&D)
            ref var secondNested = ref root.NestedLogics[1];
            Assert.AreEqual(LogicOperation.And, secondNested.GroupCombination);
            Assert.AreEqual(1, secondNested.Groups.Length);
            Assert.AreEqual(LogicOperation.And, secondNested.Groups[0].Logic);
            Assert.IsTrue(secondNested.Groups[0].Mask[2]);
            Assert.IsTrue(secondNested.Groups[0].Mask[3]);

            blobAsset.Dispose();
        }

        [Test]
        public void ComplexNesting_ThreeLevels_CreatesCorrectStructure()
        {
            // Arrange - (A|B) & (C&(D|!E))
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                    .BeginGroup(LogicOperation.Or)
                        .Add(0) // A
                        .Add(1) // B
                    .EndGroup()
                    .BeginGroup(LogicOperation.And)
                        .Add(2) // C
                        .BeginGroup(LogicOperation.Or)
                            .Add(3) // D
                            .AddNot(4) // !E
                        .EndGroup()
                    .EndGroup()
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            ref var root = ref blobAsset.Value;
            Assert.AreEqual(LogicOperation.And, root.GroupCombination);
            Assert.AreEqual(2, root.Groups.Length);
            Assert.AreEqual(2, root.NestedLogics.Length);

            // Second nested logic should have its own nested logic
            ref var secondNested = ref root.NestedLogics[1];
            Assert.AreEqual(2, secondNested.Groups.Length); // C and nested (D|!E)
            Assert.AreEqual(1, secondNested.NestedLogics.Length);

            // Third level: (D|!E)
            ref var thirdNested = ref secondNested.NestedLogics[0];
            Assert.AreEqual(LogicOperation.Or, thirdNested.GroupCombination);
            Assert.AreEqual(2, thirdNested.Groups.Length); // D and !E as separate groups

            blobAsset.Dispose();
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
        [Test]
        public void EndGroup_NoActiveGroup_ThrowsException()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.EndGroup());
        }

        [Test]
        public void Add_NoActiveGroup_ThrowsException()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.Add(1));
        }

        [Test]
        public void AddNot_NoActiveGroup_ThrowsException()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.AddNot(1));
        }

        [Test]
        public void Add_ConditionOutOfRange_ThrowsException()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();
            builder.BeginGroup(LogicOperation.And);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Add(32));
        }

        [Test]
        public void AddNot_ConditionOutOfRange_ThrowsException()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();
            builder.BeginGroup(LogicOperation.And);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddNot(32));
        }

        [Test]
        public void CreateBlobAsset_NoGroups_ThrowsException()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.CreateBlobAsset());
        }

        [Test]
        public void CreateBlobAsset_UnclosedGroups_ThrowsException()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();
            builder.BeginGroup(LogicOperation.And);
            builder.BeginGroup(LogicOperation.Or);
            builder.Add(1);
            builder.EndGroup(); // Only close one group

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.CreateBlobAsset());
        }
#endif

        [Test]
        public void FluentInterface_ChainedCalls_WorksCorrectly()
        {
            // Arrange & Act
            using var builder = CreateCompositeBuilder();
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                .Add(1).Add(2).AddNot(3)
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(2, blobAsset.Value.Groups.Length);
            blobAsset.Dispose();
        }

        [Test]
        public void EmptyGroup_NoConditions_CreatesEmptyGroup()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(0, blobAsset.Value.Groups.Length);
            Assert.AreEqual(0, blobAsset.Value.NestedLogics.Length);

            blobAsset.Dispose();
        }

        [Test]
        public void MaxConditions_Condition31_WorksCorrectly()
        {
            // Arrange
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.And)
                .Add(31) // Maximum valid condition index
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.IsTrue(blobAsset.Value.Groups[0].Mask[31]);

            blobAsset.Dispose();
        }

        [Test]
        public void GroupLevelNot_SingleGroup_CreatesNotCombination()
        {
            // Arrange: Test !(A & B) - group-level NOT operation
            using var builder = CreateCompositeBuilder();

            // Act
            var blobAsset = builder
                .BeginGroup(LogicOperation.Not) // This creates a NOT group combination
                .BeginGroup(LogicOperation.And) // Inner group: A & B
                .Add(0) // A
                .Add(1) // B
                .EndGroup()
                .EndGroup()
                .CreateBlobAsset();

            // Assert
            Assert.AreEqual(LogicOperation.Not, blobAsset.Value.GroupCombination);
            Assert.AreEqual(1, blobAsset.Value.Groups.Length);
            Assert.AreEqual(0, blobAsset.Value.Groups[0].NestedLogicIndex); // References nested logic
            Assert.AreEqual(1, blobAsset.Value.NestedLogics.Length);

            // Check the nested AND group
            ref var nestedLogic = ref blobAsset.Value.NestedLogics[0];
            Assert.AreEqual(LogicOperation.And, nestedLogic.GroupCombination);
            Assert.AreEqual(1, nestedLogic.Groups.Length);
            Assert.AreEqual(LogicOperation.And, nestedLogic.Groups[0].Logic);
            Assert.IsTrue(nestedLogic.Groups[0].Mask[0]); // A
            Assert.IsTrue(nestedLogic.Groups[0].Mask[1]); // B

            blobAsset.Dispose();
        }
    }
}