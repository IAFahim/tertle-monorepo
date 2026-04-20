// <copyright file="InputActionMapMenuSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input
{
    using BovineLabs.Core;
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    [WorldSystemFilter(Worlds.Menu)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    public partial class InputActionMapMenuSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            var inputAsset = InputCommonSettings.I.Asset;

            var logger = SystemAPI.GetSingleton<BLLogger>();

            if (inputAsset == null)
            {
                logger.LogError("Input asset not setup");
                this.Enabled = false;
                return;
            }

            // Disable all action maps by default
            inputAsset.Disable();

            // Enable defaults
            foreach (var actionMap in InputCommonSettings.I.MenuEnabled)
            {
                InputActionMapSystem.SetInputEnable(inputAsset, actionMap, true, logger);
            }
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
        }
    }
}
