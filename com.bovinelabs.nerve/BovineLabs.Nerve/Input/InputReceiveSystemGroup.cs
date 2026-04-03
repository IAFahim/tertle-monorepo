// <copyright file="InputReceiveSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Input
{
    using BovineLabs.Core;
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    [WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup), OrderLast = true)]
    public partial class InputReceiveSystemGroup : ComponentSystemGroup
    {
    }
}
