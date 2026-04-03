// <copyright file="IntrinsicComparisonMode.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Authoring.Conditions;
    using Unity.Entities;
    using UnityEngine;

    [Serializable]
    public class IntrinsicComparisonMode : ICustomComparison
    {
        [SerializeField]
        private IntrinsicSchemaObject? intrinsic;

        public void Bake(IBaker baker, Dictionary<Type, object> bakedData, byte index)
        {
            DynamicBuffer<EssenceComparisonMode> comparisonModes;

            if (!bakedData.TryGetValue(typeof(EssenceComparisonMode), out var bufferObject))
            {
                comparisonModes = baker.AddBuffer<EssenceComparisonMode>(baker.GetEntity(TransformUsageFlags.None));
                bakedData.Add(typeof(EssenceComparisonMode), comparisonModes);
            }
            else
            {
                comparisonModes = (DynamicBuffer<EssenceComparisonMode>)bufferObject;
            }

            comparisonModes.Add(new EssenceComparisonMode
            {
                Index = index,
                IsStat = false,
                Intrinsic = this.intrinsic,
            });
        }
    }
}
