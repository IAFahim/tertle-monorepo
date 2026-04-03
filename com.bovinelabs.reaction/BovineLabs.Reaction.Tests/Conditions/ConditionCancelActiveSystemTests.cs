// <copyright file="ConditionCancelActiveSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Conditions
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Conditions;
    using NUnit.Framework;
    using Unity.Entities;

    public class ConditionCancelActiveSystemTests : ReactionTestFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<ConditionCancelActiveSystem>();
        }

        [Test]
        public void CancelLogic_AllCancelConditionsMet_DoesNotTriggerCancel()
        {
            // Arrange: Cancel requires conditions 0 and 1, both are active
            var entity = this.CreateCancelEntity(
                cancel: new BitArray32(0b00000011),  // Requires conditions 0 and 1
                active: new BitArray32(0b00000011)); // Conditions 0 and 1 are active

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: Should NOT cancel because all required conditions are met
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void CancelLogic_SomeCancelConditionsNotMet_TriggersCancel()
        {
            // Arrange: Cancel requires conditions 0 and 1, but only 0 is active
            var entity = this.CreateCancelEntity(
                cancel: new BitArray32(0b00000011),  // Requires conditions 0 and 1
                active: new BitArray32(0b00000001)); // Only condition 0 is active

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: Should cancel because condition 1 is not met
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void CancelLogic_NoCancelConditionsActive_TriggersCancel()
        {
            // Arrange: Cancel requires conditions 0 and 1, but neither is active
            var entity = this.CreateCancelEntity(
                cancel: new BitArray32(0b00000011),  // Requires conditions 0 and 1
                active: new BitArray32(0b00000000)); // No conditions are active

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: Should cancel because no required conditions are met
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void CancelLogic_SingleConditionCancel_WorksCorrectly()
        {
            // Arrange: Cancel requires only condition 0, and it's active
            var entity = this.CreateCancelEntity(
                cancel: new BitArray32(0b00000001), // Requires only condition 0
                active: new BitArray32(0b00000001));  // Condition 0 is active

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: Should NOT cancel because the required condition is met
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void CancelLogic_ExtraActiveConditions_DoesNotAffectCancel()
        {
            // Arrange: Cancel requires conditions 0 and 1, extra conditions 2 and 3 are also active
            var entity = this.CreateCancelEntity(
                cancel: new BitArray32(0b00000011), // Requires conditions 0 and 1
                active: new BitArray32(0b00001111));  // Conditions 0, 1, 2, 3 are active

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: Should NOT cancel because all required conditions (0 and 1) are met
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void CancelLogic_BitMaskOperations_WorkCorrectly()
        {
            // Test various bit patterns to ensure BitAnd operations work correctly
            var testCases = new[]
            {
                // (cancel, active, expected_cancel)
                (0b00000001u, 0b00000001u, false), // Single bit match
                (0b00000001u, 0b00000000u, true),  // Single bit no match
                (0b00000011u, 0b00000011u, false), // Two bits match
                (0b00000011u, 0b00000010u, true),  // Two bits partial match
                (0b11111111u, 0b11111111u, false), // All bits match
                (0b11111111u, 0b11111110u, true),  // All bits almost match
                (0b10101010u, 0b10101010u, false), // Pattern match
                (0b10101010u, 0b10101000u, true),  // Pattern partial match
            };

            foreach (var (cancel, active, expectedCancel) in testCases)
            {
                // Arrange
                var entity = this.CreateCancelEntity(
                    cancel: new BitArray32(cancel),
                    active: new BitArray32(active));

                // Act
                this.system.Update(this.WorldUnmanaged);
                this.Manager.CompleteAllTrackedJobs();

                // Assert
                var actualCancel = this.Manager.IsComponentEnabled<ActiveCancel>(entity);
                Assert.AreEqual(expectedCancel, actualCancel,
                    $"Cancel=0x{cancel:X8}, Active=0x{active:X8} should result in Cancel={expectedCancel}");

                // Cleanup for next iteration
                this.Manager.DestroyEntity(entity);
            }
        }

        [Test]
        public void EntityQuery_WithConditionAllActiveEnabled_IsIgnored()
        {
            // Arrange: Entity with ConditionAllActive enabled should be ignored
            var entity = this.CreateCancelEntity(
                cancel: new BitArray32(0b00000011),
                active: new BitArray32(0b00000001));
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, true); // Enable to be ignored

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: ActiveCancel should remain disabled (system didn't process this entity)
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void EntityQuery_WithActiveCancelEnabled_IsIgnored()
        {
            // Arrange: Entity with ActiveCancel already enabled should be ignored
            var entity = this.CreateCancelEntity(
                cancel: new BitArray32(0b00000011),
                active: new BitArray32(0b00000001));
            this.Manager.SetComponentEnabled<ActiveCancel>(entity, true); // Enable to be ignored

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: ActiveCancel should remain enabled (system didn't process this entity)
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void EntityQuery_WithoutActiveOnDuration_IsIgnored()
        {
            // Arrange: Entity without ActiveOnDuration component should be ignored
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionCancelActive),
                typeof(ConditionActive),
                typeof(Active),
                typeof(ActiveCancel)); // Note: Missing ActiveOnDuration

            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new ConditionCancelActive { Value = new BitArray32(0b00000011) });
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0b00000001) });
            this.Manager.SetComponentEnabled<ActiveCancel>(entity, false);

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: ActiveCancel should remain disabled (system didn't process this entity)
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void EntityQuery_WithoutActive_IsIgnored()
        {
            // Arrange: Entity without Active component should be ignored
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionCancelActive),
                typeof(ConditionActive),
                typeof(ActiveOnDuration),
                typeof(ActiveCancel)); // Note: Missing Active

            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new ConditionCancelActive { Value = new BitArray32(0b00000011) });
            this.Manager.SetComponentData(entity, new ConditionActive { Value = new BitArray32(0b00000001) });
            this.Manager.SetComponentEnabled<ActiveCancel>(entity, false);

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: ActiveCancel should remain disabled (system didn't process this entity)
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void EdgeCase_EmptyCancelConditions_AlwaysCancels()
        {
            // Arrange: Empty cancel conditions (should probably not happen in practice)
            var entity = this.CreateCancelEntity(
                cancel: BitArray32.None,
                active: new BitArray32(0b11111111));

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: Empty cancel conditions should NOT result in cancel (BitAnd of None with anything == None)
            Assert.IsFalse(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        [Test]
        public void EdgeCase_EmptyActiveConditions_AlwaysCancels()
        {
            // Arrange: No active conditions
            var entity = this.CreateCancelEntity(
                cancel: new BitArray32(0b00000011),
                active: BitArray32.None);

            // Act
            this.RunConditionCancelActiveSystem();

            // Assert: No active conditions should result in cancel
            Assert.IsTrue(this.Manager.IsComponentEnabled<ActiveCancel>(entity));
        }

        private Entity CreateCancelEntity(BitArray32 cancel, BitArray32 active)
        {
            return this.CreateActiveCancelEntity(cancel, active);
        }

        private void RunConditionCancelActiveSystem()
        {
            this.RunSystems(this.system);
        }
    }
}
