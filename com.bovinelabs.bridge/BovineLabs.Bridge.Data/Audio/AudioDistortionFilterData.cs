// <copyright file="AudioDistortionFilterData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    public struct AudioDistortionFilterData : IComponentData
    {
        public float DistortionLevel;
    }
}
#endif
