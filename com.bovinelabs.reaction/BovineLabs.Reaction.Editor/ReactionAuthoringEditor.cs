// <copyright file="ReactionAuthoringEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.Editor.UI;
    using BovineLabs.Core.Utility;
    using BovineLabs.Reaction.Authoring;
    using BovineLabs.Reaction.Authoring.Core;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ReactionAuthoring))]
    public class ReactionAuthoringEditor : ElementEditor
    {
        private readonly List<MonoBehaviour> components = new();
        private readonly HashSet<Type> existingTypes = new();
        private SearchView.Item[]? allItems;

        private GameObject GetGameObject => ((ReactionAuthoring)this.serializedObject.targetObject).gameObject;

        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.components.Clear();
            this.existingTypes.Clear();

            this.GetGameObject.GetComponents(this.components);
            foreach (var c in this.components)
            {
                this.existingTypes.Add(c.GetType());
            }

            this.allItems ??= ReflectionUtility
                .GetAllWithAttribute<ReactionAuthoringAttribute>()
                .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .Select(t => new SearchView.Item
                {
                    Path = t.Name,
                    Data = t,
                })
                .ToArray();

            var items = this.allItems.Where(t => !this.existingTypes.Contains((Type)t.Data)).ToList();

            var se = new SearchElement(items, "Reaction Components", string.Empty);
            se.OnSelection += item =>
            {
                var type = (Type)item.Data;
                var go = ((ReactionAuthoring)this.serializedObject.targetObject).gameObject;
                go.AddComponent(type);
            };

            root.Add(se);
        }
    }
}
