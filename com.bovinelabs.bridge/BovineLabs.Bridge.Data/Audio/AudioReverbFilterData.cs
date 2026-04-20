// <copyright file="AudioReverbFilterData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;
    using UnityEngine;

    public struct AudioReverbFilterData : IComponentData
    {
        public AudioReverbPreset ReverbPreset;
        public float DryLevel;
        public float Room;
        public float RoomHF;
        public float RoomLF;
        public float DecayTime;
        public float DecayHFRatio;
        public float ReflectionsLevel;
        public float ReflectionsDelay;
        public float ReverbLevel;
        public float ReverbDelay;
        public float HFReference;
        public float LFReference;
        public float Diffusion;
        public float Density;
    }
}
#endif
