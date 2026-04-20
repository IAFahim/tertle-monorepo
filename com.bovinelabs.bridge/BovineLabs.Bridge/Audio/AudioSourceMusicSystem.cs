// <copyright file="AudioSourceMusicSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Core;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    [UpdateAfter(typeof(AudioSourcePoolSyncSystem))]
    [UpdateBefore(typeof(AudioSyncSystem))]
    public partial struct AudioSourceMusicSystem : ISystem, ISystemStartStop
    {
        private Entity slot0Entity;
        private Entity slot1Entity;

        /// <inheritdoc/>
        public void OnStartRunning(ref SystemState state)
        {
            this.slot0Entity = CreateMusicEntity(ref state, 0);
            this.slot1Entity = CreateMusicEntity(ref state, 1);
        }

        /// <inheritdoc/>
        public void OnStopRunning(ref SystemState state)
        {
            state.EntityManager.DestroyEntity(this.slot0Entity);
            state.EntityManager.DestroyEntity(this.slot1Entity);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new MusicBlendJob
            {
                AudioDataLookup = SystemAPI.GetComponentLookup<AudioSourceData>(),
                AudioExtendedLookup = SystemAPI.GetComponentLookup<AudioSourceDataExtended>(),
                Slot0Entity = this.slot0Entity,
                Slot1Entity = this.slot1Entity,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Logger = SystemAPI.GetSingleton<BLLogger>(),
            }.Schedule();
        }

        private static Entity CreateMusicEntity(ref SystemState state, byte slot)
        {
            var entity = state.EntityManager.CreateEntity(typeof(AudioSourceMusic), typeof(AudioSourceData), typeof(AudioSourceDataExtended),
                typeof(AudioSourceIndex), typeof(GlobalVolume));
            state.EntityManager.SetName(entity, "Music Source");

            state.EntityManager.SetComponentData(entity, new AudioSourceData { Volume = 1f, Pitch = 1f });
            state.EntityManager.SetComponentData(entity, new AudioSourceDataExtended
            {
                Clip = default,
                PanStereo = 0f,
                SpatialBlend = 0f,
                MinDistance = 1f,
                MaxDistance = 500f,
                DopplerLevel = 0f,
                Spread = 0f,
                RolloffMode = AudioRolloffMode.Logarithmic,
                Priority = 128,
                ReverbZoneMix = 1f,
            });
            state.EntityManager.SetComponentData(entity, new GlobalVolume { Volume = 1f });

            state.EntityManager.SetComponentData(entity, new AudioSourceIndex { PoolIndex = slot });
            state.EntityManager.SetComponentEnabled<AudioSourceIndex>(entity, true);
            return entity;
        }

        /// <summary>
        /// Blends between two music slots using current volumes as fade start points for continuity when retargeting mid-fade.
        /// TrackId 0 (or invalid clip) is treated as silence; crossfades prefer applying new clips to the quieter slot.
        /// </summary>
        [BurstCompile]
        private partial struct MusicBlendJob : IJobEntity
        {
            public ComponentLookup<AudioSourceData> AudioDataLookup;

            public ComponentLookup<AudioSourceDataExtended> AudioExtendedLookup;

            public Entity Slot0Entity;
            public Entity Slot1Entity;

            public float DeltaTime;
            public BLLogger Logger;

            private void Execute(
                ref MusicState stateValue, in MusicSelection selection, in MusicBlendConfig blendConfig,
                in DynamicBuffer<MusicTrackEntry> tracks)
            {
                var slot0 = this.Slot0Entity;
                var slot1 = this.Slot1Entity;

                var activeSlotEntity = stateValue.ActiveSlot == 0 ? slot0 : slot1;
                var inactiveSlotEntity = stateValue.ActiveSlot == 0 ? slot1 : slot0;

                var desiredTrackId = selection.TrackId;
                if (!this.TryGetTrack(desiredTrackId, tracks, out var desiredTrack))
                {
                    // Invalid selection -> treat as silence
                    desiredTrackId = 0;
                }

                if (!stateValue.IsFading && stateValue.TargetTrackId == 0 && stateValue.ActiveTrackId == desiredTrackId)
                {
                    return;
                }

                if (!IsFadeTargetMatch(stateValue, desiredTrackId))
                {
                    if (desiredTrackId != 0 && desiredTrackId != stateValue.ActiveTrackId)
                    {
                        this.PreferQuieterSlotForTarget(ref stateValue, ref activeSlotEntity, ref inactiveSlotEntity);
                    }

                    this.BeginFade(ref stateValue, desiredTrackId, desiredTrack, blendConfig, activeSlotEntity, inactiveSlotEntity);
                }

                if (!stateValue.IsFading)
                {
                    return;
                }

                this.UpdateFade(ref stateValue, activeSlotEntity, inactiveSlotEntity);
            }

            private void BeginFade(
                ref MusicState stateValue, int desiredTrackId, in MusicTrackEntry desiredTrack, in MusicBlendConfig blendConfig,
                Entity activeSlotEntity, Entity inactiveSlotEntity)
            {
                stateValue.ActiveFadeStartVolume = this.GetVolume(activeSlotEntity);
                stateValue.TargetFadeStartVolume = this.GetVolume(inactiveSlotEntity);

                stateValue.FadeTimer = 0f;
                stateValue.FadeDuration = GetBlendSeconds(desiredTrackId, desiredTrack, blendConfig);
                stateValue.IsFading = true;

                if (desiredTrackId == 0)
                {
                    stateValue.TargetTrackId = 0;
                    stateValue.TargetBaseVolume = 0f;
                    return;
                }

                if (desiredTrackId == stateValue.ActiveTrackId)
                {
                    // Cancel a fade or return to the active track smoothly.
                    stateValue.TargetTrackId = stateValue.ActiveTrackId;
                    stateValue.TargetBaseVolume = 0f;
                    return;
                }

                this.SetClip(inactiveSlotEntity, desiredTrack.Clip);
                stateValue.TargetTrackId = desiredTrackId;
                stateValue.TargetBaseVolume = desiredTrack.BaseVolume;

                if (stateValue.ActiveTrackId == 0)
                {
                    stateValue.ActiveBaseVolume = 0f;
                }
            }

            private void UpdateFade(ref MusicState stateValue, Entity activeSlotEntity, Entity inactiveSlotEntity)
            {
                stateValue.FadeTimer += this.DeltaTime;
                var duration = math.max(0f, stateValue.FadeDuration);
                var t = duration <= 0f ? 1f : math.saturate(stateValue.FadeTimer / duration);

                var activeEnd = 0f;
                var targetEnd = 0f;
                if (stateValue.TargetTrackId != 0 && stateValue.TargetTrackId != stateValue.ActiveTrackId)
                {
                    targetEnd = stateValue.TargetBaseVolume;
                }
                else if (stateValue.TargetTrackId == stateValue.ActiveTrackId && stateValue.ActiveTrackId != 0)
                {
                    activeEnd = stateValue.ActiveBaseVolume;
                }

                var activeVolume = math.lerp(stateValue.ActiveFadeStartVolume, activeEnd, t);
                var targetVolume = math.lerp(stateValue.TargetFadeStartVolume, targetEnd, t);

                this.SetVolume(activeSlotEntity, activeVolume);
                this.SetVolume(inactiveSlotEntity, targetVolume);

                if (t < 1f)
                {
                    return;
                }

                if (stateValue.TargetTrackId != 0 && stateValue.TargetTrackId != stateValue.ActiveTrackId)
                {
                    stateValue.ActiveSlot = (byte)(stateValue.ActiveSlot == 0 ? 1 : 0);
                    stateValue.ActiveTrackId = stateValue.TargetTrackId;
                    stateValue.ActiveBaseVolume = stateValue.TargetBaseVolume;
                }
                else if (stateValue.TargetTrackId == 0)
                {
                    stateValue.ActiveTrackId = 0;
                    stateValue.ActiveBaseVolume = 0f;
                    this.SetClip(activeSlotEntity, default);
                    this.SetClip(inactiveSlotEntity, default);
                }

                stateValue.TargetTrackId = 0;
                stateValue.TargetBaseVolume = 0f;
                stateValue.FadeTimer = 0f;
                stateValue.FadeDuration = 0f;
                stateValue.IsFading = false;
                stateValue.ActiveFadeStartVolume = 0f;
                stateValue.TargetFadeStartVolume = 0f;
            }

            private void PreferQuieterSlotForTarget(ref MusicState stateValue, ref Entity activeSlotEntity, ref Entity inactiveSlotEntity)
            {
                if (!stateValue.IsFading || stateValue.TargetTrackId == 0)
                {
                    return;
                }

                var activeVolume = this.GetVolume(activeSlotEntity);
                var inactiveVolume = this.GetVolume(inactiveSlotEntity);
                if (inactiveVolume <= activeVolume)
                {
                    return;
                }

                stateValue.ActiveSlot = (byte)(stateValue.ActiveSlot == 0 ? 1 : 0);

                // Swap
                (stateValue.ActiveTrackId, stateValue.TargetTrackId) = (stateValue.TargetTrackId, stateValue.ActiveTrackId);
                (stateValue.ActiveBaseVolume, stateValue.TargetBaseVolume) = (stateValue.TargetBaseVolume, stateValue.ActiveBaseVolume);
                (activeSlotEntity, inactiveSlotEntity) = (inactiveSlotEntity, activeSlotEntity);
            }

            private float GetVolume(Entity entity)
            {
                return this.AudioDataLookup[entity].Volume;
            }

            private void SetVolume(Entity entity, float volume)
            {
                this.AudioDataLookup.GetRefRW(entity).ValueRW.Volume = volume;
            }

            private void SetClip(Entity entity, UnityObjectRef<AudioClip> clip)
            {
                var extended = this.AudioExtendedLookup[entity];
                if (extended.Clip == clip)
                {
                    return;
                }

                extended.Clip = clip;
                this.AudioExtendedLookup[entity] = extended;
            }

            private static bool IsFadeTargetMatch(in MusicState stateValue, int desiredTrackId)
            {
                if (!stateValue.IsFading)
                {
                    return false;
                }

                if (desiredTrackId == 0)
                {
                    return stateValue.TargetTrackId == 0;
                }

                if (desiredTrackId == stateValue.ActiveTrackId)
                {
                    return stateValue.TargetTrackId == stateValue.ActiveTrackId;
                }

                return stateValue.TargetTrackId == desiredTrackId;
            }

            private static float GetBlendSeconds(int desiredTrackId, in MusicTrackEntry desiredTrack, in MusicBlendConfig blendConfig)
            {
                if (desiredTrackId != 0 && desiredTrack.BlendOverrideSeconds > 0f)
                {
                    return desiredTrack.BlendOverrideSeconds;
                }

                return math.max(0f, blendConfig.DefaultBlendSeconds);
            }

            private bool TryGetTrack(int trackId, in DynamicBuffer<MusicTrackEntry> tracks, out MusicTrackEntry track)
            {
                if (trackId > 0 && trackId < tracks.Length)
                {
                    track = tracks[trackId];
                    if (track.Clip.IsValid())
                    {
                        return true;
                    }

                    this.Logger.LogWarning($"Track {trackId} clip not set");
                }

                track = default;
                return false;
            }
        }
    }
}
#endif
