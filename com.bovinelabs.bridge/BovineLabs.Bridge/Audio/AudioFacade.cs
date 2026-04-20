// <copyright file="AudioFacade.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using Unity.Entities;
    using UnityEngine;

    public struct AudioFacade
    {
        public UnityObjectRef<AudioSource> AudioSource;
        public UnityObjectRef<AudioLowPassFilter> AudioLowPassFilter;
        public UnityObjectRef<AudioHighPassFilter> AudioHighPassFilter;
        public UnityObjectRef<AudioDistortionFilter> AudioDistortionFilter;
        public UnityObjectRef<AudioEchoFilter> AudioEchoFilter;
        public UnityObjectRef<AudioReverbFilter> AudioReverbFilter;
        public UnityObjectRef<AudioChorusFilter> AudioChorusFilter;
    }
}
#endif
