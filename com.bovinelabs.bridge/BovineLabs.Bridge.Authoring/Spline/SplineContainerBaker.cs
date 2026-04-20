// <copyright file="SplineContainerBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_SPLINES
namespace BovineLabs.Bridge.Authoring.Spline
{
    using BovineLabs.Bridge.Data.Spline;
    using BovineLabs.Core.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine.Splines;

    public class SplineContainerBaker : Baker<SplineContainer>
    {
        public override void Bake(SplineContainer authoring)
        {
            var addSplineBridge = authoring.GetComponent<SplineContainerBridgeAuthoring>() != null;

            var entity = this.GetEntity(addSplineBridge ? TransformUsageFlags.Renderable : TransformUsageFlags.None);
            var blob = BlobSpline.Create(authoring.Splines, float4x4.identity);

            this.AddBlobAsset(ref blob, out _);
            this.AddComponent(entity, new Splines { Value = blob });

            if (addSplineBridge)
            {
                this.AddComponent<AddSplineBridge>(entity);
            }
        }
    }
}
#endif