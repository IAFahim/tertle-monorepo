// <copyright file="AudioSourcePoolSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Bridge.Util;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Syncs pooled AudioSource managed objects with entity data.
    /// This managed system handles the actual AudioSource components.
    /// </summary>
    [UpdateInGroup(typeof(BridgeSystemGroup))]
    public partial class AudioSourcePoolSyncSystem : SystemBase
    {
        private const HideFlags HideFlags = UnityEngine.HideFlags.HideAndDontSave;

        private GameObject poolContainer;
        private NativeArray<AudioFacade> facades;
        private TrackedIndexPool pool;
        private int poolSize;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.EnsurePoolContainer();

            this.CheckedStateRef.AddDependency<AudioSourcePool>();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            if (!this.poolContainer)
            {
                return;
            }

            if (!this.World.IsEditorWorld())
            {
                Object.Destroy(this.poolContainer);
            }
            else
            {
                Object.DestroyImmediate(this.poolContainer);
            }
        }

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            this.EntityManager.CreateSingleton<AudioSourcePool>();

            this.poolSize = SystemAPI.GetSingleton<AudioSourcePoolConfig>().LoopedAudioPoolSize;
            this.pool = new TrackedIndexPool(this.poolSize);
            this.EnsurePool();
        }

        protected override void OnStopRunning()
        {
            this.EntityManager.DestroyEntity(this.EntityManager.GetSingletonEntity<AudioSourcePool>());

            this.facades.Dispose();
            this.pool.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            this.CompleteDependency();

#if UNITY_EDITOR
            this.EnsurePool();
#endif

            foreach (var p in this.pool.Returned)
            {
                this.DisableAllComponents(p);
            }

            this.pool.ClearReturned();

            foreach (var p in this.pool.Requests)
            {
                this.DisableAllComponents(p);
            }

            this.pool.ClearRequests();
        }

        private void DisableAllComponents(int index)
        {
            var facade = this.facades[index];
            facade.AudioSource.Value.enabled = false;
            facade.AudioLowPassFilter.Value.enabled = false;
            facade.AudioHighPassFilter.Value.enabled = false;
            facade.AudioDistortionFilter.Value.enabled = false;
            facade.AudioEchoFilter.Value.enabled = false;
            facade.AudioReverbFilter.Value.enabled = false;
            facade.AudioChorusFilter.Value.enabled = false;
        }

        private void EnsurePoolContainer()
        {
            if (this.poolContainer)
            {
                return;
            }

            this.poolContainer = new GameObject("AudioSourcePool");

            if (this.World.IsEditorWorld())
            {
                this.poolContainer.hideFlags = HideFlags;
            }
            else
            {
                Object.DontDestroyOnLoad(this.poolContainer);
            }
        }

        private void EnsurePool()
        {
            // Setup this way for editor
            if (!this.poolContainer && this.facades.IsCreated)
            {
                this.facades.Dispose();
            }

            // Already setup
            if (this.facades.IsCreated)
            {
#if UNITY_EDITOR
                if (this.poolContainer.scene == default)
                {
                    SceneManager.MoveGameObjectToScene(this.poolContainer, SceneManager.GetActiveScene());
                }
#endif

                return;
            }

            this.facades = new NativeArray<AudioFacade>(this.poolSize, Allocator.Persistent);

            SystemAPI.SetSingleton(new AudioSourcePool
            {
                AudioSources = this.facades.AsReadOnly(),
                Pool = this.pool,
            });

            var types = new[]
            {
                typeof(AudioSource),
                typeof(AudioChorusFilter),
                typeof(AudioDistortionFilter),
                typeof(AudioEchoFilter),
                typeof(AudioHighPassFilter),
                typeof(AudioLowPassFilter),
                typeof(AudioReverbFilter),
            };

            this.EnsurePoolContainer();

            var isEditor = this.World.IsEditorWorld();

            for (var i = 0; i < this.facades.Length; i++)
            {
                var go = new GameObject($"AudioSource{i}", types);

                var facade = new AudioFacade
                {
                    AudioSource = GetDisable<AudioSource>(),
                    AudioChorusFilter = GetDisable<AudioChorusFilter>(),
                    AudioDistortionFilter = GetDisable<AudioDistortionFilter>(),
                    AudioEchoFilter = GetDisable<AudioEchoFilter>(),
                    AudioHighPassFilter = GetDisable<AudioHighPassFilter>(),
                    AudioLowPassFilter = GetDisable<AudioLowPassFilter>(),
                    AudioReverbFilter = GetDisable<AudioReverbFilter>(),
                };

                facade.AudioSource.Value.playOnAwake = true;
                facade.AudioSource.Value.loop = true;
                go.transform.SetParent(this.poolContainer.transform);
                if (isEditor)
                {
                    go.hideFlags = HideFlags;
                }

                this.facades[i] = facade;

                continue;

                T GetDisable<T>()
                    where T : Behaviour
                {
                    var comp = go.GetComponent<T>();
                    comp.enabled = false;
                    return comp;
                }
            }
        }
    }
}
