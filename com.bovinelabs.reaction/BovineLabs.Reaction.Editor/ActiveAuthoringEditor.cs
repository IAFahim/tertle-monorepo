// <copyright file="ActiveAuthoringEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Reaction.Authoring.Active;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ActiveAuthoring))]
    public class ActiveAuthoringEditor : ElementProperty
    {
        protected override VisualElement? CreateElement(SerializedProperty property)
        {
            var cache = this.Cache<Cache>();

            switch (property.name)
            {
                case "duration":
                {
                    cache.DurationProperty = property;

                    var durationField = CreatePropertyField(property, property.serializedObject);
                    durationField.RegisterValueChangeCallback(evt =>
                    {
                        ElementUtility.SetVisible(cache.CancellableField, evt.changedProperty.floatValue > 0);
                        UpdateCooldownAfterDuration(cache);
                    });

                    return durationField;
                }

                case "cooldown":
                {
                    cache.CooldownProperty = property;
                    var cooldownField = CreatePropertyField(property, property.serializedObject);
                    cooldownField.RegisterValueChangeCallback(_ => UpdateCooldownAfterDuration(cache));
                    return cooldownField;
                }

                case "cancellable":
                {
                    return cache.CancellableField = CreatePropertyField(property, property.serializedObject);
                }

                case "cooldownAfterDuration":
                    return cache.CooldownAfterDuration = CreatePropertyField(property, property.serializedObject);
            }

            return base.CreateElement(property);
        }

        private static void UpdateCooldownAfterDuration(Cache cache)
        {
            var isVisible = cache.CooldownProperty.floatValue > 0 && cache.DurationProperty.floatValue > 0;
            ElementUtility.SetVisible(cache.CooldownAfterDuration, isVisible);
        }

        private class Cache
        {
            public PropertyField CancellableField = null!;
            public PropertyField CooldownAfterDuration = null!;

            public SerializedProperty DurationProperty = null!;
            public SerializedProperty CooldownProperty = null!;
        }
    }
}
