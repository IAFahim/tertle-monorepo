// <copyright file="StatTypeEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Essence.Authoring;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class StatTypeEditor
    {
        public SerializedProperty ModifyTypeProperty { get; private set; }

        public SerializedProperty ValueProperty { get; private set; }

        public PropertyField ModifyTypeField { get; private set; }

        public IntegerField ValueIntField { get; private set; }

        public FloatField ValueFloatField { get; private set; }

        public PropertyField CreateModifierField(SerializedProperty property)
        {
            var field = PropertyUtil.CreateProperty(property, property.serializedObject);
            this.SetModifierProperty(field, property);
            return field;
        }

        public VisualElement CreateValue(SerializedProperty property)
        {
            const float maxMulti = 128;

            this.ValueProperty = property;

            this.ValueIntField = new IntegerField(property.displayName);
            this.ValueIntField.AddToClassList(IntegerField.alignedFieldUssClassName);
            this.ValueIntField.RegisterValueChangedCallback(evt =>
            {
                switch (evt.newValue)
                {
                    case < 0:
                        this.ValueIntField.value = 0;
                        break;
                    case > short.MaxValue:
                        this.ValueIntField.value = short.MaxValue;
                        break;
                    default:
                        this.ValueFloatField.SetValueWithoutNotify(Mathf.Min(evt.newValue, maxMulti));

                        this.ValueProperty.floatValue = evt.newValue;
                        this.ValueProperty.serializedObject.ApplyModifiedProperties();
                        break;
                }
            });

            this.ValueFloatField = new FloatField(property.displayName);
            this.ValueFloatField.AddToClassList(FloatField.alignedFieldUssClassName);
            this.ValueFloatField.RegisterValueChangedCallback(evt =>
            {
                switch (evt.newValue)
                {
                    case < 0:
                        this.ValueFloatField.value = 0;
                        break;
                    case > maxMulti:
                        this.ValueFloatField.value = maxMulti;
                        break;
                    default:
                        this.ValueIntField.SetValueWithoutNotify(Mathf.RoundToInt(evt.newValue));

                        this.ValueProperty.floatValue = evt.newValue;
                        this.ValueProperty.serializedObject.ApplyModifiedProperties();
                        break;
                }
            });

            var parent = new VisualElement();
            parent.Add(this.ValueIntField);
            parent.Add(this.ValueFloatField);
            return parent;
        }

        public void PostElementCreation()
        {
            this.ModifyTypeField.RegisterValueChangeCallback(_ => this.UpdateVisibility());
            this.UpdateVisibility();
        }

        public void SetModifierProperty(PropertyField field, SerializedProperty property)
        {
            this.ModifyTypeField = field;
            this.ModifyTypeProperty = property;
        }

        private void UpdateVisibility()
        {
            var type = (StatAuthoringType)this.ModifyTypeProperty.enumValueIndex;

            if (type is StatAuthoringType.Added or StatAuthoringType.Subtracted)
            {
                ElementUtility.SetVisible(this.ValueIntField, true);
                ElementUtility.SetVisible(this.ValueFloatField, false);

                this.ValueIntField.value = Mathf.RoundToInt(this.ValueProperty.floatValue);
            }
            else
            {
                ElementUtility.SetVisible(this.ValueIntField, false);
                ElementUtility.SetVisible(this.ValueFloatField, true);

                this.ValueFloatField.value = this.ValueProperty.floatValue;
            }
        }
    }
}