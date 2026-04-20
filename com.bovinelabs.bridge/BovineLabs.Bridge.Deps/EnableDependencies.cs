// <copyright file="EnableDependencies.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Deps
{
    using System;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEngine;
#if BL_CORE
    using BovineLabs.Core;
    using BovineLabs.Core.Editor.Settings;
    using EditorSettings = BovineLabs.Core.Editor.Settings.EditorSettings;
#endif

    [InitializeOnLoad]
    internal static class EnableDependencies
    {
        private const string CorePackageName = "com.bovinelabs.core";
        private const string CorePackageGitUrl = "https://gitlab.com/tertle/com.bovinelabs.core.git#1.6.0";
        private const string CorePackageInstallAttemptedKey = "BovineLabs.CorePackageInstallFailed";

        static EnableDependencies()
        {
            _ = EnsureDependencies();
        }

        private static async Task EnsureDependencies()
        {
            if (await SetupCorePackage())
            {
                SetupCore();
            }
        }

        private static async Task<bool> SetupCorePackage()
        {
            if (SessionState.GetBool(CorePackageInstallAttemptedKey, false))
            {
                return false;
            }

            SessionState.SetBool(CorePackageInstallAttemptedKey, true);

            var listRequest = Client.List();
            while (!listRequest.IsCompleted)
            {
                await Task.Yield();
            }

            if (listRequest.Status == StatusCode.Success)
            {
                foreach (var package in listRequest.Result)
                {
                    if (string.Equals(package.name, CorePackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            else if (listRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError($"Failed to install {CorePackageName} from {CorePackageGitUrl}. Error: {listRequest.Error.message}");
                return false;
            }

            var addRequest = Client.Add(CorePackageGitUrl);
            while (!addRequest.IsCompleted)
            {
                await Task.Yield();
            }

            if (addRequest.Status == StatusCode.Success)
            {
                Debug.LogWarning("Installed BovineLab Core dependency.");
                return true;
            }

            Debug.LogError($"Failed to install {CorePackageName} from {CorePackageGitUrl}. Error: {addRequest.Error.message}");

            return false;
        }

        private static void SetupCore()
        {
#if BL_CORE
            var settings = EditorSettingsUtility.GetSettings<EditorSettings>();
            settings.EnsureDefines(new[] { "BL_CORE_EXTENSIONS" });

            BLGlobalLogger.LogWarningString("Enabling BovineLab Core extension as BovineLabs Bridge depends on it. Check BovineLabs->Manager menu to manage other extensions.");
#endif
        }
    }
}
