// <copyright file="BridgeObjectConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data
{
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using UnityEngine;

    [Configurable]
    public static class BridgeObjectConfig
    {
        [ConfigVar("bridge.hide-flags", true, "Hide the pooled objects")]
        private static readonly SharedStatic<bool> Hide = SharedStatic<bool>.GetOrCreate<bool, HideType>();

        public static HideFlags Flags => Hide.Data ? HideFlags.HideAndDontSave : HideFlags.DontSave | HideFlags.NotEditable;

        private struct HideType
        {
        }
    }
}
