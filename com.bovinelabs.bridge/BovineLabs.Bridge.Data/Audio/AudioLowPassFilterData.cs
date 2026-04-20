// <copyright file="AudioLowPassFilterData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    public struct AudioLowPassFilterData : IComponentData
    {
        public float CutoffFrequency;
        public float LowpassResonanceQ;
    }
}
#endif
