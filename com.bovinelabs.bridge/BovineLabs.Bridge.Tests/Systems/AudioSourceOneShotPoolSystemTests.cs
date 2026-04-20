// <copyright file="AudioSourceOneShotPoolSystemTests.cs" company="BovineLabs">
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

    public class AudioSourceOneShotPoolSystemTests : ECSTestsFixture
    {
        private SystemHandle system;
        private Entity poolEntity;
        private TrackedIndexPool loopedPool;
        private TrackedIndexPool oneShotPool;
        private NativeArray<long> oneShotOrder;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<AudioSourceOneShotPoolSystem>();
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
        public void Update_WithoutPoolOrListener_DoesNotMutateIndices()
        {
            var entity = this.CreateOneShotEntity(new float3(1f, 0f, 0f));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsFalse(this.Manager.IsComponentEnabled<AudioSourceIndex>(entity));
            Assert.AreEqual(-1, this.Manager.GetComponentData<AudioSourceIndex>(entity).PoolIndex);
        }

        [Test]
        public void Update_AssignsClosestAndDisablesNonSelected()
        {
            this.CreatePool(loopedLength: 1, oneShotLength: 1, oneShotStartIndex: 3);
            this.CreateListener(float3.zero);

            var close = this.CreateOneShotEntity(new float3(1f, 0f, 0f));
            var far = this.CreateOneShotEntity(new float3(20f, 0f, 0f));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.IsComponentEnabled<AudioSourceIndex>(close));
            Assert.AreEqual(3, this.Manager.GetComponentData<AudioSourceIndex>(close).PoolIndex);
            Assert.IsFalse(this.Manager.IsComponentEnabled<AudioSourceIndex>(far));
            Assert.IsFalse(this.Manager.IsComponentEnabled<AudioSourceEnabled>(far));
        }

        [Test]
        public void Update_WhenPoolExhausted_ReusesOldestIndex()
        {
            this.CreatePool(loopedLength: 1, oneShotLength: 1, oneShotStartIndex: 5);
            this.CreateListener(float3.zero);

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var pool = this.Manager.GetComponentData<AudioSourcePool>(this.poolEntity);
            Assert.AreEqual(0, pool.OneShotPool.Get());
            pool.OneShotOrder[0] = 1;

            var entity = this.CreateOneShotEntity(new float3(0.5f, 0f, 0f));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.IsComponentEnabled<AudioSourceIndex>(entity));
            Assert.AreEqual(5, this.Manager.GetComponentData<AudioSourceIndex>(entity).PoolIndex);
            Assert.AreEqual(2, pool.OneShotOrder[0]);
        }

        private void CreatePool(int loopedLength, int oneShotLength, int oneShotStartIndex)
        {
            this.loopedPool = new TrackedIndexPool(loopedLength);
            this.oneShotPool = new TrackedIndexPool(oneShotLength);
            this.oneShotOrder = new NativeArray<long>(oneShotLength, Allocator.Persistent);

            this.poolEntity = this.Manager.CreateEntity(typeof(AudioSourcePool));
            this.Manager.SetComponentData(this.poolEntity, new AudioSourcePool
            {
                AudioSources = default,
                LoopedPool = this.loopedPool,
                OneShotPool = this.oneShotPool,
                OneShotOrder = this.oneShotOrder,
                LoopedStartIndex = 0,
                OneShotStartIndex = oneShotStartIndex,
            });
        }

        private void CreateListener(float3 position)
        {
            var entity = this.Manager.CreateEntity(typeof(CameraMain), typeof(LocalTransform));
            this.Manager.SetComponentData(entity, LocalTransform.FromPosition(position));
        }

        private Entity CreateOneShotEntity(float3 position)
        {
            var entity = this.Manager.CreateEntity(
                typeof(AudioSourceOneShot),
                typeof(AudioSourceDataExtended),
                typeof(AudioSourceAudibleRange),
                typeof(AudioSourceEnabled),
                typeof(LocalToWorld),
                typeof(AudioSourceIndex));

            this.Manager.SetComponentData(entity, new AudioSourceDataExtended { Priority = 10 });
            this.Manager.SetComponentData(entity, new AudioSourceAudibleRange { MaxDistance = 64f });
            this.Manager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(position) });
            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = -1 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, false);
            this.Manager.SetComponentEnabled<AudioSourceEnabled>(entity, true);
            return entity;
        }
    }
}
#endif
