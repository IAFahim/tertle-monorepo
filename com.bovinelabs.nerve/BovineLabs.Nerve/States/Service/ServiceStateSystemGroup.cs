// <copyright file="ServiceStateSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States.Service
{
    using BovineLabs.Core;
    using Unity.Entities;

    [UpdateInGroup(typeof(GameStateSystemGroup))]
    [WorldSystemFilter(Worlds.Service, Worlds.Service)]
    public partial class ServiceStateSystemGroup : ComponentSystemGroup
    {
    }
}
