// <copyright file="AudioSourcePoolSyncSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Audio;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using UnityEngine;

    public class AudioSourcePoolSyncSystemTests : ECSTestsFixture
    {
        [Test]
        public void Update_CreatesPoolSingletonWithConfiguredCapacity()
        {
            var configEntity = this.Manager.CreateEntity(typeof(AudioSourcePoolConfig));
            this.Manager.SetComponentData(configEntity, new AudioSourcePoolConfig { LoopedAudioPoolSize = 2, OneShotAudioPoolSize = 3 });
            var system = this.World.CreateSystemManaged<AudioSourcePoolSyncSystem>();

            system.Update();

            var query = this.Manager.CreateEntityQuery(typeof(AudioSourcePool));
            var pool = query.GetSingleton<AudioSourcePool>();
            Assert.AreEqual(7, pool.AudioSources.Length); // 2 music + 2 looped + 3 one-shot
            Assert.AreEqual(2, pool.LoopedStartIndex);
            Assert.AreEqual(4, pool.OneShotStartIndex);
        }

        [Test]
        public void Update_DisablesRequestedAndReturnedPoolEntries()
        {
            var configEntity = this.Manager.CreateEntity(typeof(AudioSourcePoolConfig));
            this.Manager.SetComponentData(configEntity, new AudioSourcePoolConfig { LoopedAudioPoolSize = 1, OneShotAudioPoolSize = 1 });
            var system = this.World.CreateSystemManaged<AudioSourcePoolSyncSystem>();
            system.Update();

            var query = this.Manager.CreateEntityQuery(typeof(AudioSourcePool));
            var pool = query.GetSingleton<AudioSourcePool>();
            var loopedGlobal = pool.LoopedStartIndex;
            var oneShotGlobal = pool.OneShotStartIndex;

            pool.AudioSources[loopedGlobal].AudioSource.Value.enabled = true;
            pool.AudioSources[oneShotGlobal].AudioSource.Value.enabled = true;

            var loopedIndex = pool.LoopedPool.Get();
            pool.LoopedPool.Return(loopedIndex);
            pool.OneShotPool.Get();

            system.Update();

            Assert.IsFalse(pool.AudioSources[loopedGlobal].AudioSource.Value.enabled);
            Assert.IsFalse(pool.AudioSources[oneShotGlobal].AudioSource.Value.enabled);
            Assert.AreEqual(0, pool.LoopedPool.Returned.Count);
            Assert.AreEqual(0, pool.OneShotPool.Requests.Count);
        }

        [Test]
        public void DestroyAllSystems_WithDestroyedPooledSource_DoesNotLogException()
        {
            using var world = new World("AudioSourcePoolSyncSystemTests_DestroyedSource", WorldFlags.Editor);
            var manager = world.EntityManager;

            var configEntity = manager.CreateEntity(typeof(AudioSourcePoolConfig));
            manager.SetComponentData(configEntity, new AudioSourcePoolConfig { LoopedAudioPoolSize = 1, OneShotAudioPoolSize = 1 });

            var system = world.CreateSystemManaged<AudioSourcePoolSyncSystem>();
            system.Update();

            var query = manager.CreateEntityQuery(typeof(AudioSourcePool));
            var pool = query.GetSingleton<AudioSourcePool>();
            var oneShot = pool.AudioSources[pool.OneShotStartIndex].AudioSource.Value;
            Object.DestroyImmediate(oneShot.gameObject);

            world.DestroyAllSystemsAndLogException(out var loggedException);
            Assert.IsFalse(loggedException);
        }

        [Test]
        public void DestroyAllSystems_WithExternalMusicSource_DoesNotDestroyMusicSource()
        {
            var musicGo = new GameObject("AudioSourcePoolSyncSystemTests_ExternalMusic", typeof(MusicSource));
            musicGo.AddComponent<AudioSource>();

            try
            {
                using var world = new World("AudioSourcePoolSyncSystemTests_ExternalMusic", WorldFlags.Editor);
                var manager = world.EntityManager;

                var configEntity = manager.CreateEntity(typeof(AudioSourcePoolConfig));
                manager.SetComponentData(configEntity, new AudioSourcePoolConfig { LoopedAudioPoolSize = 1, OneShotAudioPoolSize = 1 });

                var system = world.CreateSystemManaged<AudioSourcePoolSyncSystem>();
                system.Update();

                world.DestroyAllSystemsAndLogException(out var loggedException);
                Assert.IsFalse(loggedException);

                Assert.IsNotNull(musicGo);
                Assert.IsNotNull(musicGo.GetComponent<MusicSource>());
            }
            finally
            {
                if (musicGo)
                {
                    Object.DestroyImmediate(musicGo);
                }
            }
        }
    }
}
#endif
