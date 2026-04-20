// <copyright file="AudioSourceAmbiancePoolSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Audio;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    public class AudioSourceAmbiancePoolSystemTests : ECSTestsFixture
    {
        private SystemHandle system;
        private TrackedIndexPool loopedPool;
        private TrackedIndexPool oneShotPool;
        private NativeArray<long> oneShotOrder;
        private Entity poolEntity;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<AudioSourceAmbiancePoolSystem>();
            this.CreateListener(float3.zero);
        }

        public override void TearDown()
        {
            if (this.oneShotOrder.IsCreated)
            {
                this.oneShotOrder.Dispose();
            }

            if (this.oneShotPool.IsCreated)
            {
                this.oneShotPool.Dispose();
            }

            if (this.loopedPool.IsCreated)
            {
                this.loopedPool.Dispose();
            }

            base.TearDown();
        }

        [Test]
        public void Update_DisabledPreviouslyActive_ReturnsIndexAndDisablesComponent()
        {
            this.CreatePool(loopedLength: 2);
            var pool = this.Manager.GetComponentData<AudioSourcePool>(this.poolEntity);
            var reservedIndex = pool.LoopedPool.Get();
            pool.LoopedPool.ClearRequests();

            var entity = this.Manager.CreateEntity(
                typeof(AudioSourceIndex),
                typeof(AudioSourceEnabled),
                typeof(AudioSourceEnabledPrevious));
            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = reservedIndex });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            this.Manager.SetComponentEnabled<AudioSourceEnabled>(entity, false);

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(-1, this.Manager.GetComponentData<AudioSourceIndex>(entity).PoolIndex);
            Assert.IsFalse(this.Manager.IsComponentEnabled<AudioSourceIndex>(entity));
            Assert.IsTrue(pool.LoopedPool.Returned.Contains(reservedIndex));
        }

        [Test]
        public void Update_ExistingActiveEntity_RetainsPoolIndex()
        {
            this.CreatePool(loopedLength: 1);
            var pool = this.Manager.GetComponentData<AudioSourcePool>(this.poolEntity);
            var reservedIndex = pool.LoopedPool.Get();
            pool.LoopedPool.ClearRequests();

            var entity = this.CreateAmbianceEntity(new float3(1f, 0f, 0f), reservedIndex, true);

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.AreEqual(reservedIndex, this.Manager.GetComponentData<AudioSourceIndex>(entity).PoolIndex);
            Assert.IsTrue(this.Manager.IsComponentEnabled<AudioSourceIndex>(entity));
        }

        [Test]
        public void Update_NewClosestEntity_GetsAssignedPoolIndex()
        {
            this.CreatePool(loopedLength: 1);
            var entity = this.CreateAmbianceEntity(new float3(2f, 0f, 0f), -1, false);

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.IsComponentEnabled<AudioSourceIndex>(entity));
            Assert.AreEqual(0, this.Manager.GetComponentData<AudioSourceIndex>(entity).PoolIndex);
        }

        private void CreatePool(int loopedLength)
        {
            this.loopedPool = new TrackedIndexPool(loopedLength);
            this.oneShotPool = new TrackedIndexPool(1);
            this.oneShotOrder = new NativeArray<long>(1, Allocator.Persistent);

            this.poolEntity = this.Manager.CreateEntity(typeof(AudioSourcePool));
            this.Manager.SetComponentData(this.poolEntity, new AudioSourcePool
            {
                AudioSources = default,
                LoopedPool = this.loopedPool,
                OneShotPool = this.oneShotPool,
                OneShotOrder = this.oneShotOrder,
                LoopedStartIndex = 0,
                OneShotStartIndex = 0,
            });
        }

        private void CreateListener(float3 position)
        {
            var listener = this.Manager.CreateEntity(typeof(CameraMain), typeof(LocalTransform));
            this.Manager.SetComponentData(listener, LocalTransform.FromPosition(position));
        }

        private Entity CreateAmbianceEntity(float3 position, int poolIndex, bool indexEnabled)
        {
            var entity = this.Manager.CreateEntity(
                typeof(AudioSourceData),
                typeof(AudioSourceDataExtended),
                typeof(AudioSourceAudibleRange),
                typeof(AudioSourceEnabled),
                typeof(LocalToWorld),
                typeof(AudioSourceIndex));

            this.Manager.SetComponentData(entity, new AudioSourceData { Volume = 1f, Pitch = 1f });
            this.Manager.SetComponentData(entity, new AudioSourceDataExtended { Priority = 8, MaxDistance = 16f });
            this.Manager.SetComponentData(entity, new AudioSourceAudibleRange { MaxDistance = 64f });
            this.Manager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = poolIndex });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, indexEnabled);
            this.Manager.SetComponentEnabled<AudioSourceEnabled>(entity, true);
            return entity;
        }
    }
}
#endif
