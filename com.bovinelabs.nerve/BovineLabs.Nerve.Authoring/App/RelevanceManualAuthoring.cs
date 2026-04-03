// <copyright file="RelevanceManualAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Authoring.App
{
    using BovineLabs.Nerve.Data.App;
    using Unity.Entities;
    using UnityEngine;

    public class RelevanceManualAuthoring : MonoBehaviour
    {
        private class RelevanceManualBaker : Baker<RelevanceManualAuthoring>
        {
            public override void Bake(RelevanceManualAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddComponent<RelevanceManual>(entity);
            }
        }
    }
}
