// <copyright file="AudioVolumeData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Burst;

    public static class AudioVolumeData
    {
        public static readonly SharedStatic<bool> MuteWhenUnfocused = SharedStatic<bool>.GetOrCreate<MuteWhenUnfocusedType>();
        public static readonly SharedStatic<float> MusicVolume = SharedStatic<float>.GetOrCreate<MusicVolumeType>();
        public static readonly SharedStatic<float> AmbianceVolume = SharedStatic<float>.GetOrCreate<AmbianceVolumeType>();
        public static readonly SharedStatic<float> EffectVolume = SharedStatic<float>.GetOrCreate<EffectVolumeType>();

        public struct MuteWhenUnfocusedType
        {
        }

        public struct MusicVolumeType
        {
        }

        public struct AmbianceVolumeType
        {
        }

        public struct EffectVolumeType
        {
        }
    }
}
#endif
