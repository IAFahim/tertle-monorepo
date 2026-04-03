// <copyright file="StatAuthoringEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Essence.Authoring;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(StatAuthoring))]
    public class StatAuthoringEditor : ElementEditor
    {
        private SerializedProperty statsCanBeModifiedProperty;
        private SerializedProperty addIntrinsicsProperty;

        private Label warning;
        private PropertyField initializeField;
        private PropertyField intrinsicDefaultOverridesField;
        private PropertyField intrinsicGroupDefaultOverridesField;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case nameof(StatAuthoring.Initialize):
                    return this.initializeField = CreatePropertyField(property);

                case nameof(StatAuthoring.StatsCanBeModified):
                {
                    this.statsCanBeModifiedProperty = property;
                    var statsCanBeModifiedField = CreatePropertyField(property);
                    statsCanBeModifiedField.RegisterValueChangeCallback(_ => this.UpdateVisibility());
                    return statsCanBeModifiedField;
                }

                case nameof(StatAuthoring.AddIntrinsics):
                    this.addIntrinsicsProperty = property;
                    var addIntrinsicsField = CreatePropertyField(property);
                    addIntrinsicsField.RegisterValueChangeCallback(_ => this.UpdateIntrinsicVisibility());
                    addIntrinsicsField.label = "Enable";
                    return addIntrinsicsField;

                case nameof(StatAuthoring.IntrinsicDefaults):
                    return this.intrinsicDefaultOverridesField = CreatePropertyField(property);

                case nameof(StatAuthoring.IntrinsicDefaultGroups):
                    return this.intrinsicGroupDefaultOverridesField = CreatePropertyField(property);

                default:
                    return base.CreateElement(property);
            }
        }

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.UpdateVisibility();
            this.UpdateIntrinsicVisibility();
        }

        private void UpdateVisibility()
        {
            ElementUtility.SetVisible(this.initializeField, !this.statsCanBeModifiedProperty.boolValue);
        }

        private void UpdateIntrinsicVisibility()
        {
            var visible = this.addIntrinsicsProperty.boolValue;

            ElementUtility.SetVisible(this.intrinsicDefaultOverridesField, visible);
            ElementUtility.SetVisible(this.intrinsicGroupDefaultOverridesField, visible);
        }
    }
}
