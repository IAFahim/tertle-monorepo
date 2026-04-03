// <copyright file="StatSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.ObjectManagement;
    using BovineLabs.Essence.Authoring;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(EssenceSettings))]
    public class StatSettingsEditor : ElementEditor
    {
        /// <inheritdoc />
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                "statSchemas" => new AssetCreator<StatSchemaObject>(this.serializedObject, property).Element,
                "intrinsicSchemas" => new AssetCreator<IntrinsicSchemaObject>(this.serializedObject, property).Element,
                _ => base.CreateElement(property),
            };
        }
    }
}
