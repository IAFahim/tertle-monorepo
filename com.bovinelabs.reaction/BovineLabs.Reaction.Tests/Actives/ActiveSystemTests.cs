// <copyright file="ActiveSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Actives
{
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Conditions;
    using NUnit.Framework;
    using Unity.Entities;

    /// <summary>
    /// Tests for <see cref="ActiveSystem"/>, verifying active state determination
    /// across all 16 component combinations.
    /// </summary>
    public class ActiveSystemTests : ReactionTestFixture
    {
        private SystemHandle activeSystem;
        private SystemHandle activeTriggerResetSystem;

        /// <inheritdoc/>
        public override void Setup()
        {
            base.Setup();
            this.activeSystem = this.World.CreateSystem<ActiveSystem>();
            this.activeTriggerResetSystem = this.World.CreateSystem<ActiveTriggerSystem>();
        }

        [Test]
        public void Case_None_AlwaysActive()
        {
            // Create entity with only Active component (user controlled)
            var archetype = this.Manager.CreateArchetype(typeof(Active));
            var entity = this.Manager.CreateEntity(archetype);

            // Test user setting it to active
            this.Manager.SetComponentEnabled<Active>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "User-controlled active should remain true");

            // Test user setting it to inactive
            this.Manager.SetComponentEnabled<Active>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "User-controlled active should toggle true");
        }

        [Test]
        public void Case_Duration_OnlyActiveWhenDurationActive()
        {
            // Create entity with Duration component
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ActiveOnDuration));
            var entity = this.Manager.CreateEntity(archetype);

            // Initially both should be inactive
            this.Manager.SetComponentEnabled<Active>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should activate");

            // Activate duration
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should remain active when duration is on");

            // Deactivate active and duration
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when duration turns off");

            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should reactivate"); // TODO change filters screwing us here
        }

        [Test]
        public void Case_Cooldown_ActiveWhenNotOnCooldown()
        {
            // Create entity with Cooldown component
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ActiveOnCooldown));
            var entity = this.Manager.CreateEntity(archetype);

            // Start with cooldown off (entity should be active)
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when not on cooldown");

            // Activate cooldown (entity should be inactive)
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when on cooldown");

            // Deactivate cooldown (entity should be active again)
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when cooldown ends");
        }

        [Test]
        public void Case_CooldownDuration_CombinesLogicCorrectly()
        {
            // Create entity with both Duration and Cooldown
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ActiveOnDuration), typeof(ActiveOnCooldown));
            var entity = this.Manager.CreateEntity(archetype);

            // Test: Duration off, Cooldown off -> should be active (because NOT on cooldown)
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when not on cooldown, even without duration");

            // Test: Duration on, Cooldown off -> should be active (duration OR not cooldown)
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when duration on and not on cooldown");

            // Test: Duration off, Cooldown on -> should be inactive (not duration AND on cooldown)
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when on cooldown without duration");

            // Test: Duration on, Cooldown on -> should be active (duration overrides cooldown)
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when duration on, even on cooldown");
        }

        [Test]
        public void Case_Condition_ActiveWhenConditionsMet()
        {
            // Create entity with condition component
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ConditionAllActive));
            var entity = this.Manager.CreateEntity(archetype);

            // Condition not met
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when conditions not met");

            // Condition met
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when conditions met");

            // Condition no longer met
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when conditions no longer met");
        }

        [Test]
        public void Case_ConditionDuration_CombinesLogicCorrectly()
        {
            // Create entity with both Condition and Duration
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ConditionAllActive), typeof(ActiveOnDuration));
            var entity = this.Manager.CreateEntity(archetype);

            // Test: Condition off, Duration off -> inactive
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when neither condition nor duration are active");

            // Test: Condition on, Duration off -> active (condition met)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when condition met");

            // Test: Condition off, Duration on -> active (duration active)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when duration active");

            // Test: Condition on, Duration on -> active (both active)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when both condition and duration active");
        }

        [Test]
        public void Case_ConditionCooldown_CombinesLogicCorrectly()
        {
            // Create entity with both Condition and Cooldown
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ConditionAllActive), typeof(ActiveOnCooldown));
            var entity = this.Manager.CreateEntity(archetype);

            // Test: Condition off, Cooldown off -> inactive (condition not met)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when condition not met");

            // Test: Condition on, Cooldown off -> active (condition met and not on cooldown)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when condition met and not on cooldown");

            // Test: Condition off, Cooldown on -> inactive (condition not met)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when condition not met, regardless of cooldown");

            // Test: Condition on, Cooldown on -> inactive (on cooldown blocks activation)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when on cooldown, even with condition met");
        }

        [Test]
        public void Case_ConditionDurationCooldown_CombinesAllLogicCorrectly()
        {
            // Create entity with all three components
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(ConditionAllActive),
                typeof(ActiveOnDuration),
                typeof(ActiveOnCooldown));
            var entity = this.Manager.CreateEntity(archetype);

            // Test all 8 combinations (2^3)
            // Format: Condition, Duration, Cooldown -> Expected result

            // 0,0,0 -> false (nothing active)
            this.SetComponentStates(entity, false, false, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Case 0,0,0 should be inactive");

            // 0,0,1 -> false (on cooldown, nothing to override)
            this.SetComponentStates(entity, false, false, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Case 0,0,1 should be inactive");

            // 0,1,0 -> true (duration active)
            this.SetComponentStates(entity, false, true, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Case 0,1,0 should be active");

            // 0,1,1 -> true (duration overrides cooldown)
            this.SetComponentStates(entity, false, true, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Case 0,1,1 should be active");

            // 1,0,0 -> true (condition met, not on cooldown)
            this.SetComponentStates(entity, true, false, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Case 1,0,0 should be active");

            // 1,0,1 -> false (condition met but on cooldown)
            this.SetComponentStates(entity, true, false, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Case 1,0,1 should be inactive");

            // 1,1,0 -> true (both condition and duration active)
            this.SetComponentStates(entity, true, true, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Case 1,1,0 should be active");

            // 1,1,1 -> true (duration overrides cooldown, condition also met)
            this.SetComponentStates(entity, true, true, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Case 1,1,1 should be active");
        }

        [Test]
        public void Case_Trigger_ActivatesAndAutomaticallyResets()
        {
            // Create entity with Trigger component
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ActiveTrigger));
            var entity = this.Manager.CreateEntity(archetype);

            // Start with trigger off
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when trigger not set");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should remain off");

            // Activate trigger
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when trigger is set");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should be automatically reset");

            // Run again to ensure it stays reset
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive after trigger reset");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should remain reset");
        }

        [Test]
        public void Case_DurationTrigger_CombinesLogicCorrectly()
        {
            // Create entity with both Duration and Trigger
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ActiveOnDuration), typeof(ActiveTrigger));
            var entity = this.Manager.CreateEntity(archetype);

            // Test: Duration off, Trigger off -> inactive
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when neither duration nor trigger active");

            // Test: Duration on, Trigger off -> active (duration active)
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when duration active");

            // Test: Duration off, Trigger on -> active (trigger activates, then resets)
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when trigger fires");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should be reset");

            // Next frame, without duration active, should be inactive
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive after trigger reset without duration");
        }

        [Test]
        public void Case_CooldownTrigger_TriggerBlockedByCooldown()
        {
            // Create entity with both Cooldown and Trigger
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ActiveOnCooldown), typeof(ActiveTrigger));
            var entity = this.Manager.CreateEntity(archetype);

            // Test: Not on cooldown, trigger fires -> should activate
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when trigger fires and not on cooldown");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should be reset");

            // Test: On cooldown, trigger fires -> should NOT activate
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when trigger fires but on cooldown");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should still be reset even when blocked");
        }

        [Test]
        public void Case_CooldownDurationTrigger_CombinesAllLogicCorrectly()
        {
            // Create entity with Duration, Cooldown, and Trigger
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(ActiveOnDuration),
                typeof(ActiveOnCooldown),
                typeof(ActiveTrigger));
            var entity = this.Manager.CreateEntity(archetype);

            // Test: Duration overrides cooldown even with trigger
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Duration should override cooldown");

            // Test: Trigger blocked by cooldown when duration not active
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Trigger should be blocked by cooldown");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should be reset");

            // Test: Trigger works when not on cooldown and duration not active
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Trigger should work when not on cooldown");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should be reset");
        }

        [Test]
        public void Case_ConditionTrigger_BothMustBeActive()
        {
            // Create entity with both Condition and Trigger
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ConditionAllActive), typeof(ActiveTrigger));
            var entity = this.Manager.CreateEntity(archetype);

            // Test: Condition off, Trigger on -> inactive (condition not met)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when condition not met, even with trigger");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should still be reset");

            // Test: Condition on, Trigger on -> active (both met)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when both condition and trigger active");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should be reset");

            // Test: Condition on, Trigger off -> inactive (trigger required)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when trigger not set, even with condition");
        }

        [Test]
        public void Case_ConditionDurationTrigger_CombinesLogicCorrectly()
        {
            // Create entity with Condition, Duration, and Trigger
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(ConditionAllActive),
                typeof(ActiveOnDuration),
                typeof(ActiveTrigger));
            var entity = this.Manager.CreateEntity(archetype);

            // Test: Duration active alone -> active (overrides other requirements)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Duration should override other requirements");

            // Test: Condition and Trigger both active, Duration off -> active
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when both condition and trigger met");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should be reset");

            // Test: Only condition active -> inactive (trigger required when duration not active)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when only condition met without trigger");
        }

        [Test]
        public void Case_ConditionCooldownTrigger_AllMustAlign()
        {
            // Create entity with Condition, Cooldown, and Trigger
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(ConditionAllActive),
                typeof(ActiveOnCooldown),
                typeof(ActiveTrigger));
            var entity = this.Manager.CreateEntity(archetype);

            // Test: All conditions met (Condition on, not on cooldown, trigger fires)
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when all conditions align");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should be reset");

            // Test: Condition met, trigger fires, but on cooldown -> inactive
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when on cooldown");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should still be reset");

            // Test: Not on cooldown, trigger fires, but condition not met -> inactive
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when condition not met");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should still be reset");
        }

        [Test]
        public void Case_ConditionDurationCooldownTrigger_ComplexLogicCombination()
        {
            // Create entity with all four components
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(ConditionAllActive),
                typeof(ActiveOnDuration),
                typeof(ActiveOnCooldown),
                typeof(ActiveTrigger));
            var entity = this.Manager.CreateEntity(archetype);

            // Test key scenarios from the complex logic:
            // Logic: duration OR (trigger AND condition AND NOT cooldown)

            // Duration active -> should always be active regardless of others
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, true);
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, false);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Duration should override all other conditions");

            // Duration off, all other conditions perfect -> active
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsTrue(this.Manager.IsComponentEnabled<Active>(entity), "Should be active when trigger, condition met, and not on cooldown");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should be reset");

            // Duration off, missing condition -> inactive
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when condition not met");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should still be reset");

            // Duration off, on cooldown -> inactive
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, true);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, true);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when on cooldown");
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveTrigger>(entity), "Trigger should still be reset");

            // Duration off, no trigger -> inactive
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, false);
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, false);
            this.Manager.SetComponentEnabled<ActiveTrigger>(entity, false);
            this.RunActiveSystem();
            Assert.IsFalse(this.Manager.IsComponentEnabled<Active>(entity), "Should be inactive when trigger not set");
        }

        [Test]
        public void MultipleEntitiesInChunk_ProcessedCorrectly()
        {
            // Create multiple entities with the same archetype
            var archetype = this.Manager.CreateArchetype(typeof(Active), typeof(ConditionAllActive));
            var entities = this.Manager.CreateEntity(archetype, 5, Unity.Collections.Allocator.Temp);

            // Set different states for each entity
            for (int i = 0; i < entities.Length; i++)
            {
                this.Manager.SetComponentEnabled<ConditionAllActive>(entities[i], i % 2 == 0);
            }

            this.RunActiveSystem();

            // Verify each entity has correct state
            for (int i = 0; i < entities.Length; i++)
            {
                bool expectedActive = i % 2 == 0;
                Assert.AreEqual(expectedActive, this.Manager.IsComponentEnabled<Active>(entities[i]),
                    $"Entity {i} should have Active={expectedActive}");
            }

            entities.Dispose();
        }

        private void RunActiveSystem()
        {
            this.RunSystems(this.activeSystem, this.activeTriggerResetSystem);
        }

        private void SetComponentStates(Entity entity, bool condition, bool duration, bool cooldown)
        {
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, condition);
            this.Manager.SetComponentEnabled<ActiveOnDuration>(entity, duration);
            this.Manager.SetComponentEnabled<ActiveOnCooldown>(entity, cooldown);
        }
    }
}
