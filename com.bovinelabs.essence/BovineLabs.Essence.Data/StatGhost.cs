// <copyright file="StatGhost.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Essence.Data
{
    using BovineLabs.Core.Stripping;
    using Unity.Entities;
    using Unity.NetCode;
    using Unity.Properties;

    /// <summary>
    /// Ghost-replicated stat key-value pairs for network synchronization.
    /// </summary>
    [StripLocal]
    [InternalBufferCapacity(0)]
    [GhostComponent]
    public struct StatGhost : IBufferElementData
    {
        [GhostField]
        public StatKey Key;

        [GhostField]
        [CreateProperty(ReadOnly = true)]
        public StatValue Value;
    }
}
#endif
