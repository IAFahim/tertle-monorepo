// <copyright file="InitializeTransformSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests.Core
{
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Core;
    using BovineLabs.Reaction.Data.Core;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// Tests for the InitializeTransformSystem that handles transform initialization for newly created reaction entities.
    /// </summary>
    [TestFixture]
    public class InitializeTransformSystemTests : ReactionTestFixture
    {
        private Entity initializeTransformSingleton;
        private ComponentSystemGroup systemGroup;

        [SetUp]
        public void SetUp()
        {
            // Create the InitializeTransform singleton entity with buffer
            this.initializeTransformSingleton = this.Manager.CreateEntity(typeof(InitializeTransform));
            this.Manager.GetBuffer<InitializeTransform>(this.initializeTransformSingleton).Initialize();

            // Create and configure the system group
            this.systemGroup = this.World.CreateSystemManaged<InitializeSystemGroup>();
            var system = this.World.CreateSystem<InitializeTransformSystem>();
            this.systemGroup.AddSystemToUpdateList(system);
        }

        [Test]
        public void InitializeTransformSystem_WithFromPosition_SetsPositionCorrectly()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var fromPosition = new float3(10, 20, 30);
            var toPosition = new float3(100, 200, 300);

            var fromEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            var toEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            this.Manager.SetComponentData(fromEntity, LocalTransform.FromPosition(fromPosition));
            this.Manager.SetComponentData(toEntity, LocalTransform.FromPosition(toPosition));

            var sourceEntity = this.CreateInitializeTransformEntity(objectId, owner: fromEntity, target: toEntity);

            this.AddInitializeTransformData(objectId, this.CreateTransformData(
                position: InitializeTransform.Data.PositionInit.From));

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            Assert.AreEqual(fromPosition, resultTransform.Position, "Position should be set to From target position");
        }

        [Test]
        public void InitializeTransformSystem_WithToPosition_SetsPositionCorrectly()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var fromPosition = new float3(10, 20, 30);
            var toPosition = new float3(100, 200, 300);

            var fromEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            var toEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            this.Manager.SetComponentData(fromEntity, LocalTransform.FromPosition(fromPosition));
            this.Manager.SetComponentData(toEntity, LocalTransform.FromPosition(toPosition));

            var sourceEntity = this.CreateInitializeTransformEntity(objectId, owner: fromEntity, target: toEntity);

            this.AddInitializeTransformData(objectId, this.CreateTransformData(
                position: InitializeTransform.Data.PositionInit.To));

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            Assert.AreEqual(toPosition, resultTransform.Position, "Position should be set to To target position");
        }

        [Test]
        public void InitializeTransformSystem_WithFromRotation_SetsRotationCorrectly()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var fromRotation = quaternion.AxisAngle(math.up(), math.radians(45));
            var toRotation = quaternion.AxisAngle(math.up(), math.radians(90));

            var fromEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            var toEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            var sourceEntity = this.Manager.CreateEntity(typeof(InitializeEntity), typeof(ObjectId), typeof(Targets), typeof(LocalTransform));

            this.Manager.SetComponentData(fromEntity, LocalTransform.FromRotation(fromRotation));
            this.Manager.SetComponentData(toEntity, LocalTransform.FromRotation(toRotation));
            this.Manager.SetComponentData(sourceEntity, objectId);
            this.Manager.SetComponentData(sourceEntity, new Targets
            {
                Owner = fromEntity,
                Source = sourceEntity,
                Target = toEntity,
            });
            this.Manager.SetComponentData(sourceEntity, LocalTransform.Identity);

            // Set up InitializeTransform buffer
            var buffer = this.Manager.GetBuffer<InitializeTransform>(this.initializeTransformSingleton).AsMap();
            buffer.Add(objectId, new InitializeTransform.Data
            {
                From = Target.Owner,
                To = Target.Target,
                Position = InitializeTransform.Data.PositionInit.None,
                Rotation = InitializeTransform.Data.RotationInit.From,
                Scale = InitializeTransform.Data.ScaleInit.None,
                ApplyInitialTransform = false,
            });

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            Assert.IsTrue(math.all(math.abs(fromRotation.value - resultTransform.Rotation.value) < 0.001f),
                "Rotation should be set to From target rotation");
        }

        [Test]
        public void InitializeTransformSystem_WithDirectionRotation_CalculatesDirectionCorrectly()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var fromPosition = new float3(0, 0, 0);
            var toPosition = new float3(10, 0, 0); // Forward direction

            var fromEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            var toEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            this.Manager.SetComponentData(fromEntity, LocalTransform.FromPosition(fromPosition));
            this.Manager.SetComponentData(toEntity, LocalTransform.FromPosition(toPosition));

            var sourceEntity = this.CreateInitializeTransformEntity(objectId, owner: fromEntity, target: toEntity);

            this.AddInitializeTransformData(objectId, this.CreateTransformData(
                rotation: InitializeTransform.Data.RotationInit.Direction));

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            var expectedDirection = math.normalize(toPosition - fromPosition);
            var actualForward = math.mul(resultTransform.Rotation, math.forward());
            Assert.IsTrue(math.distance(expectedDirection, actualForward) < 0.1f,
                "Rotation should point from source to target");
        }

        [Test]
        public void InitializeTransformSystem_WithDistanceScale_CalculatesDistanceCorrectly()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var fromPosition = new float3(0, 0, 0);
            var toPosition = new float3(3, 4, 0); // Distance = 5
            var expectedDistance = math.distance(fromPosition, toPosition);

            var fromEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            var toEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            var sourceEntity = this.Manager.CreateEntity(typeof(InitializeEntity), typeof(ObjectId), typeof(Targets), typeof(LocalTransform));

            this.Manager.SetComponentData(fromEntity, LocalTransform.FromPosition(fromPosition));
            this.Manager.SetComponentData(toEntity, LocalTransform.FromPosition(toPosition));
            this.Manager.SetComponentData(sourceEntity, objectId);
            this.Manager.SetComponentData(sourceEntity, new Targets
            {
                Owner = fromEntity,
                Source = sourceEntity,
                Target = toEntity,
            });
            this.Manager.SetComponentData(sourceEntity, LocalTransform.Identity);

            // Set up InitializeTransform buffer
            var buffer = this.Manager.GetBuffer<InitializeTransform>(this.initializeTransformSingleton).AsMap();
            buffer.Add(objectId, new InitializeTransform.Data
            {
                From = Target.Owner,
                To = Target.Target,
                Position = InitializeTransform.Data.PositionInit.None,
                Rotation = InitializeTransform.Data.RotationInit.None,
                Scale = InitializeTransform.Data.ScaleInit.Distance,
                ApplyInitialTransform = false,
            });

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            Assert.AreEqual(expectedDistance, resultTransform.Scale, 0.001f,
                "Scale should be set to distance between From and To targets");
        }

        [Test]
        public void InitializeTransformSystem_WithApplyInitialTransform_CombinesTransforms()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var originalPosition = new float3(5, 0, 0);
            var fromPosition = new float3(10, 0, 0);

            var fromEntity = this.Manager.CreateEntity(typeof(LocalTransform));
            var sourceEntity = this.Manager.CreateEntity(typeof(InitializeEntity), typeof(ObjectId), typeof(Targets), typeof(LocalTransform));

            this.Manager.SetComponentData(fromEntity, LocalTransform.FromPosition(fromPosition));
            this.Manager.SetComponentData(sourceEntity, objectId);
            this.Manager.SetComponentData(sourceEntity, new Targets
            {
                Owner = fromEntity,
                Source = sourceEntity,
                Target = Entity.Null,
            });
            this.Manager.SetComponentData(sourceEntity, LocalTransform.FromPosition(originalPosition));

            // Set up InitializeTransform buffer
            var buffer = this.Manager.GetBuffer<InitializeTransform>(this.initializeTransformSingleton).AsMap();
            buffer.Add(objectId, new InitializeTransform.Data
            {
                From = Target.Owner,
                To = Target.None,
                Position = InitializeTransform.Data.PositionInit.From,
                Rotation = InitializeTransform.Data.RotationInit.None,
                Scale = InitializeTransform.Data.ScaleInit.None,
                ApplyInitialTransform = true,
            });

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            var expectedPosition = originalPosition + fromPosition; // Combination of both transforms
            Assert.IsTrue(math.distance(expectedPosition, resultTransform.Position) < 0.1f,
                "ApplyInitialTransform should combine the new transform with the existing one");
        }

        [Test]
        public void InitializeTransformSystem_WithParentEntity_UsesLocalToWorld()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var worldPosition = new float3(15, 25, 35);

            var fromEntity = this.Manager.CreateEntity(typeof(LocalToWorld), typeof(Parent));
            var sourceEntity = this.Manager.CreateEntity(typeof(InitializeEntity), typeof(ObjectId), typeof(Targets), typeof(LocalTransform));

            this.Manager.SetComponentData(fromEntity, new LocalToWorld { Value = float4x4.Translate(worldPosition) });
            this.Manager.SetComponentData(sourceEntity, objectId);
            this.Manager.SetComponentData(sourceEntity, new Targets
            {
                Owner = fromEntity,
                Source = sourceEntity,
                Target = Entity.Null,
            });
            this.Manager.SetComponentData(sourceEntity, LocalTransform.Identity);

            // Set up InitializeTransform buffer
            var buffer = this.Manager.GetBuffer<InitializeTransform>(this.initializeTransformSingleton).AsMap();
            buffer.Add(objectId, new InitializeTransform.Data
            {
                From = Target.Owner,
                To = Target.None,
                Position = InitializeTransform.Data.PositionInit.From,
                Rotation = InitializeTransform.Data.RotationInit.None,
                Scale = InitializeTransform.Data.ScaleInit.None,
                ApplyInitialTransform = false,
            });

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            Assert.AreEqual(worldPosition, resultTransform.Position,
                "Position should be extracted from LocalToWorld for entities with Parent component");
        }

        [Test]
        public void InitializeTransformSystem_WithSelfTarget_UsesIdentityTransform()
        {
            // Arrange
            var objectId = new ObjectId(42);
            var originalPosition = new float3(10, 20, 30);

            var sourceEntity = this.Manager.CreateEntity(typeof(InitializeEntity), typeof(ObjectId), typeof(Targets), typeof(LocalTransform));

            this.Manager.SetComponentData(sourceEntity, objectId);
            this.Manager.SetComponentData(sourceEntity, new Targets
            {
                Owner = sourceEntity,
                Source = sourceEntity,
                Target = Entity.Null,
            });
            this.Manager.SetComponentData(sourceEntity, LocalTransform.FromPosition(originalPosition));

            // Set up InitializeTransform buffer - Self target should use identity
            var buffer = this.Manager.GetBuffer<InitializeTransform>(this.initializeTransformSingleton).AsMap();
            buffer.Add(objectId, new InitializeTransform.Data
            {
                From = Target.Self,
                To = Target.None,
                Position = InitializeTransform.Data.PositionInit.From,
                Rotation = InitializeTransform.Data.RotationInit.None,
                Scale = InitializeTransform.Data.ScaleInit.None,
                ApplyInitialTransform = false,
            });

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            Assert.AreEqual(float3.zero, resultTransform.Position,
                "Self target should use identity transform (zero position)");
        }

        [Test]
        public void InitializeTransformSystem_WithInvalidObjectId_DoesNotModifyTransform()
        {
            // Arrange
            var objectId = new ObjectId(999); // Non-existent ObjectId
            var originalTransform = LocalTransform.FromPosition(new float3(5, 10, 15));

            var sourceEntity = this.Manager.CreateEntity(typeof(InitializeEntity), typeof(ObjectId), typeof(Targets), typeof(LocalTransform));

            this.Manager.SetComponentData(sourceEntity, objectId);
            this.Manager.SetComponentData(sourceEntity, new Targets
            {
                Owner = sourceEntity,
                Source = sourceEntity,
                Target = Entity.Null,
            });
            this.Manager.SetComponentData(sourceEntity, originalTransform);

            // Set up InitializeTransform buffer with different ObjectId
            var buffer = this.Manager.GetBuffer<InitializeTransform>(this.initializeTransformSingleton).AsMap();
            buffer.Add(new ObjectId(42), new InitializeTransform.Data
            {
                From = Target.Owner,
                To = Target.None,
                Position = InitializeTransform.Data.PositionInit.From,
                Rotation = InitializeTransform.Data.RotationInit.None,
                Scale = InitializeTransform.Data.ScaleInit.None,
                ApplyInitialTransform = false,
            });

            // Act
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            Assert.AreEqual(originalTransform.Position, resultTransform.Position,
                "Transform should remain unchanged when ObjectId not found");
        }

        [Test]
        public void InitializeTransformSystem_WithoutInitializeTransformSingleton_DoesNotUpdate()
        {
            // Arrange
            this.Manager.DestroyEntity(this.initializeTransformSingleton);

            var objectId = new ObjectId(42);
            var originalTransform = LocalTransform.FromPosition(new float3(5, 10, 15));

            var sourceEntity = this.Manager.CreateEntity(typeof(InitializeEntity), typeof(ObjectId), typeof(Targets), typeof(LocalTransform));

            this.Manager.SetComponentData(sourceEntity, objectId);
            this.Manager.SetComponentData(sourceEntity, new Targets
            {
                Owner = sourceEntity,
                Source = sourceEntity,
                Target = Entity.Null,
            });
            this.Manager.SetComponentData(sourceEntity, originalTransform);

            // Act - system should not run without the required singleton
            this.RunSystemGroup(this.systemGroup);

            // Assert
            var resultTransform = this.Manager.GetComponentData<LocalTransform>(sourceEntity);
            Assert.AreEqual(originalTransform.Position, resultTransform.Position,
                "Transform should remain unchanged when singleton is missing");
        }

        private Entity CreateInitializeTransformEntity(ObjectId objectId, Entity owner = default, Entity source = default, Entity target = default)
        {
            var entity = this.Manager.CreateEntity(typeof(InitializeEntity), typeof(ObjectId), typeof(Targets), typeof(LocalTransform));

            this.Manager.SetComponentData(entity, objectId);
            this.Manager.SetComponentData(entity, new Targets
            {
                Owner = owner == default ? entity : owner,
                Source = source == default ? entity : source,
                Target = target,
            });
            this.Manager.SetComponentData(entity, LocalTransform.Identity);

            return entity;
        }

        private void AddInitializeTransformData(ObjectId objectId, InitializeTransform.Data data)
        {
            var buffer = this.Manager.GetBuffer<InitializeTransform>(this.initializeTransformSingleton).AsMap();
            buffer.Add(objectId, data);
        }

        private InitializeTransform.Data CreateTransformData(
            Target from = Target.Owner,
            Target to = Target.Target,
            InitializeTransform.Data.PositionInit position = InitializeTransform.Data.PositionInit.None,
            InitializeTransform.Data.RotationInit rotation = InitializeTransform.Data.RotationInit.None,
            InitializeTransform.Data.ScaleInit scale = InitializeTransform.Data.ScaleInit.None,
            bool applyInitialTransform = false)
        {
            return new InitializeTransform.Data
            {
                From = from,
                To = to,
                Position = position,
                Rotation = rotation,
                Scale = scale,
                ApplyInitialTransform = applyInitialTransform,
            };
        }
    }
}
