// <copyright file="IntrinsicGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using System;
    using UnityEngine;

    /// <summary>
    /// ScriptableObject for grouping multiple intrinsic default values together.
    /// This allows for reusable collections of intrinsic configurations that can be applied to multiple entities.
    /// </summary>
    [CreateAssetMenu(menuName = "BovineLabs/Stats/Intrinsic Group", fileName = "IntrinsicGroup")]
    public class IntrinsicGroup : ScriptableObject
    {
        public IntrinsicDefault[] Values = Array.Empty<IntrinsicDefault>();
    }
}
