// <copyright file="ActionStatAuthoringLinearEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Essence.Authoring.Actions;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ActionStatAuthoring.Linear))]
    public class ActionStatAuthoringLinearEditor : ElementProperty
    {
        /// <inheritdoc/>
        protected override ParentTypes ParentType => ParentTypes.Label;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            var editor = this.Cache<Cache>();

            switch (property.name)
            {
                case nameof(ActionStatAuthoring.Linear.ModifyType):
                {
                    var field = editor.ToMin.CreateModifierField(property);
                    editor.ToMax.SetModifierProperty(field, property);
                    return field;
                }

                case nameof(ActionStatAuthoring.Linear.ToMin):
                {
                    return editor.ToMin.CreateValue(property);
                }

                case nameof(ActionStatAuthoring.Linear.ToMax):
                {
                    return editor.ToMax.CreateValue(property);
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
            var cache = this.Cache<Cache>();
            cache.ToMin.PostElementCreation();
            cache.ToMax.PostElementCreation();
        }

        private class Cache
        {
            public readonly StatTypeEditor ToMin = new();
            public readonly StatTypeEditor ToMax = new();
        }
    }
}