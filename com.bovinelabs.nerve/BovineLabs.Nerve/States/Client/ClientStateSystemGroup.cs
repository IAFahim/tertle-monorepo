// <copyright file="ClientStateSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States.Client
{
    using BovineLabs.Core;
    using BovineLabs.Core.Pause;
    using Unity.Entities;

    [UpdateInGroup(typeof(GameStateSystemGroup))]
    [WorldSystemFilter(Worlds.ClientLocal, Worlds.ClientLocal)]
    public partial class ClientStateSystemGroup : ComponentSystemGroup, IUpdateWhilePaused
    {
    }
}
