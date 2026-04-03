// <copyright file="ActionStatAuthoringRangeEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Essence.Authoring.Actions;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ActionStatAuthoring.Range))]
    public class ActionStatAuthoringRangeEditor : ElementProperty
    {
        /// <inheritdoc/>
        protected override ParentTypes ParentType => ParentTypes.Label;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            var editor = this.Cache<Cache>();

            switch (property.name)
            {
                case nameof(ActionStatAuthoring.Range.ModifyType):
                {
                    var field = editor.Min.CreateModifierField(property);
                    editor.Max.SetModifierProperty(field, property);
                    return field;
                }

                case nameof(ActionStatAuthoring.Range.Min):
                {
                    return editor.Min.CreateValue(property);
                }

                case nameof(ActionStatAuthoring.Range.Max):
                {
                    return editor.Max.CreateValue(property);
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
            cache.Min.PostElementCreation();
            cache.Max.PostElementCreation();
        }

        private class Cache
        {
            public readonly StatTypeEditor Min = new();
            public readonly StatTypeEditor Max = new();
        }
    }
}