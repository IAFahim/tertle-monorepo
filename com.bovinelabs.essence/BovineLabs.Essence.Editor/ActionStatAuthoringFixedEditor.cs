// <copyright file="ActionStatAuthoringFixedEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Essence.Authoring.Actions;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ActionStatAuthoring.Fixed))]
    public class ActionStatAuthoringFixedEditor : ElementProperty
    {
        /// <inheritdoc/>
        protected override ParentTypes ParentType => ParentTypes.Label;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            var editor = this.Cache<StatTypeEditor>();

            switch (property.name)
            {
                case nameof(ActionStatAuthoring.Fixed.ModifyType):
                {
                    return editor.CreateModifierField(property);
                }

                case nameof(ActionStatAuthoring.Fixed.Value):
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