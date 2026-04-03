// <copyright file="StatGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using System;
    using UnityEngine;

    /// <summary>
    /// ScriptableObject for grouping multiple stat default values together.
    /// This allows for reusable collections of stat configurations that can be applied to multiple entities.
    /// </summary>
    [CreateAssetMenu(menuName = "BovineLabs/Stats/Stat Group", fileName = "StatGroup")]
    public class StatGroup : ScriptableObject
    {
        public StatModifierAuthoring[] Values = Array.Empty<StatModifierAuthoring>();
    }
}
