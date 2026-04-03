// <copyright file="AudioSourcePool.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Util
{
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    internal struct AudioSourcePool : IComponentData
    {
        public NativeArray<AudioFacade>.ReadOnly AudioSources;
        public TrackedIndexPool Pool;
    }

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
