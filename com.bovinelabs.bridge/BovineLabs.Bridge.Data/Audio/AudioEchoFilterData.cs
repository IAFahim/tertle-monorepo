// <copyright file="AudioEchoFilterData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    public struct AudioEchoFilterData : IComponentData
    {
        public float Delay;
        public float DecayRatio;
        public float WetMix;
        public float DryMix;
    }
}
#endif
