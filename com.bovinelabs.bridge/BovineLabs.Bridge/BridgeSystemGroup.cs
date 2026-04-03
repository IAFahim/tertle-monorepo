// <copyright file="BridgeSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge
{
    using Unity.Entities;
    using Unity.Rendering;

    [WorldSystemFilter(WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor, WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(StructuralChangePresentationSystemGroup))]
    public partial class BridgeSystemGroup : ComponentSystemGroup
    {
    }
}
