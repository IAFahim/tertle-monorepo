// <copyright file="StatInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Essence.Data;
    using JetBrains.Annotations;
    using Unity.Entities;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    [UsedImplicitly]
    internal class StatInspector : PropertyInspector<DynamicBuffer<Stat>>
    {
        /// <inheritdoc/>
        public override VisualElement Build()
        {
            return new DynamicHashMapListElement<Stat, Stat, StatKey, StatValue>(this, 0);
        }
    }
}
