// <copyright file="StatDefaultEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Essence.Authoring;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(StatModifierAuthoring))]
    public class StatModifierAuthoringEditor : ElementProperty
    {
        /// <inheritdoc/>
        protected override string GetDisplayName(SerializedProperty property)
        {
            var obj = property.FindPropertyRelative(nameof(StatModifierAuthoring.Stat)).objectReferenceValue;
            return obj ? obj.name : "Null";
        }

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            var editor = this.Cache<StatTypeEditor>();

            switch (property.name)
            {
                case nameof(StatModifierAuthoring.ModifyType):
                {
                    return editor.CreateModifierField(property);
                }

                case nameof(StatModifierAuthoring.Value):
                {
                    return editor.CreateValue(property);
                }

                default:
                {
                    return base.CreateElement(property);
                }
            }
        }

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.Cache<StatTypeEditor>().PostElementCreation();
        }
    }
}