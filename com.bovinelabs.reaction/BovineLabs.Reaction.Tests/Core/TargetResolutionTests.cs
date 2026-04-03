// <copyright file="TargetResolutionTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Core
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;
    using UnityEngine.TestTools;

    /// <summary>
    /// Tests for target resolution functionality in the <see cref="Targets"/> component,
    /// verifying all 7 target types are correctly resolved to entities.
    /// </summary>
    public class TargetResolutionTests : ReactionTestFixture
    {
        private ComponentLookup<TargetsCustom> TargetsCustomLookup => this.Manager.GetComponentLookup<TargetsCustom>(true);

        [Test]
        public void TargetResolution_None_ReturnsEntityNull()
        {
            // Arrange
            var targets = new Targets
            {
                Owner = this.Manager.CreateEntity(),
                Source = this.Manager.CreateEntity(),
                Target = this.Manager.CreateEntity(),
            };
            var selfEntity = this.Manager.CreateEntity();

            // Act
            var result = targets.Get(Target.None, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(Entity.Null, result, "Target.None should resolve to Entity.Null");
        }

        [Test]
        public void TargetResolution_Target_ReturnsTargetEntity()
        {
            // Arrange
            var targetEntity = this.Manager.CreateEntity();
            var targets = new Targets
            {
                Owner = this.Manager.CreateEntity(),
                Source = this.Manager.CreateEntity(),
                Target = targetEntity,
            };
            var selfEntity = this.Manager.CreateEntity();

            // Act
            var result = targets.Get(Target.Target, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(targetEntity, result, "Target.Target should resolve to the Target entity");
        }

        [Test]
        public void TargetResolution_Owner_ReturnsOwnerEntity()
        {
            // Arrange
            var ownerEntity = this.Manager.CreateEntity();
            var targets = new Targets
            {
                Owner = ownerEntity,
                Source = this.Manager.CreateEntity(),
                Target = this.Manager.CreateEntity(),
            };
            var selfEntity = this.Manager.CreateEntity();

            // Act
            var result = targets.Get(Target.Owner, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(ownerEntity, result, "Target.Owner should resolve to the Owner entity");
        }

        [Test]
        public void TargetResolution_Source_ReturnsSourceEntity()
        {
            // Arrange
            var sourceEntity = this.Manager.CreateEntity();
            var targets = new Targets
            {
                Owner = this.Manager.CreateEntity(),
                Source = sourceEntity,
                Target = this.Manager.CreateEntity(),
            };
            var selfEntity = this.Manager.CreateEntity();

            // Act
            var result = targets.Get(Target.Source, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(sourceEntity, result, "Target.Source should resolve to the Source entity");
        }

        [Test]
        public void TargetResolution_Self_ReturnsSelfEntity()
        {
            // Arrange
            var targets = new Targets
            {
                Owner = this.Manager.CreateEntity(),
                Source = this.Manager.CreateEntity(),
                Target = this.Manager.CreateEntity(),
            };
            var selfEntity = this.Manager.CreateEntity();

            // Act
            var result = targets.Get(Target.Self, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(selfEntity, result, "Target.Self should resolve to the self entity parameter");
        }

        [Test]
        public void TargetResolution_Custom0_WithTargetsCustom_ReturnsTarget0()
        {
            // Arrange
            var custom0Entity = this.Manager.CreateEntity();
            var selfEntity = this.Manager.CreateEntity();

            this.Manager.AddComponentData(selfEntity, new TargetsCustom
            {
                Target0 = custom0Entity,
                Target1 = this.Manager.CreateEntity(),
            });

            var targets = new Targets
            {
                Owner = this.Manager.CreateEntity(),
                Source = this.Manager.CreateEntity(),
                Target = this.Manager.CreateEntity(),
            };

            // Act
            var result = targets.Get(Target.Custom0, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(custom0Entity, result, "Target.Custom0 should resolve to Target0 from TargetsCustom");
        }

        [Test]
        public void TargetResolution_Custom1_WithTargetsCustom_ReturnsTarget1()
        {
            // Arrange
            var custom1Entity = this.Manager.CreateEntity();
            var selfEntity = this.Manager.CreateEntity();

            this.Manager.AddComponentData(selfEntity, new TargetsCustom
            {
                Target0 = this.Manager.CreateEntity(),
                Target1 = custom1Entity,
            });

            var targets = new Targets
            {
                Owner = this.Manager.CreateEntity(),
                Source = this.Manager.CreateEntity(),
                Target = this.Manager.CreateEntity(),
            };

            // Act
            var result = targets.Get(Target.Custom1, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(custom1Entity, result, "Target.Custom1 should resolve to Target1 from TargetsCustom");
        }

        [Test]
        public void TargetResolution_Custom0_WithoutTargetsCustom_ReturnsEntityNull()
        {
            // Arrange
            var targets = new Targets
            {
                Owner = this.Manager.CreateEntity(),
                Source = this.Manager.CreateEntity(),
                Target = this.Manager.CreateEntity(),
            };

            // Note: selfEntity does not have TargetsCustom component
            var selfEntity = this.Manager.CreateEntity();

            // Expect the error log from GetCustom method
            LogAssert.Expect(UnityEngine.LogType.Error, $"Trying to get custom targets on {selfEntity.ToFixedString()} but doesn't have TargetsCustom component");

            // Act
            var result = targets.Get(Target.Custom0, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(Entity.Null, result, "Target.Custom0 without TargetsCustom should resolve to Entity.Null");
        }

        [Test]
        public void TargetResolution_Custom1_WithoutTargetsCustom_ReturnsEntityNull()
        {
            // Arrange
            var targets = new Targets
            {
                Owner = this.Manager.CreateEntity(),
                Source = this.Manager.CreateEntity(),
                Target = this.Manager.CreateEntity(),
            };

            // Note: selfEntity does not have TargetsCustom component
            var selfEntity = this.Manager.CreateEntity();

            // Expect the error log from GetCustom method
            LogAssert.Expect(UnityEngine.LogType.Error, $"Trying to get custom targets on {selfEntity.ToFixedString()} but doesn't have TargetsCustom component");

            // Act
            var result = targets.Get(Target.Custom1, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(Entity.Null, result, "Target.Custom1 without TargetsCustom should resolve to Entity.Null");
        }

        [Test]
        public void TargetResolution_InvalidTargetValue_ReturnsEntityNull()
        {
            // Arrange
            var targets = new Targets
            {
                Owner = this.Manager.CreateEntity(),
                Source = this.Manager.CreateEntity(),
                Target = this.Manager.CreateEntity(),
            };
            var selfEntity = this.Manager.CreateEntity();
            var invalidTarget = (Target)99; // Invalid enum value

            // Act
            var result = targets.Get(invalidTarget, selfEntity, this.TargetsCustomLookup);

            // Assert
            Assert.AreEqual(Entity.Null, result, "Invalid target enum value should resolve to Entity.Null");
        }

        [Test]
        public void TargetResolution_EntityNullTargets_ReturnsEntityNull()
        {
            // Arrange
            var targets = new Targets
            {
                Owner = Entity.Null,
                Source = Entity.Null,
                Target = Entity.Null,
            };
            var selfEntity = this.Manager.CreateEntity();

            // Act & Assert
            Assert.AreEqual(Entity.Null, targets.Get(Target.Owner, selfEntity, this.TargetsCustomLookup),
                "Entity.Null Owner should resolve to Entity.Null");
            Assert.AreEqual(Entity.Null, targets.Get(Target.Source, selfEntity, this.TargetsCustomLookup),
                "Entity.Null Source should resolve to Entity.Null");
            Assert.AreEqual(Entity.Null, targets.Get(Target.Target, selfEntity, this.TargetsCustomLookup),
                "Entity.Null Target should resolve to Entity.Null");
        }

        [Test]
        public void TargetResolution_AllValidTargets_ResolveCorrectly()
        {
            // Arrange
            var ownerEntity = this.Manager.CreateEntity();
            var sourceEntity = this.Manager.CreateEntity();
            var targetEntity = this.Manager.CreateEntity();
            var selfEntity = this.Manager.CreateEntity();
            var custom0Entity = this.Manager.CreateEntity();
            var custom1Entity = this.Manager.CreateEntity();

            var targets = new Targets
            {
                Owner = ownerEntity,
                Source = sourceEntity,
                Target = targetEntity,
            };

            this.Manager.AddComponentData(selfEntity, new TargetsCustom
            {
                Target0 = custom0Entity,
                Target1 = custom1Entity,
            });

            // Act & Assert - Test all target types in one comprehensive test
            Assert.AreEqual(Entity.Null, targets.Get(Target.None, selfEntity, this.TargetsCustomLookup), "None failed");
            Assert.AreEqual(targetEntity, targets.Get(Target.Target, selfEntity, this.TargetsCustomLookup), "Target failed");
            Assert.AreEqual(ownerEntity, targets.Get(Target.Owner, selfEntity, this.TargetsCustomLookup), "Owner failed");
            Assert.AreEqual(sourceEntity, targets.Get(Target.Source, selfEntity, this.TargetsCustomLookup), "Source failed");
            Assert.AreEqual(selfEntity, targets.Get(Target.Self, selfEntity, this.TargetsCustomLookup), "Self failed");
            Assert.AreEqual(custom0Entity, targets.Get(Target.Custom0, selfEntity, this.TargetsCustomLookup), "Custom0 failed");
            Assert.AreEqual(custom1Entity, targets.Get(Target.Custom1, selfEntity, this.TargetsCustomLookup), "Custom1 failed");
        }

        [Test]
        public void TargetsCopy_UpdatesSourceAndTarget_PreservesOwner()
        {
            // Arrange
            var originalOwner = this.Manager.CreateEntity();
            var originalSource = this.Manager.CreateEntity();
            var originalTarget = this.Manager.CreateEntity();
            var newSource = this.Manager.CreateEntity();
            var newTarget = this.Manager.CreateEntity();

            var originalTargets = new Targets
            {
                Owner = originalOwner,
                Source = originalSource,
                Target = originalTarget,
            };

            // Act
            var copiedTargets = originalTargets.Copy(newSource, newTarget);

            // Assert
            Assert.AreEqual(originalOwner, copiedTargets.Owner, "Owner should be preserved in copy");
            Assert.AreEqual(newSource, copiedTargets.Source, "Source should be updated in copy");
            Assert.AreEqual(newTarget, copiedTargets.Target, "Target should be updated in copy");
        }

        [Test]
        public void TargetsCopy_DefaultTarget_PreservesOriginalTarget()
        {
            // Arrange
            var originalOwner = this.Manager.CreateEntity();
            var originalSource = this.Manager.CreateEntity();
            var originalTarget = this.Manager.CreateEntity();
            var newSource = this.Manager.CreateEntity();

            var originalTargets = new Targets
            {
                Owner = originalOwner,
                Source = originalSource,
                Target = originalTarget,
            };

            // Act - Pass default for target parameter
            var copiedTargets = originalTargets.Copy(newSource);

            // Assert
            Assert.AreEqual(originalOwner, copiedTargets.Owner, "Owner should be preserved in copy");
            Assert.AreEqual(newSource, copiedTargets.Source, "Source should be updated in copy");
            Assert.AreEqual(originalTarget, copiedTargets.Target, "Target should be preserved when default is passed");
        }
    }
}
