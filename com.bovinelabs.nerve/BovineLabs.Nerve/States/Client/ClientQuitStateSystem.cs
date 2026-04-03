// <copyright file="ClientQuitStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States.Client
{
    using System.Threading.Tasks;
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Pause;
    using BovineLabs.Core.States;
    using BovineLabs.Nerve.Data.States;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;
#if UNITY_EDITOR
#endif

    [UpdateInGroup(typeof(ClientStateSystemGroup))]
    public partial class ClientQuitStateSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnCreate()
        {
            StateAPI.Register<ClientState, StateQuit, ClientStates>(ref this.CheckedStateRef, ClientStates.Quit);
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            PauseGame.Pause(ref this.CheckedStateRef, true);

            _ = this.DestroyWorld(SystemAPI.HasSingleton<Exit>());
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            // NO-OP
        }

        private async Task DestroyWorld(bool exit)
        {
            // Can't exit on mobile or console
            exit &= !(Application.isMobilePlatform || Application.isConsolePlatform);

            // We can't destroy a world from inside the world update so we yield which calls back outside the world update
            await Task.Yield();

            if (exit)
            {
                Application.Quit();
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#endif
            }
            else
            {
                if (BovineLabsBootstrap.ServiceWorld == null)
                {
                    Debug.LogError("Service world not setup");
                    return;
                }

                var em = BovineLabsBootstrap.ServiceWorld.EntityManager;
                em.GetSingleton<BLLogger>(false).LogDebug($"GameState set to {ClientStates.Init}");
                var state = ClientStates.NameToKey(ClientStates.Init);
                em.SetSingleton(new ClientState { Value = new BitArray256 { [state] = true } });
            }
        }

        private struct StateQuit : IComponentData
        {
        }
    }
}
