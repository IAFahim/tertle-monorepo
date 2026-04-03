// <copyright file="ConditionAuthoringConditionDataEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Editor.Helpers;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Reaction.Authoring.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ConditionAuthoring.ConditionData))]
    public class ConditionAuthoringConditionDataEditor : ElementProperty
    {
        /// <inheritdoc/>
        protected override VisualElement? CreateElement(SerializedProperty property)
        {
            var cache = this.Cache<Cache>();

            switch (property.name)
            {
                case nameof(ConditionAuthoring.ConditionData.Name):
                {
                    return null;
                }

                case nameof(ConditionAuthoring.ConditionData.Condition):
                {
                    cache.ConditionProperty = property;

                    var field = CreatePropertyField(property, property.serializedObject);
                    field.RegisterValueChangeCallback(evt => SetTargetVisibility(evt.changedProperty, cache));
                    return field;
                }

                case nameof(ConditionAuthoring.ConditionData.Target):
                {
                    return cache.TargetField = CreatePropertyField(property, property.serializedObject);
                }

                case nameof(ConditionAuthoring.ConditionData.Operation):
                {
                    cache.OperationProperty = property;
                    var field = CreatePropertyField(property, property.serializedObject);
                    field.RegisterValueChangeCallback(evt => SetOperationVisibility(evt.changedProperty, cache));
                    return field;
                }

                case nameof(ConditionAuthoring.ConditionData.ComparisonMode):
                {
                    cache.ComparisonModeProperty = property;
                    var field = CreatePropertyField(property, property.serializedObject);
                    field.RegisterValueChangeCallback(_ =>
                    {
                        if (cache.OperationProperty != null)
                        {
                            SetOperationVisibility(cache.OperationProperty, cache);
                        }
                    });

                    return cache.ComparisonModeField = field;
                }

                case nameof(ConditionAuthoring.ConditionData.MinComparisonMode):
                {
                    cache.MinComparisonModeProperty = property;
                    var field = CreatePropertyField(property, property.serializedObject);
                    field.RegisterValueChangeCallback(_ =>
                    {
                        if (cache.OperationProperty != null)
                        {
                            SetOperationVisibility(cache.OperationProperty, cache);
                        }
                    });

                    return cache.MinComparisonModeField = field;
                }

                case nameof(ConditionAuthoring.ConditionData.MaxComparisonMode):
                {
                    cache.MaxComparisonModeProperty = property;
                    var field = CreatePropertyField(property, property.serializedObject);
                    field.RegisterValueChangeCallback(_ =>
                    {
                        if (cache.OperationProperty != null)
                        {
                            SetOperationVisibility(cache.OperationProperty, cache);
                        }
                    });

                    return cache.MaxComparisonModeField = field;
                }

                case nameof(ConditionAuthoring.ConditionData.Value):
                {
                    return cache.ValueField = CreatePropertyField(property, property.serializedObject);
                }

                case nameof(ConditionAuthoring.ConditionData.ValueMin):
                {
                    cache.ValueMinProperty = property;
                    return cache.ValueMinField = CreatePropertyField(property, property.serializedObject);
                }

                case nameof(ConditionAuthoring.ConditionData.ValueMax):
                {
                    cache.ValueMaxProperty = property;
                    return cache.ValueMaxField = CreatePropertyField(property, property.serializedObject);
                }

                case nameof(ConditionAuthoring.ConditionData.CustomValue):
                {
                    return cache.CustomValueField = new CustomComparisonElement(property);
                }

                case nameof(ConditionAuthoring.ConditionData.CustomValueMin):
                {
                    return cache.CustomValueMinField = new CustomComparisonElement(property);
                }

                case nameof(ConditionAuthoring.ConditionData.CustomValueMax):
                {
                    return cache.CustomValueMaxField = new CustomComparisonElement(property);
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
            SetTargetVisibility(cache.ConditionProperty!, cache);
            SetOperationVisibility(cache.OperationProperty!, cache);

            cache.ValueMinField!.RegisterValueChangeCallback(_ => this.RangeCheck(cache));
            cache.ValueMaxField!.RegisterValueChangeCallback(_ => this.RangeCheck(cache));
        }

        private static void SetTargetVisibility(SerializedProperty serializedProperty, Cache cache)
        {
            var condition = serializedProperty.objectReferenceValue as ConditionSchemaObject;
            var isVisible = condition == null || !condition.IsGlobal;
            ElementUtility.SetVisible(cache.TargetField!, isVisible);
        }

        private static void SetOperationVisibility(SerializedProperty serializedProperty, Cache cache)
        {
            var equality = (Equality)serializedProperty.enumValueIndex;
            var showSingle = equality != Equality.Any && equality != Equality.Between;
            var showBetween = equality == Equality.Between;

            SetVisible(cache.ComparisonModeField, showSingle);
            SetVisible(cache.ValueField, showSingle && IsConstant(cache.ComparisonModeProperty));
            SetVisible(cache.CustomValueField, showSingle && IsCustom(cache.ComparisonModeProperty));

            SetVisible(cache.MinComparisonModeField, showBetween);
            SetVisible(cache.MaxComparisonModeField, showBetween);
            SetVisible(cache.ValueMinField, showBetween && IsConstant(cache.MinComparisonModeProperty));
            SetVisible(cache.ValueMaxField, showBetween && IsConstant(cache.MaxComparisonModeProperty));
            SetVisible(cache.CustomValueMinField, showBetween && IsCustom(cache.MinComparisonModeProperty));
            SetVisible(cache.CustomValueMaxField, showBetween && IsCustom(cache.MaxComparisonModeProperty));

            if (equality == Equality.Any)
            {
                SetVisible(cache.ValueField, false);
                SetVisible(cache.ValueMinField, false);
                SetVisible(cache.ValueMaxField, false);
                SetVisible(cache.ComparisonModeField, false);
                SetVisible(cache.MinComparisonModeField, false);
                SetVisible(cache.MaxComparisonModeField, false);
                SetVisible(cache.CustomValueField, false);
                SetVisible(cache.CustomValueMinField, false);
                SetVisible(cache.CustomValueMaxField, false);
            }
        }

        private void RangeCheck(Cache cache)
        {
            if (cache.ValueMaxProperty!.intValue < cache.ValueMinProperty!.intValue)
            {
                cache.ValueMaxProperty.intValue = cache.ValueMinProperty.intValue;
                cache.ValueMaxProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetVisible(VisualElement? element, bool visible)
        {
            if (element != null)
            {
                ElementUtility.SetVisible(element, visible);
            }
        }

        private static bool IsConstant(SerializedProperty? property)
        {
            return property!.enumValueIndex == (int)ConditionAuthoring.ConditionData.ConditionComparisonMode.Constant;
        }

        private static bool IsCustom(SerializedProperty? property)
        {
            return property!.enumValueIndex == (int)ConditionAuthoring.ConditionData.ConditionComparisonMode.Custom;
        }

        private class Cache
        {
            public PropertyField? TargetField;
            public PropertyField? ValueField;
            public PropertyField? ValueMinField;
            public PropertyField? ValueMaxField;
            public PropertyField? ComparisonModeField;
            public PropertyField? MinComparisonModeField;
            public PropertyField? MaxComparisonModeField;
            public VisualElement? CustomValueField;
            public VisualElement? CustomValueMinField;
            public VisualElement? CustomValueMaxField;

            public SerializedProperty? ConditionProperty;
            public SerializedProperty? OperationProperty;
            public SerializedProperty? ValueMinProperty;
            public SerializedProperty? ValueMaxProperty;
            public SerializedProperty? ComparisonModeProperty;
            public SerializedProperty? MinComparisonModeProperty;
            public SerializedProperty? MaxComparisonModeProperty;
        }

        private sealed class CustomComparisonElement : VisualElement
        {
            private const string NoneOption = "None";

            private static readonly List<Type?> TypeOptions;
            private static readonly List<string> TypeNames;

            private readonly SerializedObject serializedObject;
            private readonly string propertyPath;
            private readonly DropdownField dropdown;
            private readonly VisualElement dataContainer;

            static CustomComparisonElement()
            {
                TypeOptions = new List<Type?> { null };
                TypeNames = new List<string> { NoneOption };

                var validTypes = TypeCache
                    .GetTypesDerivedFrom<ICustomComparison>()
                    .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType && t.GetConstructor(Type.EmptyTypes) != null)
                    .OrderBy(GetDisplayName);

                foreach (var type in validTypes)
                {
                    TypeOptions.Add(type);
                    TypeNames.Add(GetDisplayName(type));
                }
            }

            public CustomComparisonElement(SerializedProperty property)
            {
                this.serializedObject = property.serializedObject;
                this.propertyPath = property.propertyPath;

                var initialIndex = GetIndexForType(GetManagedReferenceType(property));

                this.dropdown = new DropdownField(property.displayName, TypeNames, initialIndex)
                {
                    name = property.propertyPath + ":CustomComparisonType",
                };
                this.dropdown.AddToClassList(DropdownField.alignedFieldUssClassName);

                this.dropdown.RegisterValueChangedCallback(this.OnTypeChanged);

                this.dataContainer = new VisualElement
                {
                    style =
                    {
                        marginLeft = 16f,
                        marginTop = 2f,
                    },
                };

                this.Add(this.dropdown);
                this.Add(this.dataContainer);

                this.TrackPropertyValue(property, _ => this.Refresh());
                this.Refresh();
            }

            private void OnTypeChanged(ChangeEvent<string> evt)
            {
                var newIndex = TypeNames.IndexOf(evt.newValue);
                if (newIndex < 0)
                {
                    return;
                }

                this.SetType(TypeOptions[newIndex]);
            }

            private void SetType(Type? type)
            {
                this.serializedObject.Update();

                var property = this.serializedObject.FindProperty(this.propertyPath);
                if (property == null)
                {
                    return;
                }

                var currentType = GetManagedReferenceType(property);
                if (currentType == type)
                {
                    return;
                }

                if (type == null)
                {
                    property.managedReferenceValue = null;
                }
                else
                {
                    property.managedReferenceValue = Activator.CreateInstance(type);
                }

                this.serializedObject.ApplyModifiedProperties();
                this.Refresh();
            }

            private void Refresh()
            {
                this.serializedObject.UpdateIfRequiredOrScript();

                var property = this.serializedObject.FindProperty(this.propertyPath);
                var currentType = GetManagedReferenceType(property);
                var index = GetIndexForType(currentType);

                this.dropdown.choices = TypeNames;
                this.dropdown.SetValueWithoutNotify(TypeNames[index]);

                this.dataContainer.Clear();

                if (currentType == null)
                {
                    return;
                }

                foreach (var child in SerializedHelper.GetChildren(property))
                {
                    var childField = PropertyUtil.CreateProperty(child, this.serializedObject);
                    this.dataContainer.Add(childField);
                }
            }

            private static int GetIndexForType(Type? type)
            {
                if (type == null)
                {
                    return 0;
                }

                var index = TypeOptions.IndexOf(type);
                if (index >= 0)
                {
                    return index;
                }

                TypeOptions.Add(type);
                TypeNames.Add(GetDisplayName(type));
                return TypeOptions.Count - 1;
            }

            private static Type? GetManagedReferenceType(SerializedProperty property)
            {
                var fullTypeName = property.managedReferenceFullTypename;
                if (string.IsNullOrEmpty(fullTypeName))
                {
                    return null;
                }

                var parts = fullTypeName.Split(' ');
                if (parts.Length != 2)
                {
                    return null;
                }

                var assemblyQualifiedName = parts[1] + ", " + parts[0];
                return Type.GetType(assemblyQualifiedName);
            }

            private static string GetDisplayName(Type type)
            {
                var inspectorName = type.GetCustomAttribute<InspectorNameAttribute>();
                return inspectorName != null ? inspectorName.displayName : ObjectNames.NicifyVariableName(type.Name);
            }
        }
    }
}
