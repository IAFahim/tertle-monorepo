// <copyright file="DOTSClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Editor
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.Helpers;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Authoring;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(DOTSClip), true)]
    public class DOTSClipEditor : ElementEditor
    {
        private const string OverridePrefix = "override";

        private readonly Dictionary<string, string> overrideToValuePaths = new();
        private readonly HashSet<string> valuePaths = new();

        protected override bool PreElementCreation(VisualElement root)
        {
            this.CacheOverridePairs();
            return base.PreElementCreation(root);
        }

        protected override VisualElement CreateElement(SerializedProperty property)
        {
            if (this.overrideToValuePaths.TryGetValue(property.propertyPath, out var valuePath))
            {
                return new ToggleOption(this.serializedObject, property.propertyPath, valuePath);
            }

            if (this.valuePaths.Contains(property.propertyPath))
            {
                return null;
            }

            return base.CreateElement(property);
        }

        private void CacheOverridePairs()
        {
            this.overrideToValuePaths.Clear();
            this.valuePaths.Clear();

            foreach (var property in SerializedHelper.IterateAllChildren(this.serializedObject, false))
            {
                if (property.propertyType != SerializedPropertyType.Boolean)
                {
                    continue;
                }

                if (!this.TryGetValuePath(property, out var valuePath))
                {
                    continue;
                }

                this.overrideToValuePaths[property.propertyPath] = valuePath;
                this.valuePaths.Add(valuePath);
            }
        }

        private bool TryGetValuePath(SerializedProperty property, out string valuePath)
        {
            valuePath = string.Empty;

            var propertyName = property.name;
            if (string.IsNullOrEmpty(propertyName))
            {
                return false;
            }

            var startIndex = 0;
            var leading = string.Empty;
            if (propertyName.StartsWith("m_", StringComparison.Ordinal))
            {
                leading = "m_";
                startIndex = 2;
            }
            else if (propertyName.StartsWith("_", StringComparison.Ordinal))
            {
                leading = "_";
                startIndex = 1;
            }

            if (propertyName.Length - startIndex <= OverridePrefix.Length)
            {
                return false;
            }

            var prefix = propertyName.Substring(startIndex, OverridePrefix.Length);
            if (!prefix.Equals(OverridePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var trimmed = propertyName[(startIndex + OverridePrefix.Length)..];
            if (trimmed.Length == 0)
            {
                return false;
            }

            var lowerName = char.ToLowerInvariant(trimmed[0]) + trimmed[1..];
            var upperName = char.ToUpperInvariant(trimmed[0]) + trimmed[1..];

            var preferUpper = char.IsUpper(prefix[0]);
            var firstCandidate = preferUpper ? upperName : lowerName;
            var secondCandidate = preferUpper ? lowerName : upperName;

            var path = property.propertyPath;
            var lastDot = path.LastIndexOf('.');
            var basePath = lastDot >= 0 ? path[..(lastDot + 1)] : string.Empty;

            var firstPath = basePath + leading + firstCandidate;
            if (this.serializedObject.FindProperty(firstPath) != null)
            {
                valuePath = firstPath;
                return true;
            }

            if (!string.Equals(firstCandidate, secondCandidate, StringComparison.Ordinal))
            {
                var secondPath = basePath + leading + secondCandidate;
                if (this.serializedObject.FindProperty(secondPath) != null)
                {
                    valuePath = secondPath;
                    return true;
                }
            }

            return false;
        }
    }
}
