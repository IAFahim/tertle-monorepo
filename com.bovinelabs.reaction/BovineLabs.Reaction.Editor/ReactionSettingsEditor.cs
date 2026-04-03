// <copyright file="ReactionSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.ObjectManagement;
    using BovineLabs.Reaction.Authoring.Conditions;
    using BovineLabs.Reaction.Authoring.Core;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ReactionSettings))]
    public class ReactionSettingsEditor : ElementEditor
    {
        /// <inheritdoc />
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                "conditionEvents" => new AssetCreator<ConditionEventObject>(this.serializedObject, property).Element,
                _ => CreatePropertyField(property),
            };
        }
    }
}
