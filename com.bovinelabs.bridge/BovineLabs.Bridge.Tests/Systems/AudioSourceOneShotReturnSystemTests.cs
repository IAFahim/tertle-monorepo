// <copyright file="AudioSourceOneShotReturnSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Audio;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    public class AudioSourceOneShotReturnSystemTests : ECSTestsFixture
    {
        private AudioSourceOneShotReturnSystem system;
        private NativeArray<AudioFacade> facades;
        private TrackedIndexPool loopedPool;
        private TrackedIndexPool oneShotPool;
        private NativeArray<long> order;
        private GameObject sourceObject;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystemManaged<AudioSourceOneShotReturnSystem>();
        }

        public override void TearDown()
        {
            if (this.facades.IsCreated)
            {
                this.facades.Dispose();
            }

            if (this.order.IsCreated)
            {
                this.order.Dispose();
            }

            if (this.loopedPool.IsCreated)
            {
                this.loopedPool.Dispose();
            }

            if (this.oneShotPool.IsCreated)
            {
                this.oneShotPool.Dispose();
            }

            if (this.sourceObject != null)
            {
                Object.DestroyImmediate(this.sourceObject);
            }

            base.TearDown();
        }

        [Test]
        public void Update_ReturnsFinishedOneShotIndices()
        {
            this.sourceObject = new GameObject("AudioSourceOneShotReturnSystemTests", typeof(AudioSource));

            this.facades = new NativeArray<AudioFacade>(1, Allocator.Persistent);
            this.facades[0] = new AudioFacade { AudioSource = this.sourceObject.GetComponent<AudioSource>() };

            this.loopedPool = new TrackedIndexPool(1);
            this.oneShotPool = new TrackedIndexPool(1);
            this.order = new NativeArray<long>(1, Allocator.Persistent);

            Assert.AreEqual(0, this.oneShotPool.Get());
            this.order[0] = 123;

            var poolEntity = this.Manager.CreateEntity(typeof(AudioSourcePool));
            this.Manager.SetComponentData(poolEntity, new AudioSourcePool
            {
                AudioSources = this.facades.AsReadOnly(),
                LoopedPool = this.loopedPool,
                OneShotPool = this.oneShotPool,
                OneShotOrder = this.order,
                LoopedStartIndex = 0,
                OneShotStartIndex = 0,
            });

            this.system.Update();

            Assert.AreEqual(0, this.order[0]);
            Assert.IsTrue(this.oneShotPool.Returned.Contains(0));
        }
    }
}
#endif
