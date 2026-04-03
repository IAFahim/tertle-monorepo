// // <copyright file="PauseServerSystem.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Net.Pause
// {
//     using BovineLabs.Core;
//     using BovineLabs.Core.ObjectManagement;
//     using BovineLabs.Core.Pause;
//     using BovineLabs.Net.Data.App;
//     using BovineLabs.Net.Data.Pause;
//     using Unity.Burst;
//     using Unity.Entities;
//
//     [WorldSystemFilter(Worlds.ServerLocal)]
//     public partial struct PauseServerSystem : ISystem, IUpdateWhilePaused
//     {
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<ServerPrefabs>();
//         }
//
//         /// <inheritdoc />
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             // var nt = SystemAPI.GetSingleton<NetworkTime>();
//             // var debug = SystemAPI.GetSingleton<BLDebug>();
//             // debug.Info($"{nt.ServerTick.ToFixedString()} {nt.InterpolationTick.ToFixedString()}");
//
//             var isPaused = false;
//             var isPresentationPaused = false;
//
//             foreach (var paused in SystemAPI.Query<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems))
//             {
//                 isPaused = true;
//                 isPresentationPaused |= paused.PausePresentation;
//             }
//
//             var isCurrentlyPaused = SystemAPI.TryGetSingletonEntity<ClientPaused>(out var entity);
//
//             if (isCurrentlyPaused && !isPaused)
//             {
//                 state.EntityManager.DestroyEntity(entity);
//             }
//             else if (!isCurrentlyPaused && isPaused)
//             {
//                 state.Dependency.Complete(); // we access ObjectDefinitionRegistry on main thread
//                 var prefabId = SystemAPI.GetSingleton<ServerPrefabs>().PauseGhost;
//                 var prefab = SystemAPI.GetSingleton<ObjectDefinitionRegistry>()[prefabId];
//
//                 if (prefab != Entity.Null) // TODO fix this needed
//                 {
//                     entity = state.EntityManager.Instantiate(prefab);
//                     SystemAPI.SetComponent(entity, new ClientPaused { PausePresentation = isPresentationPaused });
//                 }
//             }
//             else if (isCurrentlyPaused)
//             {
//                 SystemAPI.SetComponent(entity, new ClientPaused { PausePresentation = isPresentationPaused });
//             }
//         }
//     }
// }
