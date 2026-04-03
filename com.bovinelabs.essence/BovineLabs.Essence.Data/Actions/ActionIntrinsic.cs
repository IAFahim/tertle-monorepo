// <copyright file="ActionIntrinsic.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data.Actions
{
    using BovineLabs.Reaction.Data.Core;
    using Unity.Entities;

    /// <summary>
    /// An action that modifies an intrinsic value on an entity.
    /// </summary>
    [InternalBufferCapacity(1)]
    public struct ActionIntrinsic : IBufferElementData
    {
        public IntrinsicKey Intrinsic;
        public int Amount;
        public Target Target;
    }
}
