// <copyright file="ConditionEventInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Reaction.Data.Conditions;
    using JetBrains.Annotations;
    using Unity.Entities;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    [UsedImplicitly]
    internal class ConditionEventInspector : PropertyInspector<DynamicBuffer<ConditionEvent>>
    {
        public override VisualElement Build()
        {
            return new DynamicHashMapElement<ConditionEvent, ConditionKey, int>(this);
        }
    }
}
