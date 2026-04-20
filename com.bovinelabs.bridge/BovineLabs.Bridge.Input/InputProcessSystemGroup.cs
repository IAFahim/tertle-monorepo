// <copyright file="InputProcessSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input
{
    using BovineLabs.Core;
    using Unity.Entities;
#if UNITY_NETCODE
    using Unity.NetCode;
#else
    using BovineLabs.Core.Groups;
#endif

    [WorldSystemFilter(Worlds.ClientLocal, WorldSystemFilterFlags.Presentation)]
    [UpdateAfter(typeof(InputSystemGroup))]
#if UNITY_NETCODE
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
#else
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
#endif
    public partial class InputProcessSystemGroup : ComponentSystemGroup
    {
    }
}
