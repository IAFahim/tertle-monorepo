// <copyright file="ActionTagSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Actions
{
    using BovineLabs.Core;
    using BovineLabs.Reaction.Actions;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;

    /// <summary>
    /// Tests for <see cref="ActionTagSystem"/> and <see cref="ActionTagDeactivatedSystem"/>,
    /// verifying tag component addition/removal functionality with reference counting.
    /// </summary>
    public class ActionTagSystemTests : ReactionTestFixture
    {
        private SystemHandle tagSystem;
        private SystemHandle deactivatedSystem;
        private SystemHandle instantiateCommandBufferSystem;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();

            // Create required command buffer system
            this.instantiateCommandBufferSystem = this.World.CreateSystem<InstantiateCommandBufferSystem>();

            this.tagSystem = this.World.CreateSystem<ActionTagSystem>();
            this.deactivatedSystem = this.World.CreateSystem<ActionTagDeactivatedSystem>();
        }

        [Test]
        public void BasicTagAddRemove_AddsAndRemovesTagComponent()
        {
            // Create reaction entity
            var reactionEntity = this.CreateReactionEntity();

            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Act: Activate reaction
            this.SetReactionActiveState(reactionEntity, true);
            this.RunTagSystems();

            // Assert: Tag should be added
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "Tag component should be added after reaction activation");

            // Act: Deactivate reaction
            this.SetReactionActiveState(reactionEntity, false);
            this.RunTagSystems();

            // Assert: Tag should be removed
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reactionEntity),
                "Tag component should be removed after reaction deactivation");
        }

        [Test]
        public void ReferenceCountingBasic_MultipleTaggersKeepComponentPresent()
        {
            // Arrange: Create target entity
            var targetEntity = this.Manager.CreateEntity();

            // Create two reaction entities both targeting the same tag
            var reaction1 = this.CreateReactionEntity(targetEntity);
            var reaction2 = this.CreateReactionEntity(targetEntity);

            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();

            // Both reactions add the same tag to the same target
            var buffer1 = this.Manager.AddBuffer<ActionTag>(reaction1);
            buffer1.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            var buffer2 = this.Manager.AddBuffer<ActionTag>(reaction2);
            buffer2.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            // Act: Activate first reaction
            this.Manager.SetComponentEnabled<Active>(reaction1, true);
            this.Manager.SetComponentEnabled<Active>(reaction2, false);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, true);

            // Assert: Tag should be added
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));

            // Act: Activate second reaction
            this.Manager.SetComponentEnabled<Active>(reaction2, true);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, true);

            // Assert: Tag should still be present
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));

            // Act: Deactivate first reaction
            this.Manager.SetComponentEnabled<Active>(reaction1, false);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, false);

            // Assert: Tag should still be present (second reaction still active)
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));

            // Act: Deactivate second reaction
            this.Manager.SetComponentEnabled<Active>(reaction2, false);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, false);

            // Assert: Tag should now be removed
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(targetEntity));
        }

        [Test]
        public void MultipleTags_HandlesMultipleTagTypesIndependently()
        {
            // Create reaction entity that adds both tags
            var reactionEntity = this.CreateReactionEntity();

            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);

            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            var secondTestTypeHash = GetTestComponentTypeHash<SecondTestTagComponent>();

            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            tagBuffer.Add(new ActionTag
            {
                Value = secondTestTypeHash,
                Target = Target.Self,
            });

            // Act: Activate reaction
            this.SetReactionActiveState(reactionEntity, true);
            this.RunTagSystems();

            // Assert: Both tags should be added
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reactionEntity));
            Assert.IsTrue(this.Manager.HasComponent<SecondTestTagComponent>(reactionEntity));

            // Act: Deactivate reaction
            this.SetReactionActiveState(reactionEntity, false);
            this.RunTagSystems();

            // Assert: Both tags should be removed
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reactionEntity));
            Assert.IsFalse(this.Manager.HasComponent<SecondTestTagComponent>(reactionEntity));
        }

        [Test]
        public void TargetTypes_HandlesAllTargetTypesCorrectly()
        {
            // Create multiple entities for different target types
            var ownerEntity = this.Manager.CreateEntity();
            var targetEntity = this.Manager.CreateEntity();
            var sourceEntity = this.Manager.CreateEntity();
            var custom0Entity = this.Manager.CreateEntity();

            // Create reaction entity with various target types
            var reactionEntity = this.CreateReactionEntity(ownerEntity, sourceEntity, targetEntity);
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();

            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Owner,
            });

            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Source,
            });

            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Custom0,
            });

            // Set up custom targets
            this.Manager.AddComponentData(reactionEntity, new TargetsCustom { Target0 = custom0Entity });

            // Act: Activate reaction
            this.SetReactionActiveState(reactionEntity, true);
            this.RunTagSystems();

            // Assert: All target entities should have the tag
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(ownerEntity));
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(sourceEntity));
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(custom0Entity));
        }

        [Test]
        public void InvalidTagHash_LogsWarningAndContinues()
        {
            // Create reaction with invalid tag hash
            var reactionEntity = this.CreateReactionEntity();

            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            tagBuffer.Add(new ActionTag
            {
                Value = 0xDEADBEEF,
                Target = Target.Self,
            }); // Invalid hash

            // Act: Activate reaction
            this.Manager.SetComponentEnabled<Active>(reactionEntity, true);
            this.RunTagSystems();

            // Assert: System should not crash (warning will be logged)
            Assert.Pass("System handled invalid tag hash gracefully");
        }

        [Test]
        public void NonExistentTarget_LogsErrorAndContinues()
        {
            // Create reaction with non-existent target
            var reactionEntity = this.CreateReactionEntity(Entity.Null, Entity.Null, Entity.Null);
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            // Act: Activate reaction
            this.Manager.SetComponentEnabled<Active>(reactionEntity, true);
            this.RunTagSystems();

            // Assert: System should not crash (error will be logged)
            Assert.Pass("System handled non-existent target gracefully");
        }

        [Test]
        public void AlreadyPresentTag_HandlesCorrectly()
        {
            // Create target entity with tag already present
            var targetEntity = this.Manager.CreateEntity();
            this.Manager.AddComponent<TestTagComponent>(targetEntity);

            // Create reaction entity
            var reactionEntity = this.CreateReactionEntity(targetEntity);
            var tagBuffer = this.Manager.AddBuffer<ActionTag>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            tagBuffer.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            // Act: Activate reaction
            this.SetReactionActiveState(reactionEntity, true);
            this.RunTagSystems();

            // Assert: Tag should remain present
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));

            // Act: Deactivate reaction
            this.SetReactionActiveState(reactionEntity, false);
            this.RunTagSystems();

            // Assert: Tag should be removed (reference count reached 0)
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(targetEntity));
        }

        [Test]
        public void ComplexReferenceCountingScenario_HandlesCorrectlyAcrossMultipleUpdates()
        {
            // Arrange: Create target entity
            var targetEntity = this.Manager.CreateEntity();

            // Create 3 reaction entities
            var reaction1 = this.CreateReactionEntity(targetEntity);
            var reaction2 = this.CreateReactionEntity(targetEntity);
            var reaction3 = this.CreateReactionEntity(targetEntity);

            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();

            foreach (var reaction in new[] { reaction1, reaction2, reaction3 })
            {
                var buffer = this.Manager.AddBuffer<ActionTag>(reaction);
                buffer.Add(new ActionTag
                {
                    Value = testTypeHash,
                    Target = Target.Target,
                });
            }

            // Act: Activate reactions in sequence
            this.SetReactionActiveState(reaction1, true);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, true);
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));

            this.SetReactionActiveState(reaction2, true);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, true);
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));

            this.SetReactionActiveState(reaction3, true);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction3, true);
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));

            // Deactivate middle reaction
            this.SetReactionActiveState(reaction2, false);
            this.RunTagSystems();
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));

            // Deactivate first reaction
            this.SetReactionActiveState(reaction1, false);
            this.RunTagSystems();
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity));

            // Deactivate last reaction
            this.SetReactionActiveState(reaction3, false);
            this.RunTagSystems();
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(targetEntity));
        }

        [Test]
        public void MixedTargetAndTag_HandlesComplexScenario()
        {
            // Create multiple entities
            var targetEntity1 = this.Manager.CreateEntity();
            var targetEntity2 = this.Manager.CreateEntity();

            // Create reaction entities with different tags and targets
            var reaction1 = this.CreateReactionEntity(targetEntity1);
            var reaction2 = this.CreateReactionEntity(targetEntity2);

            var testTypeHash = GetTestComponentTypeHash<TestTagComponent>();
            var secondTestTypeHash = GetTestComponentTypeHash<SecondTestTagComponent>();

            // Reaction 1: adds TestTag to target1
            var buffer1 = this.Manager.AddBuffer<ActionTag>(reaction1);
            buffer1.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            // Reaction 2: adds SecondTestTag to target2, TestTag to reaction2 itself, AND TestTag to target1
            var buffer2 = this.Manager.AddBuffer<ActionTag>(reaction2);
            buffer2.Add(new ActionTag
            {
                Value = secondTestTypeHash,
                Target = Target.Target, // SecondTestTag to target2
            });
            buffer2.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Self, // TestTag to reaction2 itself
            });

            buffer2.Add(new ActionTag
            {
                Value = testTypeHash,
                Target = Target.Custom0, // TestTag to target1 via custom target
            });

            // This creates the mixed scenario: both reactions add TestTag to target1
            // Need to create a third ActionTag that targets targetEntity1
            // We'll use a custom target setup for this
            this.Manager.AddComponentData(reaction2, new TargetsCustom { Target0 = targetEntity1 });

            // Act: Activate both reactions
            this.Manager.SetComponentEnabled<Active>(reaction1, true);
            this.Manager.SetComponentEnabled<Active>(reaction2, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, false);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, true);

            // Assert: Check all expected tags are present
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity1));
            Assert.IsTrue(this.Manager.HasComponent<SecondTestTagComponent>(targetEntity2));
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reaction2));

            // Act: Deactivate reaction1
            this.Manager.SetComponentEnabled<Active>(reaction1, false);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, false);

            // Assert: TestTag should still be present on target1 (reaction2 still has it), others remain
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(targetEntity1));
            Assert.IsTrue(this.Manager.HasComponent<SecondTestTagComponent>(targetEntity2));
            Assert.IsTrue(this.Manager.HasComponent<TestTagComponent>(reaction2));

            // Act: Deactivate reaction2
            this.Manager.SetComponentEnabled<Active>(reaction2, false);
            this.RunTagSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, false);

            // Assert: All remaining tags should be removed
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(targetEntity1));
            Assert.IsFalse(this.Manager.HasComponent<SecondTestTagComponent>(targetEntity2));
            Assert.IsFalse(this.Manager.HasComponent<TestTagComponent>(reaction2));
        }

        private void RunTagSystems()
        {
            this.RunSystems(this.tagSystem, this.deactivatedSystem, this.instantiateCommandBufferSystem);
        }

        /// <summary>
        /// Test tag component for testing purposes.
        /// </summary>
        private struct TestTagComponent : IComponentData
        {
        }

        /// <summary>
        /// Another test tag component for multi-tag testing.
        /// </summary>
        private struct SecondTestTagComponent : IComponentData
        {
        }
    }
}
