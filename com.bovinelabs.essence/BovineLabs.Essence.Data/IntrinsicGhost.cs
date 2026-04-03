// <copyright file="IntrinsicGhost.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Essence.Data
{
    using BovineLabs.Core.Stripping;
    using Unity.Entities;
    using Unity.NetCode;

    /// <summary>
    /// Ghost-replicated intrinsic key-value pairs for network synchronization.
    /// </summary>
    [GhostComponent]
    [InternalBufferCapacity(0)]
    [StripLocal]
    public struct IntrinsicGhost : IBufferElementData
    {
        [GhostField]
        public IntrinsicKey Key;

        [GhostField]
        public int Value;
    }
}
#endif