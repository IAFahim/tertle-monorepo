// <copyright file="InputCommonSettingsTestScope.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Tests.Systems
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine.InputSystem;

    internal sealed class InputCommonSettingsTestScope : IDisposable
    {
        private static readonly FieldInfo AssetField = typeof(InputCommonSettings).GetField("asset", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo DefaultEnabledField =
            typeof(InputCommonSettings).GetField("defaultEnabled", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly FieldInfo MenuEnabledField =
            typeof(InputCommonSettings).GetField("menuEnabled", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly FieldInfo CursorPositionField =
            typeof(InputCommonSettings).GetField("cursorPosition", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly FieldInfo SettingsField =
            typeof(InputCommonSettings).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly FieldInfo AllSettingsField =
            typeof(InputCommonSettings).GetField("allSettings", BindingFlags.Instance | BindingFlags.NonPublic)!;

#if UNITY_EDITOR || BL_DEBUG
        private static readonly FieldInfo DebugSettingsField =
            typeof(InputCommonSettings).GetField("debugSettings", BindingFlags.Instance | BindingFlags.NonPublic)!;
#endif

        private static readonly MethodInfo OnInitializeMethod =
            typeof(InputCommonSettings).GetMethod("OnInitialize", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private readonly InputCommonSettings settings;
        private readonly InputActionAsset previousAsset;
        private readonly string[] previousDefaultEnabled;
        private readonly string[] previousMenuEnabled;
        private readonly InputActionReference previousCursorPosition;
        private readonly List<IInputSettings> previousSettings;
        private readonly Dictionary<Type, IInputSettings> previousAllSettings;
#if UNITY_EDITOR || BL_DEBUG
        private readonly List<IInputSettings> previousDebugSettings;
#endif

        public InputCommonSettingsTestScope()
        {
            this.settings = InputCommonSettings.I;
            this.previousAsset = (InputActionAsset)AssetField.GetValue(this.settings);
            this.previousDefaultEnabled = (string[])DefaultEnabledField.GetValue(this.settings);
            this.previousMenuEnabled = (string[])MenuEnabledField.GetValue(this.settings);
            this.previousCursorPosition = (InputActionReference)CursorPositionField.GetValue(this.settings);
            this.previousSettings = (List<IInputSettings>)SettingsField.GetValue(this.settings);
            this.previousAllSettings = new Dictionary<Type, IInputSettings>((Dictionary<Type, IInputSettings>)AllSettingsField.GetValue(this.settings));
#if UNITY_EDITOR || BL_DEBUG
            this.previousDebugSettings = (List<IInputSettings>)DebugSettingsField.GetValue(this.settings);
#endif
        }

        public void Set(
            InputActionAsset asset,
            string[] defaultEnabled = null,
            string[] menuEnabled = null,
            InputActionReference cursorPosition = null,
            List<IInputSettings> settings = null,
            List<IInputSettings> debugSettings = null)
        {
            AssetField.SetValue(this.settings, asset);
            DefaultEnabledField.SetValue(this.settings, defaultEnabled ?? Array.Empty<string>());
            MenuEnabledField.SetValue(this.settings, menuEnabled ?? Array.Empty<string>());
            CursorPositionField.SetValue(this.settings, cursorPosition);
            SettingsField.SetValue(this.settings, settings ?? new List<IInputSettings>());
#if UNITY_EDITOR || BL_DEBUG
            DebugSettingsField.SetValue(this.settings, debugSettings ?? new List<IInputSettings>());
#endif
        }

        public void Initialize()
        {
            OnInitializeMethod.Invoke(this.settings, null);
        }

        public void Dispose()
        {
            AssetField.SetValue(this.settings, this.previousAsset);
            DefaultEnabledField.SetValue(this.settings, this.previousDefaultEnabled);
            MenuEnabledField.SetValue(this.settings, this.previousMenuEnabled);
            CursorPositionField.SetValue(this.settings, this.previousCursorPosition);
            SettingsField.SetValue(this.settings, this.previousSettings);
#if UNITY_EDITOR || BL_DEBUG
            DebugSettingsField.SetValue(this.settings, this.previousDebugSettings);
#endif
            var allSettings = (Dictionary<Type, IInputSettings>)AllSettingsField.GetValue(this.settings);
            allSettings.Clear();
            foreach (var (type, value) in this.previousAllSettings)
            {
                allSettings.Add(type, value);
            }
        }
    }
}
