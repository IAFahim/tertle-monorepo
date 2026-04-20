// <copyright file="AudioReverbZoneData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;
    using UnityEngine;

    public struct AudioReverbZoneData : IComponentData
    {
        public float MinDistance;
        public float MaxDistance;
        public AudioReverbPreset ReverbPreset;

        // Custom reverb properties (when ReverbPreset is set to User)
        public int Room;
        public int RoomHF;
        public int RoomLF;
        public float DecayTime;
        public float DecayHFRatio;
        public int Reflections;
        public float ReflectionsDelay;
        public int Reverb;
        public float ReverbDelay;
        public float HFReference;
        public float LFReference;
        public float Diffusion;
        public float Density;
    }
}
#endif
