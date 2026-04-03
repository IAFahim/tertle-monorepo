// <copyright file="StatKeyInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using System.Linq;
    using BovineLabs.Core.Editor.Internal;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Essence.Authoring;
    using BovineLabs.Essence.Data;
    using JetBrains.Annotations;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    [UsedImplicitly]
    internal class StatKeyInspector : PropertyInspector<StatKey>
    {
        private static EssenceSettings essenceSettings;

        /// <inheritdoc/>
        public override VisualElement Build()
        {
            if (essenceSettings == null)
            {
                essenceSettings = EditorSettingsUtility.GetSettings<EssenceSettings>();
            }

            var stat = essenceSettings.StatSchemas.FirstOrDefault(f => f.Key == this.Target);
            var id = new TextField(this.Name)
            {
                value = stat?.name,
                isReadOnly = true,
            };
            InspectorUtility.AddRuntimeBar(id);
            return id;
        }
    }
}
