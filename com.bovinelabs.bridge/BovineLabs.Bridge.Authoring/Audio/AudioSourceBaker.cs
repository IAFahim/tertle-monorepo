// <copyright file="AudioSourceBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Authoring.Audio
{
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Core.Authoring;
    using Unity.Entities;
    using UnityEngine;

    public class AudioSourceBaker : Baker<AudioSource>
    {
        private const float MinAudibleVolume = 0.01f;
        private const int CustomRolloffSamples = 32;

        /// <inheritdoc />
        public override void Bake(AudioSource authoring)
        {
            // If user drags playing audio source into an open subscene
            if (authoring.isPlaying)
            {
                authoring.Stop();
            }

            var entity = this.GetEntity(TransformUsageFlags.Renderable);

            // Mute = authoring.mute,

            var components = new ComponentTypeSet(new[]
            {
                ComponentType.ReadWrite<AudioSourceData>(),
                ComponentType.ReadWrite<AudioSourceDataExtended>(),
                ComponentType.ReadWrite<AudioSourceAudibleRange>(),
                ComponentType.ReadWrite<AudioSourceIndex>(),
                ComponentType.ReadWrite<AudioSourceEnabled>(),
                ComponentType.ReadWrite<GlobalVolume>(),
            });

            this.AddComponent(entity, components);

            this.SetComponent(entity, new AudioSourceData
            {
                Volume = authoring.volume,
                Pitch = authoring.pitch,
            });

            this.SetComponent(entity, new AudioSourceDataExtended
            {
                Clip = authoring.clip,
                PanStereo = authoring.panStereo,
                SpatialBlend = authoring.spatialBlend,
                MinDistance = authoring.minDistance,
                MaxDistance = authoring.maxDistance,
                DopplerLevel = authoring.dopplerLevel,
                Spread = authoring.spread,
                RolloffMode = authoring.rolloffMode,
                Priority = authoring.priority,
                ReverbZoneMix = authoring.reverbZoneMix,
            });

            this.SetComponent(entity, new AudioSourceAudibleRange
            {
                MaxDistance = CalculateAudibleDistance(authoring),
            });

            this.SetComponent(entity, new GlobalVolume
            {
                Volume = 1f,
            });

            if (authoring.loop)
            {
                this.AddEnabledComponent<AudioSourceEnabledPrevious>(entity, false);
            }
            else
            {
                // One shot don't need previous are single instance
                this.AddComponent<AudioSourceOneShot>(entity);
            }

            this.SetComponent(entity, new AudioSourceIndex { PoolIndex = -1 });
            this.SetComponentEnabled<AudioSourceIndex>(entity, false);
            this.SetComponentEnabled<AudioSourceEnabled>(entity, authoring.playOnAwake && authoring.enabled);
        }

        private static float CalculateAudibleDistance(AudioSource authoring)
        {
            var volume = Mathf.Max(0f, authoring.volume);
            var spatialBlend = Mathf.Clamp01(authoring.spatialBlend);
            var nonSpatialVolume = volume * (1f - spatialBlend);
            if (nonSpatialVolume >= MinAudibleVolume)
            {
                return float.PositiveInfinity;
            }

            var spatialVolume = volume * spatialBlend;
            if (spatialVolume <= 0f)
            {
                return 0f;
            }

            var minDistance = Mathf.Max(0.0001f, authoring.minDistance);
            var maxDistance = Mathf.Max(minDistance, authoring.maxDistance);
            var targetAttenuation = MinAudibleVolume / spatialVolume;
            if (targetAttenuation >= 1f)
            {
                return 0f;
            }

            switch (authoring.rolloffMode)
            {
                case AudioRolloffMode.Linear:
                    return CalculateLinearDistance(minDistance, maxDistance, targetAttenuation);
                case AudioRolloffMode.Logarithmic:
                    return Mathf.Min(maxDistance, minDistance / targetAttenuation);
                case AudioRolloffMode.Custom:
                    return CalculateCustomDistance(authoring, minDistance, maxDistance, targetAttenuation);
                default:
                    return maxDistance;
            }
        }

        private static float CalculateLinearDistance(float minDistance, float maxDistance, float targetAttenuation)
        {
            if (maxDistance <= minDistance)
            {
                return maxDistance;
            }

            var distance = maxDistance - (targetAttenuation * (maxDistance - minDistance));
            return Mathf.Clamp(distance, minDistance, maxDistance);
        }

        private static float CalculateCustomDistance(AudioSource authoring, float minDistance, float maxDistance, float targetAttenuation)
        {
            var curve = authoring.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
            if (curve == null || curve.length == 0)
            {
                return maxDistance;
            }

            var previousT = 0f;
            var previousValue = curve.Evaluate(0f);
            if (previousValue <= targetAttenuation)
            {
                return minDistance;
            }

            for (var i = 1; i <= CustomRolloffSamples; i++)
            {
                var t = i / (float)CustomRolloffSamples;
                var value = curve.Evaluate(t);
                if (value <= targetAttenuation)
                {
                    var range = previousValue - value;
                    var lerp = range > 0f ? (previousValue - targetAttenuation) / range : 0f;
                    var distanceT = Mathf.Lerp(previousT, t, Mathf.Clamp01(lerp));
                    return Mathf.Lerp(minDistance, maxDistance, distanceT);
                }

                previousT = t;
                previousValue = value;
            }

            return maxDistance;
        }
    }
}
#endif
