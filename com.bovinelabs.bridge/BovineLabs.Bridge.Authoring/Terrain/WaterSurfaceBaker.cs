// <copyright file="WaterSurfaceBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
namespace BovineLabs.Bridge.Authoring.Terrain
{
    using Unity.Entities;
    using UnityEngine.Rendering.HighDefinition;

    public class WaterSurfaceBaker : Baker<WaterSurface>
    {
        public override void Bake(WaterSurface authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Renderable);
            this.AddComponentObject(entity, authoring);
        }
    }
}
#endif