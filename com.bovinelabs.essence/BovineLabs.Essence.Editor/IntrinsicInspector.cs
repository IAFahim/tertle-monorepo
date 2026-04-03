// <copyright file="IntrinsicInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using System.Linq;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Essence.Authoring;
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Data.Conditions;
    using JetBrains.Annotations;
    using Unity.Entities;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    [UsedImplicitly]
    internal class IntrinsicInspector : PropertyInspector<DynamicBuffer<Intrinsic>>
    {
        /// <inheritdoc/>
        public override VisualElement Build()
        {
            var settings = EditorSettingsUtility.GetSettings<EssenceSettings>();
            var intrinsics = settings.IntrinsicSchemas.Where(s => s != null).Select(s => new SearchView.Item { Path = $"{s.name} ({s.Key})", Data = (IntrinsicKey)s.Key }).ToList();
            var element = new DynamicHashMapElement<Intrinsic, IntrinsicKey, int>(this, intrinsics);

            var fallback = element.SearchSetValue;

            element.SearchSetValue = (context, key, val) =>
            {
                var lookup = default(IntrinsicWriter.Lookup);
                lookup.EssenceConfig = context.EntityManager.GetSingleton<EssenceConfig>();
                lookup.Intrinsics = context.EntityManager.GetBufferLookup<Intrinsic>();
                lookup.Stats = context.EntityManager.GetBufferLookup<Stat>(true);
                lookup.IntrinsicConditionDirtys = context.EntityManager.GetComponentLookup<IntrinsicConditionDirty>();
                lookup.EventWriter.ConditionEvents = context.EntityManager.GetBufferLookup<ConditionEvent>();
                lookup.EventWriter.EventsDirtys = context.EntityManager.GetComponentLookup<EventsDirty>();

                if (!lookup.TryGet(context.Entity, out var writer))
                {
                    fallback(context, key, val);
                    return;
                }

                writer.Set(key, val);
            };

            return element;
        }
    }
}
