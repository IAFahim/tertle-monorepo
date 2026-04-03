// <copyright file="AudioSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Audio
{
    using AOT;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Bridge.Util;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [UpdateInGroup(typeof(BridgeSystemGroup))]
    [UpdateAfter(typeof(AudioSourcePoolSyncSystem))]
    public partial struct AudioSyncSystem : ISystem
    {
        static AudioSyncSystem()
        {
            Burst.AudioSource.Data = new BurstTrampoline<AudioFacade, AudioSourceData>(AudioSourceChanged);
            Burst.AudioSourceDataExtended.Data = new BurstTrampoline<AudioFacade, AudioSourceDataExtended>(AudioSourceDataExtendedChanged);
            Burst.AudioLowPassFilterData.Data = new BurstTrampoline<AudioFacade, AudioLowPassFilterData>(AudioLowPassFilterDataChanged);
            Burst.AudioHighPassFilterData.Data = new BurstTrampoline<AudioFacade, AudioHighPassFilterData>(AudioHighPassFilterDataChanged);
            Burst.AudioDistortionFilterData.Data = new BurstTrampoline<AudioFacade, AudioDistortionFilterData>(AudioDistortionFilterDataChanged);
            Burst.AudioEchoFilterData.Data = new BurstTrampoline<AudioFacade, AudioEchoFilterData>(AudioEchoFilterDataChanged);
            Burst.AudioReverbFilterData.Data = new BurstTrampoline<AudioFacade, AudioReverbFilterData>(AudioReverbFilterDataChanged);
            Burst.AudioChorusFilterData.Data = new BurstTrampoline<AudioFacade, AudioChorusFilterData>(AudioChorusFilterDataChanged);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var sources = SystemAPI.GetSingleton<AudioSourcePool>().AudioSources;

            foreach (var (pool, component) in SystemAPI.Query<AudioSourceIndex, AudioSourceData>().WithChangeFilter<AudioSourceData, AudioSourceIndex>())
            {
                Burst.AudioSource.Data.Invoke(sources[pool.PoolIndex], component);
            }

            foreach (var (pool, component) in SystemAPI
                .Query<AudioSourceIndex, AudioSourceDataExtended>()
                .WithChangeFilter<AudioSourceDataExtended, AudioSourceIndex>())
            {
                Burst.AudioSourceDataExtended.Data.Invoke(sources[pool.PoolIndex], component);
            }

            // foreach (var (pool, component) in SystemAPI.Query<AudioSourcePool, AudioReverbZoneData>().WithChangeFilter<AudioReverbZoneData, AudioSourcePool>())
            // {
            //     Burst.AudioReverbZoneData.Data.Invoke(pools[pool.PoolIndex], component);
            // }

            foreach (var (pool, component) in SystemAPI
                .Query<AudioSourceIndex, AudioLowPassFilterData>()
                .WithChangeFilter<AudioLowPassFilterData, AudioSourceIndex>())
            {
                Burst.AudioLowPassFilterData.Data.Invoke(sources[pool.PoolIndex], component);
            }

            foreach (var (pool, component) in SystemAPI
                .Query<AudioSourceIndex, AudioHighPassFilterData>()
                .WithChangeFilter<AudioHighPassFilterData, AudioSourceIndex>())
            {
                Burst.AudioHighPassFilterData.Data.Invoke(sources[pool.PoolIndex], component);
            }

            foreach (var (pool, component) in SystemAPI
                .Query<AudioSourceIndex, AudioDistortionFilterData>()
                .WithChangeFilter<AudioDistortionFilterData, AudioSourceIndex>())
            {
                Burst.AudioDistortionFilterData.Data.Invoke(sources[pool.PoolIndex], component);
            }

            foreach (var (pool, component) in
                SystemAPI.Query<AudioSourceIndex, AudioEchoFilterData>().WithChangeFilter<AudioEchoFilterData, AudioSourceIndex>())
            {
                Burst.AudioEchoFilterData.Data.Invoke(sources[pool.PoolIndex], component);
            }

            foreach (var (pool, component) in SystemAPI
                .Query<AudioSourceIndex, AudioReverbFilterData>()
                .WithChangeFilter<AudioReverbFilterData, AudioSourceIndex>())
            {
                Burst.AudioReverbFilterData.Data.Invoke(sources[pool.PoolIndex], component);
            }

            foreach (var (pool, component) in SystemAPI
                .Query<AudioSourceIndex, AudioChorusFilterData>()
                .WithChangeFilter<AudioChorusFilterData, AudioSourceIndex>())
            {
                Burst.AudioChorusFilterData.Data.Invoke(sources[pool.PoolIndex], component);
            }
        }

        [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioSourceData>.Delegate))]
        private static void AudioSourceChanged(in AudioFacade facade, in AudioSourceData component)
        {
            var audioSource = facade.AudioSource;
            audioSource.Value.volume = component.Volume;
            audioSource.Value.pitch = component.Pitch;
        }

        [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioSourceDataExtended>.Delegate))]
        private static void AudioSourceDataExtendedChanged(in AudioFacade facade, in AudioSourceDataExtended component)
        {
            var audioSource = facade.AudioSource;

            var newClip = component.Clip.Value;
            var play = audioSource.Value.clip != newClip || !audioSource.Value.isPlaying;

            audioSource.Value.clip = component.Clip.Value;
            audioSource.Value.panStereo = component.PanStereo;
            audioSource.Value.spatialBlend = component.SpatialBlend;
            audioSource.Value.minDistance = component.MinDistance;
            audioSource.Value.maxDistance = component.MaxDistance;
            audioSource.Value.dopplerLevel = component.DopplerLevel;
            audioSource.Value.spread = component.Spread;
            audioSource.Value.rolloffMode = component.RolloffMode;
            audioSource.Value.priority = component.Priority;
            audioSource.Value.reverbZoneMix = component.ReverbZoneMix;
            audioSource.Value.enabled = true;

            if (play)
            {
#if UNITY_EDITOR
                if (EditorApplication.isPlaying || SceneView.lastActiveSceneView is not { audioPlay: not true })
#endif
                {
                    audioSource.Value.Play();
                }
            }
        }

        // [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioReverbZoneData>.Delegate))]
        // private static void AudioReverbZoneDataChanged(in AudioFacade facade, in AudioReverbZoneData component)
        // {
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

        [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioLowPassFilterData>.Delegate))]
        private static void AudioLowPassFilterDataChanged(in AudioFacade facade, in AudioLowPassFilterData component)
        {
            var filter = facade.AudioLowPassFilter;
            filter.Value.cutoffFrequency = component.CutoffFrequency;
            filter.Value.lowpassResonanceQ = component.LowpassResonanceQ;
            filter.Value.enabled = true;
        }

        [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioHighPassFilterData>.Delegate))]
        private static void AudioHighPassFilterDataChanged(in AudioFacade facade, in AudioHighPassFilterData component)
        {
            var filter = facade.AudioHighPassFilter;
            filter.Value.cutoffFrequency = component.CutoffFrequency;
            filter.Value.highpassResonanceQ = component.HighpassResonanceQ;
            filter.Value.enabled = true;
        }

        [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioDistortionFilterData>.Delegate))]
        private static void AudioDistortionFilterDataChanged(in AudioFacade facade, in AudioDistortionFilterData component)
        {
            var filter = facade.AudioDistortionFilter;
            filter.Value.distortionLevel = component.DistortionLevel;
            filter.Value.enabled = true;
        }

        [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioEchoFilterData>.Delegate))]
        private static void AudioEchoFilterDataChanged(in AudioFacade facade, in AudioEchoFilterData component)
        {
            var filter = facade.AudioEchoFilter;
            filter.Value.delay = component.Delay;
            filter.Value.decayRatio = component.DecayRatio;
            filter.Value.wetMix = component.WetMix;
            filter.Value.dryMix = component.DryMix;
            filter.Value.enabled = true;
        }

        [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioReverbFilterData>.Delegate))]
        private static void AudioReverbFilterDataChanged(in AudioFacade facade, in AudioReverbFilterData component)
        {
            var filter = facade.AudioReverbFilter;
            filter.Value.reverbPreset = component.ReverbPreset;
            filter.Value.dryLevel = component.DryLevel;
            filter.Value.room = component.Room;
            filter.Value.roomHF = component.RoomHF;
            filter.Value.roomLF = component.RoomLF;
            filter.Value.decayTime = component.DecayTime;
            filter.Value.decayHFRatio = component.DecayHFRatio;
            filter.Value.reflectionsLevel = component.ReflectionsLevel;
            filter.Value.reflectionsDelay = component.ReflectionsDelay;
            filter.Value.reverbLevel = component.ReverbLevel;
            filter.Value.reverbDelay = component.ReverbDelay;
            filter.Value.hfReference = component.HFReference;
            filter.Value.lfReference = component.LFReference;
            filter.Value.diffusion = component.Diffusion;
            filter.Value.density = component.Density;
            filter.Value.enabled = true;
        }

        [MonoPInvokeCallback(typeof(BurstTrampoline<AudioFacade, AudioChorusFilterData>.Delegate))]
        private static void AudioChorusFilterDataChanged(in AudioFacade facade, in AudioChorusFilterData component)
        {
            var filter = facade.AudioChorusFilter;
            filter.Value.dryMix = component.DryMix;
            filter.Value.wetMix1 = component.WetMix1;
            filter.Value.wetMix2 = component.WetMix2;
            filter.Value.wetMix3 = component.WetMix3;
            filter.Value.delay = component.Delay;
            filter.Value.rate = component.Rate;
            filter.Value.depth = component.Depth;
            filter.Value.enabled = true;
        }

        private static class Burst
        {
            public static readonly SharedStatic<BurstTrampoline<AudioFacade, AudioSourceData>> AudioSource =
                SharedStatic<BurstTrampoline<AudioFacade, AudioSourceData>>.GetOrCreate<AudioSyncSystem, AudioSourceData>();

            public static readonly SharedStatic<BurstTrampoline<AudioFacade, AudioSourceDataExtended>> AudioSourceDataExtended =
                SharedStatic<BurstTrampoline<AudioFacade, AudioSourceDataExtended>>.GetOrCreate<AudioSyncSystem, AudioSourceDataExtended>();

            // public static readonly SharedStatic<BurstDelegate<AudioFacade, AudioReverbZoneData>> AudioReverbZoneData =
            // SharedStatic<BurstDelegate<AudioFacade, AudioReverbZoneData>>.GetOrCreate<AudioSyncSystem, BurstDelegate<AudioFacade, AudioReverbZoneData>>();

            public static readonly SharedStatic<BurstTrampoline<AudioFacade, AudioLowPassFilterData>> AudioLowPassFilterData =
                SharedStatic<BurstTrampoline<AudioFacade, AudioLowPassFilterData>>.GetOrCreate<AudioSyncSystem, AudioLowPassFilterData>();

            public static readonly SharedStatic<BurstTrampoline<AudioFacade, AudioHighPassFilterData>> AudioHighPassFilterData =
                SharedStatic<BurstTrampoline<AudioFacade, AudioHighPassFilterData>>.GetOrCreate<AudioSyncSystem, AudioHighPassFilterData>();

            public static readonly SharedStatic<BurstTrampoline<AudioFacade, AudioDistortionFilterData>> AudioDistortionFilterData =
                SharedStatic<BurstTrampoline<AudioFacade, AudioDistortionFilterData>>.GetOrCreate<AudioSyncSystem, AudioDistortionFilterData>();

            public static readonly SharedStatic<BurstTrampoline<AudioFacade, AudioEchoFilterData>> AudioEchoFilterData =
                SharedStatic<BurstTrampoline<AudioFacade, AudioEchoFilterData>>.GetOrCreate<AudioSyncSystem, AudioEchoFilterData>();

            public static readonly SharedStatic<BurstTrampoline<AudioFacade, AudioReverbFilterData>> AudioReverbFilterData =
                SharedStatic<BurstTrampoline<AudioFacade, AudioReverbFilterData>>.GetOrCreate<AudioSyncSystem, AudioReverbFilterData>();

            public static readonly SharedStatic<BurstTrampoline<AudioFacade, AudioChorusFilterData>> AudioChorusFilterData =
                SharedStatic<BurstTrampoline<AudioFacade, AudioChorusFilterData>>.GetOrCreate<AudioSyncSystem, AudioChorusFilterData>();
        }
    }
}
