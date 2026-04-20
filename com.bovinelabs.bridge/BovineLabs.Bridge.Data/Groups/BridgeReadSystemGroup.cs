// <copyright file="BridgeReadSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data
{
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    /// <summary> System for reading data from GameObjects and writing it to entities. </summary>
    [WorldSystemFilter(BridgeWorlds.All, BridgeWorlds.NoEditor)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    public partial class BridgeReadSystemGroup : ComponentSystemGroup
    {
    }
}