// <copyright file="InputActionMapSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input
{
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Groups;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.InputSystem;

    [UpdateInGroup(typeof(InputSystemGroup), OrderFirst = true)]
    public partial class InputActionMapSystem : SystemBase
    {
        protected override void OnCreate()
        {
            this.EntityManager.CreateEntity<InputActionMapEnable>("Input Action Map Enable");
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            var inputAsset = InputCommonSettings.I.Asset;

            if (inputAsset == null)
            {
                SystemAPI.GetSingleton<BLLogger>().LogError("Input asset not setup");
                this.Enabled = false;
                return;
            }

            // Disable all action maps by default
            inputAsset.Disable();

            // Enable defaults
            var logger = SystemAPI.GetSingleton<BLLogger>();
            foreach (var actionMap in InputCommonSettings.I.DefaultEnabled)
            {
                SetInputEnable(inputAsset, actionMap, true, logger);
            }
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var enables = SystemAPI.GetSingletonBuffer<InputActionMapEnable>();
            if (enables.Length == 0)
            {
                return;
            }

            var inputAsset = InputCommonSettings.I.Asset;
            if (inputAsset == null)
            {
                return;
            }

            var logger = SystemAPI.GetSingleton<BLLogger>();

            foreach (var state in enables.AsNativeArray())
            {
                SetInputEnable(inputAsset, state.Input, state.Enable, logger);
            }

            enables.Clear();
        }

        public static void SetInputEnable(UnityObjectRef<InputActionAsset> inputAsset, FixedString32Bytes input, bool enable, BLLogger logger)
        {
            var actionMap = inputAsset.Value.FindActionMap(input.ToString());
            if (actionMap == null)
            {
                logger.LogWarning($"Unable to find action map of name {input}");
                return;
            }

            if (enable)
            {
                actionMap.Enable();
            }
            else
            {
                actionMap.Disable();
            }
        }
    }
}
