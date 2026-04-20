// <copyright file="AudioSyncSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Audio;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    public class AudioSyncSystemTests : ECSTestsFixture
    {
        private SystemHandle system;
        private NativeArray<AudioFacade> facades;
        private TrackedIndexPool loopedPool;
        private TrackedIndexPool oneShotPool;
        private NativeArray<long> oneShotOrder;
        private GameObject sourceObject;
        private AudioClip sourceClip;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<AudioSyncSystem>();
        }

        public override void TearDown()
        {
            if (this.facades.IsCreated)
            {
                this.facades.Dispose();
            }

            if (this.oneShotOrder.IsCreated)
            {
                this.oneShotOrder.Dispose();
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

            if (this.sourceClip != null)
            {
                Object.DestroyImmediate(this.sourceClip);
            }

            base.TearDown();
        }

        [Test]
        public void Update_AppliesAudioSourceAndFilterDataToFacade()
        {
            this.CreateSourceAndPool();

            var entity = this.Manager.CreateEntity(
                typeof(AudioSourceIndex),
                typeof(AudioSourceData),
                typeof(GlobalVolume),
                typeof(AudioLowPassFilterData));
            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = 0 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            this.Manager.SetComponentData(entity, new AudioSourceData { Volume = 0.8f, Pitch = 1.1f });
            this.Manager.SetComponentData(entity, new GlobalVolume { Volume = 0.5f });
            this.Manager.SetComponentData(entity, new AudioLowPassFilterData { CutoffFrequency = 500f, LowpassResonanceQ = 1.25f });

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var source = this.sourceObject.GetComponent<AudioSource>();
            var lowPass = this.sourceObject.GetComponent<AudioLowPassFilter>();

            Assert.That(source.volume, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(source.pitch, Is.EqualTo(1.1f).Within(0.001f));
            Assert.IsTrue(lowPass.enabled);
            Assert.That(lowPass.cutoffFrequency, Is.EqualTo(500f).Within(0.001f));
            Assert.That(lowPass.lowpassResonanceQ, Is.EqualTo(1.25f).Within(0.001f));
        }

        [Test]
        public void Update_AppliesAudioSourceDataExtendedToFacade()
        {
            this.CreateSourceAndPool();
            this.sourceClip = AudioClip.Create("AudioSyncSystemTests_Clip", 256, 1, 48000, false);

            var entity = this.Manager.CreateEntity(
                typeof(AudioSourceIndex),
                typeof(AudioSourceData),
                typeof(GlobalVolume),
                typeof(AudioSourceDataExtended));

            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = 0 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            this.Manager.SetComponentData(entity, new AudioSourceData { Volume = 2f, Pitch = 0.75f });
            this.Manager.SetComponentData(entity, new GlobalVolume { Volume = 0.75f });
            this.Manager.SetComponentData(entity, new AudioSourceDataExtended
            {
                Clip = this.sourceClip,
                PanStereo = 0.2f,
                SpatialBlend = 0.8f,
                MinDistance = 2f,
                MaxDistance = 30f,
                DopplerLevel = 0.6f,
                Spread = 40f,
                RolloffMode = AudioRolloffMode.Linear,
                Priority = 32,
                ReverbZoneMix = 0.5f,
            });

            this.UpdateSystem();

            var source = this.sourceObject.GetComponent<AudioSource>();

            Assert.That(source.volume, Is.EqualTo(1f).Within(0.001f));
            Assert.That(source.pitch, Is.EqualTo(0.75f).Within(0.001f));
            Assert.AreEqual(this.sourceClip, source.clip);
            Assert.That(source.panStereo, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(source.spatialBlend, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(source.minDistance, Is.EqualTo(2f).Within(0.001f));
            Assert.That(source.maxDistance, Is.EqualTo(30f).Within(0.001f));
            Assert.That(source.dopplerLevel, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(source.spread, Is.EqualTo(40f).Within(0.001f));
            Assert.AreEqual(AudioRolloffMode.Linear, source.rolloffMode);
            Assert.AreEqual(32, source.priority);
            Assert.That(source.reverbZoneMix, Is.EqualTo(0.5f).Within(0.001f));
            Assert.IsTrue(source.enabled);
        }

        [Test]
        public void Update_AppliesHighPassDistortionAndEchoFiltersToFacade()
        {
            this.CreateSourceAndPool();

            var entity = this.Manager.CreateEntity(
                typeof(AudioSourceIndex),
                typeof(AudioHighPassFilterData),
                typeof(AudioDistortionFilterData),
                typeof(AudioEchoFilterData));

            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = 0 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            this.Manager.SetComponentData(entity, new AudioHighPassFilterData
            {
                CutoffFrequency = 1200f,
                HighpassResonanceQ = 1.8f,
            });
            this.Manager.SetComponentData(entity, new AudioDistortionFilterData
            {
                DistortionLevel = 0.4f,
            });
            this.Manager.SetComponentData(entity, new AudioEchoFilterData
            {
                Delay = 60f,
                DecayRatio = 0.5f,
                WetMix = 0.7f,
                DryMix = 0.3f,
            });

            this.UpdateSystem();

            var highPass = this.sourceObject.GetComponent<AudioHighPassFilter>();
            var distortion = this.sourceObject.GetComponent<AudioDistortionFilter>();
            var echo = this.sourceObject.GetComponent<AudioEchoFilter>();

            Assert.IsTrue(highPass.enabled);
            Assert.That(highPass.cutoffFrequency, Is.EqualTo(1200f).Within(0.001f));
            Assert.That(highPass.highpassResonanceQ, Is.EqualTo(1.8f).Within(0.001f));

            Assert.IsTrue(distortion.enabled);
            Assert.That(distortion.distortionLevel, Is.EqualTo(0.4f).Within(0.001f));

            Assert.IsTrue(echo.enabled);
            Assert.That(echo.delay, Is.EqualTo(60f).Within(0.001f));
            Assert.That(echo.decayRatio, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(echo.wetMix, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(echo.dryMix, Is.EqualTo(0.3f).Within(0.001f));
        }

        [Test]
        public void Update_AppliesReverbFilterToFacade()
        {
            this.CreateSourceAndPool();

            var entity = this.Manager.CreateEntity(
                typeof(AudioSourceIndex),
                typeof(AudioReverbFilterData));

            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = 0 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            this.Manager.SetComponentData(entity, new AudioReverbFilterData
            {
                ReverbPreset = AudioReverbPreset.User,
                DryLevel = -500f,
                Room = -800f,
                RoomHF = -300f,
                RoomLF = -200f,
                DecayTime = 1.2f,
                DecayHFRatio = 0.7f,
                ReflectionsLevel = -1000f,
                ReflectionsDelay = 0.02f,
                ReverbLevel = -200f,
                ReverbDelay = 0.03f,
                HFReference = 5000f,
                LFReference = 250f,
                Diffusion = 80f,
                Density = 90f,
            });

            this.UpdateSystem();

            var reverb = this.sourceObject.GetComponent<AudioReverbFilter>();

            Assert.IsTrue(reverb.enabled);
            Assert.AreEqual(AudioReverbPreset.User, reverb.reverbPreset);
            Assert.That(reverb.dryLevel, Is.EqualTo(-500f).Within(0.001f));
            Assert.That(reverb.reverbLevel, Is.EqualTo(-200f).Within(0.001f));
            Assert.That(reverb.hfReference, Is.EqualTo(5000f).Within(0.001f));
            Assert.That(reverb.diffusion, Is.EqualTo(80f).Within(0.001f));
        }

        [Test]
        public void Update_AppliesChorusFilterToFacade()
        {
            this.CreateSourceAndPool();

            var entity = this.Manager.CreateEntity(
                typeof(AudioSourceIndex),
                typeof(AudioChorusFilterData));

            this.Manager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = 0 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            this.Manager.SetComponentData(entity, new AudioChorusFilterData
            {
                DryMix = 0.2f,
                WetMix1 = 0.4f,
                WetMix2 = 0.5f,
                WetMix3 = 0.6f,
                Delay = 30f,
                Rate = 1.5f,
                Depth = 0.9f,
            });

            this.UpdateSystem();

            var chorus = this.sourceObject.GetComponent<AudioChorusFilter>();

            Assert.IsTrue(chorus.enabled);
            Assert.That(chorus.dryMix, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(chorus.wetMix1, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(chorus.wetMix2, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(chorus.wetMix3, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(chorus.delay, Is.EqualTo(30f).Within(0.001f));
            Assert.That(chorus.rate, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(chorus.depth, Is.EqualTo(0.9f).Within(0.001f));
        }

        private void CreateSourceAndPool()
        {
            this.sourceObject = new GameObject(
                "AudioSyncSystemTests_Source",
                typeof(AudioSource),
                typeof(AudioLowPassFilter),
                typeof(AudioHighPassFilter),
                typeof(AudioDistortionFilter),
                typeof(AudioEchoFilter),
                typeof(AudioReverbFilter),
                typeof(AudioChorusFilter));

            this.facades = new NativeArray<AudioFacade>(1, Allocator.Persistent);
            this.facades[0] = new AudioFacade
            {
                AudioSource = this.sourceObject.GetComponent<AudioSource>(),
                AudioLowPassFilter = this.sourceObject.GetComponent<AudioLowPassFilter>(),
                AudioHighPassFilter = this.sourceObject.GetComponent<AudioHighPassFilter>(),
                AudioDistortionFilter = this.sourceObject.GetComponent<AudioDistortionFilter>(),
                AudioEchoFilter = this.sourceObject.GetComponent<AudioEchoFilter>(),
                AudioReverbFilter = this.sourceObject.GetComponent<AudioReverbFilter>(),
                AudioChorusFilter = this.sourceObject.GetComponent<AudioChorusFilter>(),
            };

            this.loopedPool = new TrackedIndexPool(1);
            this.oneShotPool = new TrackedIndexPool(1);
            this.oneShotOrder = new NativeArray<long>(1, Allocator.Persistent);

            var poolEntity = this.Manager.CreateEntity(typeof(AudioSourcePool));
            this.Manager.SetComponentData(poolEntity, new AudioSourcePool
            {
                AudioSources = this.facades.AsReadOnly(),
                LoopedPool = this.loopedPool,
                OneShotPool = this.oneShotPool,
                OneShotOrder = this.oneShotOrder,
                LoopedStartIndex = 0,
                OneShotStartIndex = 0,
            });
        }

        private void UpdateSystem()
        {
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }
    }
}
#endif
