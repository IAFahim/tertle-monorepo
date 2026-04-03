// <copyright file="ConnectionEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.App
{
    using Unity.Entities;

    public struct ConnectionEntity : IComponentData
    {
        public Entity Connection;
    }
}
