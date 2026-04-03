// <copyright file="ServiceInitStateConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.States
{
    using Unity.Entities;

    public struct ServiceInitStateConfig : IComponentData
    {
        public byte DefaultState;
    }
}
