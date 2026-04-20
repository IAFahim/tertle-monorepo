// <copyright file="AudioSourceOneShot.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    [WriteGroup(typeof(GlobalVolume))]
    public struct AudioSourceOneShot : IComponentData
    {
    }
}
#endif
