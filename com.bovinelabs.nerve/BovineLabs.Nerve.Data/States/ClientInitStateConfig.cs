// <copyright file="ClientInitStateConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.States
{
    using Unity.Entities;

    public struct ClientInitStateConfig : IComponentData
    {
        public byte DefaultState;
    }
}
