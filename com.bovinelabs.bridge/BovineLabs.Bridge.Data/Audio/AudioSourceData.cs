// <copyright file="AudioSourceData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using BovineLabs.Core;
    using Unity.Entities;

    [ChangeFilterTracking]
    public struct AudioSourceData : IComponentData
    {
        public float Volume;
        public float Pitch;
    }
}
#endif
