// <copyright file="InputBoundsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Authoring.Input
{
    using BovineLabs.Nerve.Data.Input;
    using Unity.Entities;
    using UnityEngine;

    public class InputBoundsAuthoring : MonoBehaviour
    {
        private class InputCameraFrustumBaker : Baker<InputBoundsAuthoring>
        {
            public override void Bake(InputBoundsAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddComponent<InputBounds>(entity);
            }
        }
    }
}
