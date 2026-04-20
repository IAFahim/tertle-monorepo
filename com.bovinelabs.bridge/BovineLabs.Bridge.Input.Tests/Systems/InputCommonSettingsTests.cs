// <copyright file="InputCommonSettingsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Tests.Systems
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class InputCommonSettingsTests
    {
        [Test]
        public void Initialize_WithConfiguredSettings_MapsPropertiesAndLookup()
        {
            using var scope = new InputCommonSettingsTestScope();

            var runtimeSettings = new RuntimeInputSettings();
            var debugSettings = new DebugInputSettings();
            var inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            var cursorAction = inputAsset.AddActionMap("Gameplay").AddAction("CursorPosition", InputActionType.Value);
            var cursorReference = InputActionReference.Create(cursorAction);

            try
            {
                scope.Set(
                    inputAsset,
                    defaultEnabled: new[] { "Gameplay" },
                    menuEnabled: new[] { "Menu" },
                    cursorPosition: cursorReference,
                    settings: new List<IInputSettings> { runtimeSettings },
                    debugSettings: new List<IInputSettings> { debugSettings });
                scope.Initialize();

                var settings = InputCommonSettings.I;

                Assert.AreSame(inputAsset, settings.Asset);
                Assert.AreSame(cursorReference, settings.CursorPosition);
                Assert.AreEqual("Gameplay", settings.DefaultEnabled[0]);
                Assert.AreEqual("Menu", settings.MenuEnabled[0]);
                Assert.AreEqual(1, settings.Settings.Count);

                Assert.IsTrue(settings.TryGetSettings<RuntimeInputSettings>(out var runtimeResult));
                Assert.AreSame(runtimeSettings, runtimeResult);

                Assert.IsTrue(settings.TryGetSettings<DebugInputSettings>(out var debugResult));
                Assert.AreSame(debugSettings, debugResult);
            }
            finally
            {
                Object.DestroyImmediate(inputAsset);
            }
        }

        [Test]
        public void TryGetSettings_WhenTypeMissing_ReturnsFalse()
        {
            using var scope = new InputCommonSettingsTestScope();
            var inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();

            try
            {
                scope.Set(inputAsset, settings: new List<IInputSettings>());
                scope.Initialize();

                var found = InputCommonSettings.I.TryGetSettings<RuntimeInputSettings>(out var setting);

                Assert.IsFalse(found);
                Assert.IsNull(setting);
            }
            finally
            {
                Object.DestroyImmediate(inputAsset);
            }
        }

        private sealed class RuntimeInputSettings : IInputSettings
        {
            public void Bake(IBakerWrapper baker)
            {
            }
        }

        private sealed class DebugInputSettings : IInputSettings
        {
            public void Bake(IBakerWrapper baker)
            {
            }
        }
    }
}
