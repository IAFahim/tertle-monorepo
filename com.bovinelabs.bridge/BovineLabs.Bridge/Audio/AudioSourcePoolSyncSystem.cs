// <copyright file="AudioSourcePoolSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using System;
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Syncs pooled AudioSource managed objects with entity data.
    /// This managed system handles the actual AudioSource components.
    /// </summary>
    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    public partial class AudioSourcePoolSyncSystem : SystemBase
    {
        private const int MusicSourceCount = 2;
        private static readonly Type[] ComponentTypes =
        {
            typeof(AudioSource),
            typeof(AudioChorusFilter),
            typeof(AudioDistortionFilter),
            typeof(AudioEchoFilter),
            typeof(AudioHighPassFilter),
            typeof(AudioLowPassFilter),
            typeof(AudioReverbFilter),
        };

        private NativeArray<AudioFacade> facades;
        private TrackedIndexPool loopedPool;
        private TrackedIndexPool oneShotPool;
        private NativeArray<long> oneShotOrder;
        private int loopedPoolSize;
        private int oneShotPoolSize;
        private int loopedStartIndex;
        private int oneShotStartIndex;
        private bool ownsMusicSources;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.CheckedStateRef.AddDependency<AudioSourcePool>();
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            this.EntityManager.CreateEntity<AudioSourcePool>("Audio Source Pool");

            var config = SystemAPI.GetSingleton<AudioSourcePoolConfig>();
            this.loopedPoolSize = math.max(1, config.LoopedAudioPoolSize);
            this.oneShotPoolSize = math.max(1, config.OneShotAudioPoolSize);
            this.loopedStartIndex = MusicSourceCount;
            this.oneShotStartIndex = this.loopedStartIndex + this.loopedPoolSize;

            this.loopedPool = new TrackedIndexPool(this.loopedPoolSize);
            this.oneShotPool = new TrackedIndexPool(this.oneShotPoolSize);
            this.oneShotOrder = new NativeArray<long>(this.oneShotPoolSize, Allocator.Persistent);
            this.EnsurePool();
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            this.EntityManager.DestroyEntity(this.EntityManager.GetSingletonEntity<AudioSourcePool>());

            if (this.ownsMusicSources)
            {
                Destroy(this.facades[0]);
            }

            for (var i = MusicSourceCount; i < this.facades.Length; i++)
            {
                Destroy(this.facades[i]);
            }

            this.facades.Dispose();
            this.loopedPool.Dispose();
            this.oneShotPool.Dispose();
            this.oneShotOrder.Dispose();

            this.ownsMusicSources = false;
            return;

            void Destroy(AudioFacade f)
            {
                var source = f.AudioSource.Value;
                if (!source)
                {
                    return;
                }

                var go = source.gameObject;
                if (go)
                {
                    if (this.World.IsEditorWorld())
                    {
                        Object.DestroyImmediate(go);
                    }
                    else
                    {
                        Object.Destroy(go);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            this.CompleteDependency();

#if UNITY_EDITOR
            this.EnsurePool();
#endif

            this.DisablePoolRequests(this.loopedPool, this.loopedStartIndex);
            this.DisablePoolRequests(this.oneShotPool, this.oneShotStartIndex);
        }

        private void EnsurePool()
        {
            if (!this.EnsurePoolCreated())
            {
                return;
            }

            SystemAPI.SetSingleton(new AudioSourcePool
            {
                AudioSources = this.facades.AsReadOnly(),
                LoopedPool = this.loopedPool,
                OneShotPool = this.oneShotPool,
                OneShotOrder = this.oneShotOrder,
                LoopedStartIndex = this.loopedStartIndex,
                OneShotStartIndex = this.oneShotStartIndex,
            });
        }

        private void DisablePoolRequests(TrackedIndexPool pool, int startIndex)
        {
            foreach (var p in pool.Returned)
            {
                DisableAllComponents(this.facades, startIndex + p);
            }

            pool.ClearReturned();

            foreach (var p in pool.Requests)
            {
                DisableAllComponents(this.facades, startIndex + p);
            }

            pool.ClearRequests();
        }

        private bool EnsurePoolCreated()
        {
            if (this.facades.IsCreated && !this.facades[0].AudioSource.Value)
            {
                this.facades.Dispose();
            }

            if (this.facades.IsCreated)
            {
#if UNITY_EDITOR
                if (this.facades[0].AudioSource.Value.gameObject.scene == default)
                {
                    foreach (var f in this.facades)
                    {
                        SceneManager.MoveGameObjectToScene(f.AudioSource.Value.gameObject, SceneManager.GetActiveScene());
                    }
                }
#endif

                return false;
            }

            var totalSize = MusicSourceCount + this.loopedPoolSize + this.oneShotPoolSize;
            this.facades = new NativeArray<AudioFacade>(totalSize, Allocator.Persistent);

            var isEditor = this.World.IsEditorWorld();

            this.CreateMusicSources(isEditor);

            for (var i = 0; i < this.loopedPoolSize; i++)
            {
                var index = this.loopedStartIndex + i;
                CreateSource(ref this.facades, index, "AudioSourceAmbient", isEditor, true);
            }

            for (var i = 0; i < this.oneShotPoolSize; i++)
            {
                var index = this.oneShotStartIndex + i;
                CreateSource(ref this.facades, index, "AudioSourceOneShot", isEditor, false);
            }

            return true;
        }

        private static void DisableAllComponents(in NativeArray<AudioFacade> facades, int index)
        {
            var facade = facades[index];
            facade.AudioSource.Value.enabled = false;
            facade.AudioLowPassFilter.Value.enabled = false;
            facade.AudioHighPassFilter.Value.enabled = false;
            facade.AudioDistortionFilter.Value.enabled = false;
            facade.AudioEchoFilter.Value.enabled = false;
            facade.AudioReverbFilter.Value.enabled = false;
            facade.AudioChorusFilter.Value.enabled = false;
        }

        private static void CreateSource(
            ref NativeArray<AudioFacade> facades, int index, string goName, bool isEditor, bool looped, bool playOnAwake = true)
        {
            var go = new GameObject($"{goName}{index}", ComponentTypes);
            var facade = CreateFacade(go);

            facade.AudioSource.Value.playOnAwake = playOnAwake;
            facade.AudioSource.Value.loop = looped;
            SetFlags(go, isEditor);
            facades[index] = facade;
        }

        private static void SetFlags(GameObject go, bool isEditor)
        {
#if UNITY_EDITOR
            go.hideFlags = BridgeObjectConfig.Flags;

            if (!isEditor)
#endif
            {
                Object.DontDestroyOnLoad(go);
            }
        }

        private void CreateMusicSources(bool isEditor)
        {
            this.ownsMusicSources = false;

            // Always spawn custom for editor world
            if (isEditor || !TryGetMusicSources(out var source0, out var source1))
            {
                var go = new GameObject("AudioSourceMusic");
                SetFlags(go, isEditor);

                source0 = go.AddComponent<AudioSource>();
                source1 = go.AddComponent<AudioSource>();

                this.ownsMusicSources = true;
            }

            this.SetupMusicSource(source0);
            this.SetupMusicSource(source1);
            this.facades[0] = new AudioFacade { AudioSource = source0 };
            this.facades[1] = new AudioFacade { AudioSource = source1 };
        }

        private static bool TryGetMusicSources(out AudioSource source0, out AudioSource source1)
        {
            source0 = null;
            source1 = null;

            var musicSource = Object.FindAnyObjectByType<MusicSource>();
            if (musicSource == null)
            {
                return false;
            }

            var audioSources = musicSource.GetComponents<AudioSource>();

            if (audioSources.Length < 2)
            {
                BLGlobalLogger.LogErrorString("MusicSource needs 2 audio sources");
                return false;
            }
            else if (audioSources.Length > 2)
            {
                BLGlobalLogger.LogWarningString("MusicSource has more than 2 audio sources; only the first two will be used.");
            }

            source0 = audioSources[0];
            source1 = audioSources[1];

            return true;
        }

        private void SetupMusicSource(AudioSource source)
        {
            source.playOnAwake = true;
            source.loop = true;
            source.spatialBlend = 0;
            source.priority = 0; // don't want music interrupted
        }

        private static AudioFacade CreateFacade(GameObject go)
        {
            return new AudioFacade
            {
                AudioSource = GetDisable<AudioSource>(go),
                AudioChorusFilter = GetDisable<AudioChorusFilter>(go),
                AudioDistortionFilter = GetDisable<AudioDistortionFilter>(go),
                AudioEchoFilter = GetDisable<AudioEchoFilter>(go),
                AudioHighPassFilter = GetDisable<AudioHighPassFilter>(go),
                AudioLowPassFilter = GetDisable<AudioLowPassFilter>(go),
                AudioReverbFilter = GetDisable<AudioReverbFilter>(go),
            };

            static T GetDisable<T>(GameObject go)
                where T : Behaviour
            {
                var comp = go.GetComponent<T>();
                comp.enabled = false;
                return comp;
            }
        }
    }
}
#endif
