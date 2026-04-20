// <copyright file="AudioSourceMusicSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Audio;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Core;
    using Unity.Entities;
    using UnityEngine;

    public class AudioSourceMusicSystemTests : ECSTestsFixture
    {
        private SystemHandle system;
        private AudioClip testClip;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<AudioSourceMusicSystem>();
            this.testClip = AudioClip.Create("AudioSourceMusicSystemTests", 128, 1, 44100, false);
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(this.testClip);
            base.TearDown();
        }

        [Test]
        public void Update_WithTrackSelection_StartsFade()
        {
            var controller = this.CreateController(trackId: 1, blendSeconds: 1f, trackVolume: 0.5f);
            this.World.SetTime(new TimeData(0.1, 0.1f));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var state = this.Manager.GetComponentData<MusicState>(controller);
            Assert.AreEqual(2, this.Manager.CreateEntityQuery(typeof(AudioSourceMusic)).CalculateEntityCount());
            Assert.IsTrue(state.IsFading);
            Assert.AreEqual(1, state.TargetTrackId);
        }

        [Test]
        public void Update_WhenBlendCompletes_SetsActiveTrackAndClearsFadeState()
        {
            var controller = this.CreateController(trackId: 1, blendSeconds: 0.1f, trackVolume: 0.7f);
            this.World.SetTime(new TimeData(1.0, 1.0f));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var state = this.Manager.GetComponentData<MusicState>(controller);
            Assert.IsFalse(state.IsFading);
            Assert.AreEqual(1, state.ActiveTrackId);
            Assert.AreEqual(1, state.ActiveSlot);
            Assert.AreEqual(0, state.TargetTrackId);
        }

        private Entity CreateController(int trackId, float blendSeconds, float trackVolume)
        {
            var entity = this.Manager.CreateEntity(typeof(MusicState), typeof(MusicSelection), typeof(MusicBlendConfig));
            this.Manager.SetComponentData(entity, new MusicSelection { TrackId = trackId });
            this.Manager.SetComponentData(entity, new MusicBlendConfig { DefaultBlendSeconds = blendSeconds });

            var tracks = this.Manager.AddBuffer<MusicTrackEntry>(entity);
            tracks.Add(default);
            tracks.Add(new MusicTrackEntry
            {
                Clip = this.testClip,
                BaseVolume = trackVolume,
                BlendOverrideSeconds = 0f,
            });

            return entity;
        }
    }
}
#endif
