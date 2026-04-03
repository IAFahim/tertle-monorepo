// <copyright file="RelevancySystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.App
{
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    [UpdateInGroup(typeof(AfterTransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation, WorldSystemFilterFlags.ServerSimulation)]
    public partial class RelevancySystemGroup : ComponentSystemGroup
    {
    }
}
