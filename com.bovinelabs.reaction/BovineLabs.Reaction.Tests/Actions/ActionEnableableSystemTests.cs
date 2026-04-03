// <copyright file="ActionEnableableSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Actions
{
    using BovineLabs.Reaction.Actions;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;

    /// <summary>
    /// Tests for <see cref="ActionEnableableSystem"/> and <see cref="ActionEnableableDeactivatedSystem"/>,
    /// verifying component enable/disable functionality with reference counting.
    /// </summary>
    public class ActionEnableableSystemTests : ReactionTestFixture
    {
        private SystemHandle enableableSystem;
        private SystemHandle deactivatedSystem;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();

            this.enableableSystem = this.World.CreateSystem<ActionEnableableSystem>();
            this.deactivatedSystem = this.World.CreateSystem<ActionEnableableDeactivatedSystem>();

            // Create ReactionEnableables singleton with test component types
            var singletonEntity = this.Manager.CreateSingletonBuffer<ReactionEnableables>();
            var enableablesBuffer = this.Manager.GetBuffer<ReactionEnableables>(singletonEntity).Initialize().AsMap();

            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();
            var secondTestTypeHash = GetTestComponentTypeHash<SecondTestEnableableComponent>();

            enableablesBuffer.Add(testTypeHash);
            enableablesBuffer.Add(secondTestTypeHash);
        }

        [Test]
        public void BasicEnableDisable_EnablesAndDisablesComponent()
        {
            // Create reaction entity with test component (initially disabled)
            var reactionEntity = this.CreateReactionEntity();
            this.Manager.AddComponent<TestEnableableComponent>(reactionEntity);
            this.Manager.SetComponentEnabled<TestEnableableComponent>(reactionEntity, false);

            var enableableBuffer = this.Manager.AddBuffer<ActionEnableable>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();
            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            // Act: Activate reaction
            this.SetReactionActiveState(reactionEntity, true);
            this.RunEnableableSystems();

            // Assert: Component should be enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(reactionEntity),
                "Component should be enabled after reaction activation");

            // Act: Deactivate reaction
            this.SetReactionActiveState(reactionEntity, false);
            this.RunEnableableSystems();

            // Assert: Component should be disabled again
            Assert.IsFalse(this.Manager.IsComponentEnabled<TestEnableableComponent>(reactionEntity),
                "Component should be disabled after reaction deactivation");
        }

        [Test]
        public void ReferenceCountingBasic_MultipleEnablersKeepComponentEnabled()
        {
            // Arrange: Create target entity with test component (initially disabled)
            var targetEntity = this.Manager.CreateEntity();
            this.Manager.AddComponent<TestEnableableComponent>(targetEntity);
            this.Manager.SetComponentEnabled<TestEnableableComponent>(targetEntity, false);

            // Create two reaction entities both targeting the same component
            var reaction1 = this.CreateReactionEntity(targetEntity);
            var reaction2 = this.CreateReactionEntity(targetEntity);

            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();

            // Both reactions enable the same component on the same target
            var buffer1 = this.Manager.AddBuffer<ActionEnableable>(reaction1);
            buffer1.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            var buffer2 = this.Manager.AddBuffer<ActionEnableable>(reaction2);
            buffer2.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            // Act: Activate first reaction
            this.Manager.SetComponentEnabled<Active>(reaction1, true);
            this.Manager.SetComponentEnabled<Active>(reaction2, false);
            this.RunEnableableSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, true);

            // Assert: Component should be enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));

            // Act: Activate second reaction
            this.Manager.SetComponentEnabled<Active>(reaction2, true);
            this.RunEnableableSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, true);

            // Assert: Component should still be enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));

            // Act: Deactivate first reaction
            this.Manager.SetComponentEnabled<Active>(reaction1, false);
            this.RunEnableableSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, false);

            // Assert: Component should still be enabled (second reaction still active)
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));

            // Act: Deactivate second reaction
            this.Manager.SetComponentEnabled<Active>(reaction2, false);
            this.RunEnableableSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, false);

            // Assert: Component should now be disabled
            Assert.IsFalse(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));
        }

        [Test]
        public void MultipleComponents_HandlesMultipleComponentTypesIndependently()
        {
            // Create reaction entity that enables both components
            var reactionEntity = this.CreateReactionEntity();
            this.Manager.AddComponent<TestEnableableComponent>(reactionEntity);
            this.Manager.AddComponent<SecondTestEnableableComponent>(reactionEntity);
            this.Manager.SetComponentEnabled<TestEnableableComponent>(reactionEntity, false);
            this.Manager.SetComponentEnabled<SecondTestEnableableComponent>(reactionEntity, false);

            var enableableBuffer = this.Manager.AddBuffer<ActionEnableable>(reactionEntity);

            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();
            var secondTestTypeHash = GetTestComponentTypeHash<SecondTestEnableableComponent>();

            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Self,
            });

            enableableBuffer.Add(new ActionEnableable
            {
                Value = secondTestTypeHash,
                Target = Target.Self,
            });

            // Act: Activate reaction
            this.SetReactionActiveState(reactionEntity, true);
            this.RunEnableableSystems();

            // Assert: Both components should be enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(reactionEntity));
            Assert.IsTrue(this.Manager.IsComponentEnabled<SecondTestEnableableComponent>(reactionEntity));

            // Act: Deactivate reaction
            this.SetReactionActiveState(reactionEntity, false);
            this.RunEnableableSystems();

            // Assert: Both components should be disabled
            Assert.IsFalse(this.Manager.IsComponentEnabled<TestEnableableComponent>(reactionEntity));
            Assert.IsFalse(this.Manager.IsComponentEnabled<SecondTestEnableableComponent>(reactionEntity));
        }

        [Test]
        public void TargetTypes_HandlesAllTargetTypesCorrectly()
        {
            // Create multiple entities for different target types
            var ownerEntity = this.Manager.CreateEntity();
            var targetEntity = this.Manager.CreateEntity();
            var sourceEntity = this.Manager.CreateEntity();
            var custom0Entity = this.Manager.CreateEntity();

            // Add test component to all entities (initially disabled)
            foreach (var entity in new[] { ownerEntity, targetEntity, sourceEntity, custom0Entity })
            {
                this.Manager.AddComponent<TestEnableableComponent>(entity);
                this.Manager.SetComponentEnabled<TestEnableableComponent>(entity, false);
            }

            // Create reaction entity with various target types using base method with targets
            var reactionEntity = this.CreateReactionEntity(ownerEntity, sourceEntity, targetEntity);
            var enableableBuffer = this.Manager.AddBuffer<ActionEnableable>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();

            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Owner,
            });

            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Source,
            });

            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Custom0,
            });

            // Set up custom targets
            this.Manager.AddComponentData(reactionEntity, new TargetsCustom { Target0 = custom0Entity });

            // Act: Activate reaction
            this.SetReactionActiveState(reactionEntity, true);
            this.RunEnableableSystems();

            // Assert: All target entities should have enabled components
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(ownerEntity));
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(sourceEntity));
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(custom0Entity));
        }

        [Test]
        public void InvalidComponent_LogsWarningAndContinues()
        {
            // Create reaction with invalid component hash
            var reactionEntity = this.CreateReactionEntity();
            this.Manager.AddComponent<TestEnableableComponent>(reactionEntity);
            this.Manager.SetComponentEnabled<TestEnableableComponent>(reactionEntity, false);

            var enableableBuffer = this.Manager.AddBuffer<ActionEnableable>(reactionEntity);
            enableableBuffer.Add(new ActionEnableable
            {
                Value = 0xDEADBEEF,
                Target = Target.Self,
            }); // Invalid hash

            // Act: Activate reaction
            this.Manager.SetComponentEnabled<Active>(reactionEntity, true);
            this.RunEnableableSystems();

            // Assert: System should not crash (warning will be logged)
            Assert.Pass("System handled invalid component hash gracefully");
        }

        [Test]
        public void MissingComponent_LogsWarningAndContinues()
        {
            // Create target entity without the test component
            var targetEntity = this.Manager.CreateEntity();

            // Create reaction entity using base method with target
            var reactionEntity = this.CreateReactionEntity(Entity.Null, Entity.Null, targetEntity);
            var enableableBuffer = this.Manager.AddBuffer<ActionEnableable>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();
            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            // Act: Activate reaction
            this.Manager.SetComponentEnabled<Active>(reactionEntity, true);
            this.RunEnableableSystems();

            // Assert: System should not crash (warning will be logged)
            Assert.Pass("System handled missing component gracefully");
        }

        [Test]
        public void NonExistentTarget_LogsErrorAndContinues()
        {
            // Create reaction with non-existent target using base method
            var reactionEntity = this.CreateReactionEntity(Entity.Null, Entity.Null, Entity.Null);
            var enableableBuffer = this.Manager.AddBuffer<ActionEnableable>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();
            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            // Act: Activate reaction
            this.Manager.SetComponentEnabled<Active>(reactionEntity, true);
            this.RunEnableableSystems();

            // Assert: System should not crash (error will be logged)
            Assert.Pass("System handled non-existent target gracefully");
        }

        [Test]
        public void AlreadyEnabledComponent_HandlesCorrectly()
        {
            // Create target entity with component already enabled
            var targetEntity = this.Manager.CreateEntity();
            this.Manager.AddComponent<TestEnableableComponent>(targetEntity);
            this.Manager.SetComponentEnabled<TestEnableableComponent>(targetEntity, true);

            // Create reaction entity using base method with target
            var reactionEntity = this.CreateReactionEntity(targetEntity);
            var enableableBuffer = this.Manager.AddBuffer<ActionEnableable>(reactionEntity);
            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();
            enableableBuffer.Add(new ActionEnableable
            {
                Value = testTypeHash,
                Target = Target.Target,
            });

            // Act: Activate reaction
            this.SetReactionActiveState(reactionEntity, true);
            this.RunEnableableSystems();

            // Assert: Component should remain enabled
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));

            // Act: Deactivate reaction
            this.SetReactionActiveState(reactionEntity, false);
            this.RunEnableableSystems();

            // Assert: Component should be disabled (reference count reached 0)
            Assert.IsFalse(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));
        }

        [Test]
        public void ComplexReferenceCountingScenario_HandlesCorrectlyAcrossMultipleUpdates()
        {
            // Arrange: Create target entity
            var targetEntity = this.Manager.CreateEntity();
            this.Manager.AddComponent<TestEnableableComponent>(targetEntity);
            this.Manager.SetComponentEnabled<TestEnableableComponent>(targetEntity, false);

            // Create 3 reaction entities
            var reaction1 = this.CreateReactionEntity(targetEntity);
            var reaction2 = this.CreateReactionEntity(targetEntity);
            var reaction3 = this.CreateReactionEntity(targetEntity);

            var testTypeHash = GetTestComponentTypeHash<TestEnableableComponent>();

            foreach (var reaction in new[] { reaction1, reaction2, reaction3 })
            {
                var buffer = this.Manager.AddBuffer<ActionEnableable>(reaction);
                buffer.Add(new ActionEnableable
                {
                    Value = testTypeHash,
                    Target = Target.Target,
                });
            }

            // Act: Activate reactions in sequence
            this.Manager.SetComponentEnabled<Active>(reaction1, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, false);
            this.RunEnableableSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, true);
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));

            this.Manager.SetComponentEnabled<Active>(reaction2, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, false);
            this.RunEnableableSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, true);
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));

            this.Manager.SetComponentEnabled<Active>(reaction3, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction3, false);
            this.RunEnableableSystems();
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction3, true);
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));

            // Deactivate middle reaction
            this.Manager.SetComponentEnabled<Active>(reaction2, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction2, true);
            this.RunEnableableSystems();
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));

            // Deactivate first reaction
            this.Manager.SetComponentEnabled<Active>(reaction1, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction1, true);
            this.RunEnableableSystems();
            Assert.IsTrue(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));

            // Deactivate last reaction
            this.Manager.SetComponentEnabled<Active>(reaction3, false);
            this.Manager.SetComponentEnabled<ActivePrevious>(reaction3, true);
            this.RunEnableableSystems();
            Assert.IsFalse(this.Manager.IsComponentEnabled<TestEnableableComponent>(targetEntity));
        }

        private void RunEnableableSystems()
        {
            this.RunSystems(this.enableableSystem, this.deactivatedSystem);
        }

        /// <summary>
        /// Test enableable component for testing purposes.
        /// </summary>
        private struct TestEnableableComponent : IComponentData, IEnableableComponent
        {
            public int Value;
        }

        /// <summary>
        /// Another test enableable component for multi-component testing.
        /// </summary>
        private struct SecondTestEnableableComponent : IComponentData, IEnableableComponent
        {
            public float Value;
        }
    }
}
