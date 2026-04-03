// <copyright file="ClientJoinGameStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

// namespace BovineLabs.Net.States
// {
//     using BovineLabs.Core.States;
//     using BovineLabs.Net.App;
//     using BovineLabs.States.Data;
//     using Unity.Burst;
//     using Unity.Entities;
//     using Unity.NetCode;
//
//      [UpdateInGroup(typeof(ClientStateSystemGroup))]
//     public partial struct ClientJoinGameStateSystem : ISystem, ISystemStartStop
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             StateAPI.Register<GameState, StateJoinGame, ClientStates>(ref state, "join-game");
//         }
//
//         /// <inheritdoc/>
//         [BurstCompile]
//         public void OnStartRunning(ref SystemState state)
//         {
//             var ep = BovineLabsNetBootstrap.NetworkEndpoint.Data;
//             SystemAPI.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(state.EntityManager, ep);
//         }
//
//         /// <inheritdoc/>
//         public void OnStopRunning(ref SystemState state)
//         {
//             // Once system has run once, it should not run again
//             state.Enabled = false;
//         }
//
//         private struct StateJoinGame : IComponentData
//         {
//         }
//     }
// }


