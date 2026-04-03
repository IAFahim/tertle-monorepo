// <copyright file="ServerInitializePlayerSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

// namespace BovineLabs.Net.Rpc
// {
//     using BovineLabs.Core;
//     using BovineLabs.Core.ObjectManagement;
//     using BovineLabs.Net.Data;
//     using BovineLabs.Net.Data.App;
//     using Unity.Burst;
//     using Unity.Entities;
//
//     [WorldSystemFilter(Worlds.ServerLocal)]
//     [UpdateAfter(typeof(ServerGoInGameSystem))]
//     [UpdateInGroup(typeof(RpcReceivedSystemGroup))]
//     public partial struct ServerInitializePlayerSystem : ISystem
//     {
//         private EntityQuery query;
//
//         public void OnCreate(ref SystemState state)
//         {
//             this.query = SystemAPI.QueryBuilder().WithAll<UnityAuthentication>().WithNone<Initialized>().Build();
//             state.RequireForUpdate(this.query);
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var settings = SystemAPI.GetSingleton<ServerPrefabs>();
//
//             var objectDefinitions = SystemAPI.GetSingleton<ObjectDefinitionRegistry>();
//             var controllerPrefab = objectDefinitions[settings.PlayerController];
//
//             // Saved
//             // SystemAPI.QueryBuilder().WithAll<>()
//         }
//
//         private struct Initialized : IComponentData
//         {
//         }
//     }
// }


