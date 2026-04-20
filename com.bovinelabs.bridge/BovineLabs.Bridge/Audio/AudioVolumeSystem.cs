// <copyright file="AudioVolumeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary> Applies global volume multipliers to music, ambiance, and one-shot audio sources.</summary>
    [UpdateInGroup(typeof(BridgeSimulationSystemGroup), OrderLast = true)] // make sure we update after anything spawning like AudioSourceOneShotPoolSystem
    public partial struct AudioVolumeSystem : ISystem
    {
        private float musicVolume;
        private float ambianceVolume;
        private float effectVolume;

        private EntityQuery musicQuery;
        private EntityQuery effectQuery;
        private EntityQuery ambianceQuery;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.musicVolume = 1f;
            this.ambianceVolume = 1f;
            this.effectVolume = 1f;

            this.musicQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<GlobalVolume>()
                .WithAll<AudioSourceIndex, AudioSourceMusic>()
                .WithOptions(EntityQueryOptions.FilterWriteGroup)
                .Build();

            this.effectQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<GlobalVolume>()
                .WithAll<AudioSourceIndex, AudioSourceOneShot>()
                .WithOptions(EntityQueryOptions.FilterWriteGroup)
                .Build();

            this.ambianceQuery = SystemAPI
                .QueryBuilder()
                .WithAllRW<GlobalVolume>()
                .WithAll<AudioSourceIndex>()
                .WithOptions(EntityQueryOptions.FilterWriteGroup)
                .Build();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.CheckVolume(ref state, this.musicQuery, ref this.musicVolume, AudioVolumeData.MusicVolume.Data);
            this.CheckVolume(ref state, this.effectQuery, ref this.effectVolume, AudioVolumeData.EffectVolume.Data);
            this.CheckVolume(ref state, this.ambianceQuery, ref this.ambianceVolume, AudioVolumeData.AmbianceVolume.Data);
        }

        private void CheckVolume(ref SystemState state, EntityQuery query, ref float oldValue, float newValue)
        {
            const float eps = 0.005f;

            newValue = math.saturate(newValue);

            if (math.abs(oldValue - newValue) > eps)
            {
                oldValue = newValue;
                query.ResetFilter();
            }
            else
            {
                query.SetOrderVersionFilter();
            }

            new VolumeJob { Volume = oldValue }.Schedule(query);
        }

        [BurstCompile]
        private partial struct VolumeJob : IJobEntity
        {
            public float Volume;

            private void Execute(ref GlobalVolume volume)
            {
                volume.Volume = this.Volume;
            }
        }
    }
}
#endif
