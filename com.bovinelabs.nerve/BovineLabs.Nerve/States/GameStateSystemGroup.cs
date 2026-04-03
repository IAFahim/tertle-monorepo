// <copyright file="GameStateSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States
{
    using BovineLabs.Core;
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    [WorldSystemFilter(Worlds.ClientLocal | Worlds.Service, Worlds.ClientLocal)]
    public partial class GameStateSystemGroup : ComponentSystemGroup
    {
    }
}
