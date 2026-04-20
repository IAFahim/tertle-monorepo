// <copyright file="AudioVolumeSystemTests.cs" company="BovineLabs">
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

    public class AudioVolumeSystemTests : ECSTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<AudioVolumeSystem>();

            AudioVolumeData.MusicVolume.Data = 1f;
            AudioVolumeData.EffectVolume.Data = 1f;
            AudioVolumeData.AmbianceVolume.Data = 1f;
        }

        public override void TearDown()
        {
            AudioVolumeData.MusicVolume.Data = 1f;
            AudioVolumeData.EffectVolume.Data = 1f;
            AudioVolumeData.AmbianceVolume.Data = 1f;
            base.TearDown();
        }

        [Test]
        public void Update_AppliesPerCategoryVolumes()
        {
            var music = this.CreateMusicEntity();
            var oneShot = this.CreateOneShotEntity();
            var ambiance = this.CreateAmbianceEntity();

            AudioVolumeData.MusicVolume.Data = 0.2f;
            AudioVolumeData.EffectVolume.Data = 0.4f;
            AudioVolumeData.AmbianceVolume.Data = 0.7f;

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.That(this.Manager.GetComponentData<GlobalVolume>(music).Volume, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(this.Manager.GetComponentData<GlobalVolume>(oneShot).Volume, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(this.Manager.GetComponentData<GlobalVolume>(ambiance).Volume, Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void Update_SaturatesSharedVolumeValues()
        {
            var music = this.CreateMusicEntity();
            var oneShot = this.CreateOneShotEntity();
            var ambiance = this.CreateAmbianceEntity();

            AudioVolumeData.MusicVolume.Data = -1f;
            AudioVolumeData.EffectVolume.Data = 2f;
            AudioVolumeData.AmbianceVolume.Data = 10f;

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.That(this.Manager.GetComponentData<GlobalVolume>(music).Volume, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(this.Manager.GetComponentData<GlobalVolume>(oneShot).Volume, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(this.Manager.GetComponentData<GlobalVolume>(ambiance).Volume, Is.EqualTo(1f).Within(0.0001f));
        }

        private Entity CreateMusicEntity()
        {
            var entity = this.Manager.CreateEntity(typeof(GlobalVolume), typeof(AudioSourceIndex), typeof(AudioSourceMusic));
            this.Manager.SetComponentData(entity, new GlobalVolume { Volume = 1f });
            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = 0 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            return entity;
        }

        private Entity CreateOneShotEntity()
        {
            var entity = this.Manager.CreateEntity(typeof(GlobalVolume), typeof(AudioSourceIndex), typeof(AudioSourceOneShot));
            this.Manager.SetComponentData(entity, new GlobalVolume { Volume = 1f });
            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = 1 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            return entity;
        }

        private Entity CreateAmbianceEntity()
        {
            var entity = this.Manager.CreateEntity(typeof(GlobalVolume), typeof(AudioSourceIndex));
            this.Manager.SetComponentData(entity, new GlobalVolume { Volume = 1f });
            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = 2 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            return entity;
        }
    }
}
#endif
