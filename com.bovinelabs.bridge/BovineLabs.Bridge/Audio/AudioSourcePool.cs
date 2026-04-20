// <copyright file="AudioSourcePool.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using Unity.Collections;
    using Unity.Entities;

    internal struct AudioSourcePool : IComponentData
    {
        public NativeArray<AudioFacade>.ReadOnly AudioSources;
        public TrackedIndexPool LoopedPool;
        public TrackedIndexPool OneShotPool;
        public NativeArray<long> OneShotOrder;
        public int LoopedStartIndex;
        public int OneShotStartIndex;
    }
}
#endif
