// <copyright file="IntrinsicSchemaObjectEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Essence.Authoring;
    using BovineLabs.Reaction.Authoring.Conditions;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(IntrinsicSchemaObject))]
    public class IntrinsicSchemaObjectEditor : ElementEditor
    {
        private Button eventButton;

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement parentElement, bool createdElements)
        {
            this.eventButton = new Button(this.ToggleCondition);

            this.UpdateButtons();

            parentElement.Add(this.eventButton);
        }

        private void UpdateButtons()
        {
            var hasEvent = this.TryGetCondition(out _);
            this.eventButton.text = hasEvent ? "Remove Event Condition" : "Add Event Condition";
        }

        private void ToggleCondition()
        {
            if (this.TryGetCondition(out var condition))
            {
                AssetDatabase.RemoveObjectFromAsset(condition);
            }
            else
            {
                condition = CreateInstance<ConditionEventObject>();
                condition.name = $"{this.serializedObject.targetObject.name}ConditionEvent";

                AssetDatabase.AddObjectToAsset(condition, this.serializedObject.targetObject);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            this.UpdateButtons();
        }

        private bool TryGetCondition(out ConditionEventObject condition)
        {
            var path = AssetDatabase.GetAssetPath(this.serializedObject.targetObject);

            foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (asset is not ConditionEventObject conditionSchemaObject)
                {
                    continue;
                }

                condition = conditionSchemaObject;
                return true;
            }

            condition = null;
            return false;
        }
    }
}
