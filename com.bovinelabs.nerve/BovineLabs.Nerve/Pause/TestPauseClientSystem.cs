// // <copyright file="TestPauseClientSystem.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Nerve.Pause
// {
//     using BovineLabs.Core;
//     using BovineLabs.Core.Pause;
//     using BovineLabs.Nerve.Data.Pause;
//     using BovineLabs.Nerve.Rpc;
//     using Unity.Entities;
//     using Unity.NetCode;
//     using UnityEngine;
//     using UnityEngine.InputSystem;
//
//     [WorldSystemFilter(Worlds.ClientLocal)]
//     [UpdateInGroup(typeof(RpcSendSystemGroup))]
//     public partial class TestPauseClientSystem : SystemBase, IUpdateWhilePaused
//     {
//         private bool pause;
//
//         /// <inheritdoc/>
//         protected override void OnCreate()
//         {
//             this.RequireForUpdate<NetworkStreamInGame>();
//         }
//
//         /// <inheritdoc/>
//         protected override void OnUpdate()
//         {
//             if (Keyboard.current.spaceKey.wasReleasedThisFrame)
//             {
//                 var entity = SystemAPI.GetSingletonEntity<NetworkStreamInGame>();
//
//                 this.pause = !this.pause;
//
//                 Debug.Log("Send Pause");
//                 NetUtility.CreateRPC(ref this.CheckedStateRef, entity, new PauseRequest { Value = this.pause });
//             }
//         }
//     }
// }
