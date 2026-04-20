// <copyright file="BridgeSyncSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data
{
    using Unity.Entities;
    using Unity.Rendering;

    [WorldSystemFilter(BridgeWorlds.All, BridgeWorlds.All)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(StructuralChangePresentationSystemGroup))]
    public partial class BridgeSyncSystemGroup : ComponentSystemGroup
    {
    }
}
