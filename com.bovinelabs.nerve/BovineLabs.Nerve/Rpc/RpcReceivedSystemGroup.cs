// <copyright file="RpcReceivedSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Rpc
{
    using Unity.Entities;
    using Unity.NetCode;

    [UpdateAfter(typeof(RpcSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class RpcReceivedSystemGroup : ComponentSystemGroup
    {
    }
}
