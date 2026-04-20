// <copyright file="BridgeSimulationSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data
{
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    [WorldSystemFilter(BridgeWorlds.All, BridgeWorlds.All)]
    [UpdateInGroup(typeof(AfterTransformSystemGroup))]
    public partial class BridgeSimulationSystemGroup : ComponentSystemGroup
    {
    }
}
