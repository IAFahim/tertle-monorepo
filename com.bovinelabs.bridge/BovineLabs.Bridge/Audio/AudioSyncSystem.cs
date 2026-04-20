// <copyright file="AudioSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    [UpdateAfter(typeof(AudioSourcePoolSyncSystem))]
    public partial struct AudioSyncSystem : ISystem
    {
        static unsafe AudioSyncSystem()
        {
            Burst.AudioSource.Data = new BurstTrampoline(&AudioSourceChangedPacked);
            Burst.AudioSourceDataExtended.Data = new BurstTrampoline(&AudioSourceDataExtendedChangedPacked);
            Burst.AudioLowPassFilterData.Data = new BurstTrampoline(&AudioLowPassFilterDataChangedPacked);
            Burst.AudioHighPassFilterData.Data = new BurstTrampoline(&AudioHighPassFilterDataChangedPacked);
            Burst.AudioDistortionFilterData.Data = new BurstTrampoline(&AudioDistortionFilterDataChangedPacked);
            Burst.AudioEchoFilterData.Data = new BurstTrampoline(&AudioEchoFilterDataChangedPacked);
            Burst.AudioReverbFilterData.Data = new BurstTrampoline(&AudioReverbFilterDataChangedPacked);
            Burst.AudioChorusFilterData.Data = new BurstTrampoline(&AudioChorusFilterDataChangedPacked);
            // Burst.AudioMixerSnapshotData.Data = new BurstManagedCallWrapper(&AudioMixerSnapshotDataChangedPacked);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pool = SystemAPI.GetSingleton<AudioSourcePool>();
            var sources = pool.AudioSources;

            this.UpdateAudioSource(ref state, sources);

            foreach (var (index, component) in SystemAPI.Query<RefRO<AudioSourceIndex>, RefRO<AudioSourceDataExtended>>()
                .WithChangeFilter<AudioSourceDataExtended, AudioSourceIndex>())
            {
                Burst.AudioSourceDataExtended.Data.Invoke(sources[index.ValueRO.PoolIndex], component.ValueRO);
            }

            // foreach (var (pool, component) in SystemAPI.Query<AudioSourcePool, AudioReverbZoneData>().WithChangeFilter<AudioReverbZoneData, AudioSourcePool>())
            // {
            //     Burst.AudioReverbZoneData.Data.Invoke(pools[pool.PoolIndex], component);
            // }

            foreach (var (index, component) in SystemAPI.Query<RefRO<AudioSourceIndex>, RefRO<AudioLowPassFilterData>>()
                .WithChangeFilter<AudioLowPassFilterData, AudioSourceIndex>())
            {
                Burst.AudioLowPassFilterData.Data.Invoke(sources[index.ValueRO.PoolIndex], component.ValueRO);
            }

            foreach (var (index, component) in SystemAPI.Query<RefRO<AudioSourceIndex>, RefRO<AudioHighPassFilterData>>()
                .WithChangeFilter<AudioHighPassFilterData, AudioSourceIndex>())
            {
                Burst.AudioHighPassFilterData.Data.Invoke(sources[index.ValueRO.PoolIndex], component.ValueRO);
            }

            foreach (var (index, component) in SystemAPI.Query<RefRO<AudioSourceIndex>, RefRO<AudioDistortionFilterData>>()
                .WithChangeFilter<AudioDistortionFilterData, AudioSourceIndex>())
            {
                Burst.AudioDistortionFilterData.Data.Invoke(sources[index.ValueRO.PoolIndex], component.ValueRO);
            }

            foreach (var (index, component) in SystemAPI.Query<RefRO<AudioSourceIndex>, RefRO<AudioEchoFilterData>>()
                .WithChangeFilter<AudioEchoFilterData, AudioSourceIndex>())
            {
                Burst.AudioEchoFilterData.Data.Invoke(sources[index.ValueRO.PoolIndex], component.ValueRO);
            }

            foreach (var (index, component) in SystemAPI.Query<RefRO<AudioSourceIndex>, RefRO<AudioReverbFilterData>>()
                .WithChangeFilter<AudioReverbFilterData, AudioSourceIndex>())
            {
                Burst.AudioReverbFilterData.Data.Invoke(sources[index.ValueRO.PoolIndex], component.ValueRO);
            }

            foreach (var (index, component) in SystemAPI.Query<RefRO<AudioSourceIndex>, RefRO<AudioChorusFilterData>>()
                .WithChangeFilter<AudioChorusFilterData, AudioSourceIndex>())
            {
                Burst.AudioChorusFilterData.Data.Invoke(sources[index.ValueRO.PoolIndex], component.ValueRO);
            }

            // foreach (var component in SystemAPI.Query<AudioMixerSnapshotData>().WithChangeFilter<AudioMixerSnapshotData>())
            // {
            //     Burst.AudioMixerSnapshotData.Data.Invoke(component);
            // }
        }

        private unsafe void UpdateAudioSource(ref SystemState state, NativeArray<AudioFacade>.ReadOnly sources)
        {
            var audioSourceQuery = SystemAPI.QueryBuilder().WithAll<AudioSourceIndex, AudioSourceData, GlobalVolume>().Build();
            audioSourceQuery.CompleteDependency();

            var audioSourceIndexHandle = SystemAPI.GetComponentTypeHandle<AudioSourceIndex>(true);
            var audioSourceDataHandle = SystemAPI.GetComponentTypeHandle<AudioSourceData>(true);
            var globalVolumeHandle = SystemAPI.GetComponentTypeHandle<GlobalVolume>(true);

            var queryIterator = new QueryEntityEnumerator(audioSourceQuery);
            while (queryIterator.MoveNextChunk(out var chunk, out var chunkIterator))
            {
                if (!chunk.DidChange(ref audioSourceDataHandle, state.LastSystemVersion) &&
                    !chunk.DidChange(ref audioSourceIndexHandle, state.LastSystemVersion) &&
                    !chunk.DidChange(ref globalVolumeHandle, state.LastSystemVersion))
                {
                    continue;
                }

                var indices = (AudioSourceIndex*)chunk.GetRequiredComponentDataPtrRO(ref audioSourceIndexHandle);
                var components = (AudioSourceData*)chunk.GetRequiredComponentDataPtrRO(ref audioSourceDataHandle);
                var globalVolumes = (GlobalVolume*)chunk.GetRequiredComponentDataPtrRO(ref globalVolumeHandle);

                while (chunkIterator.NextEntityIndex(out var entityIndexInChunk))
                {
                    var component = components[entityIndexInChunk];
                    component.Volume = math.saturate(component.Volume * globalVolumes[entityIndexInChunk].Volume);
                    Burst.AudioSource.Data.Invoke(sources[indices[entityIndexInChunk].PoolIndex], component);
                }
            }
        }

        private static unsafe void AudioSourceChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioSourceData>>(argumentsPtr, argumentsSize);
            ref var facade = ref arguments.First;
            ref var component = ref arguments.Second;
            var audioSource = facade.AudioSource.Value;
            audioSource.volume = component.Volume;
            audioSource.pitch = component.Pitch;
        }

        private static unsafe void AudioSourceDataExtendedChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioSourceDataExtended>>(argumentsPtr, argumentsSize);
            ref var facade = ref arguments.First;
            ref var component = ref arguments.Second;
            var audioSource = facade.AudioSource.Value;
            var newClip = component.Clip.Value;
            var play = audioSource.clip != newClip || !audioSource.isPlaying;

            audioSource.clip = component.Clip.Value;
            audioSource.panStereo = component.PanStereo;
            audioSource.spatialBlend = component.SpatialBlend;
            audioSource.minDistance = component.MinDistance;
            audioSource.maxDistance = component.MaxDistance;
            audioSource.dopplerLevel = component.DopplerLevel;
            audioSource.spread = component.Spread;
            audioSource.rolloffMode = component.RolloffMode;
            audioSource.priority = component.Priority;
            audioSource.reverbZoneMix = component.ReverbZoneMix;
            audioSource.enabled = true;

            if (play)
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying || SceneView.lastActiveSceneView is not { audioPlay: not true })
#endif
                {
                    audioSource.Play();
                }
            }
        }

        // private static unsafe void AudioReverbZoneDataChangedPacked(void* argumentsPtr, int argumentsSize)
        // {
        //     ref var arguments = ref BurstManagedCallWrapper.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioReverbZoneData>>(argumentsPtr, argumentsSize);
        //     ref var facade = ref arguments.First;
        //     ref var component = ref arguments.Second;
        //     var reverbZone = facade.AudioReverbZone;
        //     reverbZone.Value.enabled = true;
        //     reverbZone.Value.minDistance = component.MinDistance;
        //     reverbZone.Value.maxDistance = component.MaxDistance;
        //     reverbZone.Value.reverbPreset = component.ReverbPreset;
        //     reverbZone.Value.room = component.Room;
        //     reverbZone.Value.roomHF = component.RoomHF;
        //     reverbZone.Value.roomLF = component.RoomLF;
        //     reverbZone.Value.decayTime = component.DecayTime;
        //     reverbZone.Value.decayHFRatio = component.DecayHFRatio;
        //     reverbZone.Value.reflections = component.Reflections;
        //     reverbZone.Value.reflectionsDelay = component.ReflectionsDelay;
        //     reverbZone.Value.reverb = component.Reverb;
        //     reverbZone.Value.reverbDelay = component.ReverbDelay;
        //     reverbZone.Value.HFReference = component.HFReference;
        //     reverbZone.Value.LFReference = component.LFReference;
        //     reverbZone.Value.diffusion = component.Diffusion;
        //     reverbZone.Value.density = component.Density;
        // }

        private static unsafe void AudioLowPassFilterDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioLowPassFilterData>>(argumentsPtr, argumentsSize);
            ref var facade = ref arguments.First;
            ref var component = ref arguments.Second;
            var filter = facade.AudioLowPassFilter.Value;
            filter.cutoffFrequency = component.CutoffFrequency;
            filter.lowpassResonanceQ = component.LowpassResonanceQ;
            filter.enabled = true;
        }

        private static unsafe void AudioHighPassFilterDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioHighPassFilterData>>(argumentsPtr, argumentsSize);
            ref var facade = ref arguments.First;
            ref var component = ref arguments.Second;
            var filter = facade.AudioHighPassFilter.Value;
            filter.cutoffFrequency = component.CutoffFrequency;
            filter.highpassResonanceQ = component.HighpassResonanceQ;
            filter.enabled = true;
        }

        private static unsafe void AudioDistortionFilterDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioDistortionFilterData>>(argumentsPtr, argumentsSize);
            ref var facade = ref arguments.First;
            ref var component = ref arguments.Second;
            var filter = facade.AudioDistortionFilter.Value;
            filter.distortionLevel = component.DistortionLevel;
            filter.enabled = true;
        }

        private static unsafe void AudioEchoFilterDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioEchoFilterData>>(argumentsPtr, argumentsSize);
            ref var facade = ref arguments.First;
            ref var component = ref arguments.Second;
            var filter = facade.AudioEchoFilter.Value;
            filter.delay = component.Delay;
            filter.decayRatio = component.DecayRatio;
            filter.wetMix = component.WetMix;
            filter.dryMix = component.DryMix;
            filter.enabled = true;
        }

        private static unsafe void AudioReverbFilterDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioReverbFilterData>>(argumentsPtr, argumentsSize);
            ref var facade = ref arguments.First;
            ref var component = ref arguments.Second;
            var filter = facade.AudioReverbFilter.Value;
            filter.reverbPreset = component.ReverbPreset;
            filter.dryLevel = component.DryLevel;
            filter.room = component.Room;
            filter.roomHF = component.RoomHF;
            filter.roomLF = component.RoomLF;
            filter.decayTime = component.DecayTime;
            filter.decayHFRatio = component.DecayHFRatio;
            filter.reflectionsLevel = component.ReflectionsLevel;
            filter.reflectionsDelay = component.ReflectionsDelay;
            filter.reverbLevel = component.ReverbLevel;
            filter.reverbDelay = component.ReverbDelay;
            filter.hfReference = component.HFReference;
            filter.lfReference = component.LFReference;
            filter.diffusion = component.Diffusion;
            filter.density = component.Density;
            filter.enabled = true;
        }

        private static unsafe void AudioChorusFilterDataChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<AudioFacade, AudioChorusFilterData>>(argumentsPtr, argumentsSize);
            ref var facade = ref arguments.First;
            ref var component = ref arguments.Second;
            var filter = facade.AudioChorusFilter.Value;
            filter.dryMix = component.DryMix;
            filter.wetMix1 = component.WetMix1;
            filter.wetMix2 = component.WetMix2;
            filter.wetMix3 = component.WetMix3;
            filter.delay = component.Delay;
            filter.rate = component.Rate;
            filter.depth = component.Depth;
            filter.enabled = true;
        }

        // private static unsafe void AudioMixerSnapshotDataChangedPacked(void* argumentsPtr, int argumentsSize)
        // {
        //     ref var component = ref BurstManagedCallWrapper.ArgumentsFromPtr<AudioMixerSnapshotData>(argumentsPtr, argumentsSize);
        //     var snapshot = component.Snapshot.Value;
        //     if (snapshot == null)
        //     {
        //         return;
        //     }
        //
        //     snapshot.TransitionTo(component.TransitionDuration);
        // }

        private static class Burst
        {
            public static readonly SharedStatic<BurstTrampoline> AudioSource =
                SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem, AudioSourceData>();

            public static readonly SharedStatic<BurstTrampoline> AudioSourceDataExtended =
                SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem, AudioSourceDataExtended>();

            // public static readonly SharedStatic<BurstDelegate<AudioFacade, AudioReverbZoneData>> AudioReverbZoneData =
            // SharedStatic<BurstDelegate<AudioFacade, AudioReverbZoneData>>.GetOrCreate<AudioSyncSystem, BurstDelegate<AudioFacade, AudioReverbZoneData>>();

            public static readonly SharedStatic<BurstTrampoline> AudioLowPassFilterData =
                SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem, AudioLowPassFilterData>();

            public static readonly SharedStatic<BurstTrampoline> AudioHighPassFilterData =
                SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem, AudioHighPassFilterData>();

            public static readonly SharedStatic<BurstTrampoline> AudioDistortionFilterData =
                SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem, AudioDistortionFilterData>();

            public static readonly SharedStatic<BurstTrampoline> AudioEchoFilterData =
                SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem, AudioEchoFilterData>();

            public static readonly SharedStatic<BurstTrampoline> AudioReverbFilterData =
                SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem, AudioReverbFilterData>();

            public static readonly SharedStatic<BurstTrampoline> AudioChorusFilterData =
                SharedStatic<BurstTrampoline>.GetOrCreate<AudioSyncSystem, AudioChorusFilterData>();

            // public static readonly SharedStatic<BurstManagedCallWrapper> AudioMixerSnapshotData =
                // SharedStatic<BurstManagedCallWrapper>.GetOrCreate<AudioSyncSystem, AudioMixerSnapshotData>();
        }
    }
}
#endif
