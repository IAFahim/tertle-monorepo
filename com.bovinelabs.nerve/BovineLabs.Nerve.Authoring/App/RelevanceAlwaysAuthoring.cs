// <copyright file="RelevanceAlwaysAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Authoring.App
{
    using BovineLabs.Nerve.Data.App;
    using Unity.Entities;
    using UnityEngine;

    public class RelevanceAlwaysAuthoring : MonoBehaviour
    {
        private class RelevanceAlwaysBaker : Baker<RelevanceAlwaysAuthoring>
        {
            public override void Bake(RelevanceAlwaysAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddComponent<RelevanceAlways>(entity);
            }
        }
    }
}
