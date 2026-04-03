// <copyright file="ActionStatAuthoringDataEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Essence.Authoring.Actions;
    using BovineLabs.Essence.Data.Actions;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ActionStatAuthoring.Data))]
    public class ActionStatAuthoringDataEditor : ElementProperty
    {
        /// <inheritdoc/>
        protected override string GetDisplayName(SerializedProperty property)
        {
            var obj = property.FindPropertyRelative(nameof(ActionStatAuthoring.Data.StatSchema)).objectReferenceValue;
            return obj ? obj.name : "Null";
        }

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            var cache = this.Cache<Cache>();

            switch (property.name)
            {
                case nameof(ActionStatAuthoring.Data.ValueType):
                    cache.ValueTypeProperty = property;
                    return cache.ValueTypeField = CreatePropertyField(property);

                case nameof(ActionStatAuthoring.Data.Fixed):
                    cache.FixedField = CreatePropertyField(property);
                    return cache.FixedField;

                case nameof(ActionStatAuthoring.Data.Linear):
                    return cache.LinearField = CreatePropertyField(property);

                case nameof(ActionStatAuthoring.Data.Range):
                    return cache.RangeField = CreatePropertyField(property);
            }

            return base.CreateElement(property);
        }

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            var cache = this.Cache<Cache>();
            cache.ValueTypeField.RegisterValueChangeCallback(_ => ToggleVisibility(cache));
            ToggleVisibility(cache);
        }

        private static void ToggleVisibility(Cache cache)
        {
            var type = (StatValueType)cache.ValueTypeProperty.enumValueIndex;

            ElementUtility.SetVisible(cache.FixedField, type == StatValueType.Fixed);
            ElementUtility.SetVisible(cache.LinearField, type == StatValueType.Linear);
            ElementUtility.SetVisible(cache.RangeField, type == StatValueType.Range);
        }

        private class Cache
        {
            public SerializedProperty ValueTypeProperty;

            public PropertyField ValueTypeField;
            public PropertyField FixedField;
            public PropertyField LinearField;
            public PropertyField RangeField;
        }
    }
}