// <copyright file="RelevancyProviderAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Authoring.App
{
    using BovineLabs.Nerve.Data.App;
    using Unity.Entities;
    using UnityEngine;

    public class RelevancyProviderAuthoring : MonoBehaviour
    {
        private class RelevancyProviderBaker : Baker<RelevancyProviderAuthoring>
        {
            public override void Bake(RelevancyProviderAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddComponent<RelevanceProvider>(entity);
            }
        }
    }
}
