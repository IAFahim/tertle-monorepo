// <copyright file="BridgeObject.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data
{
    using Unity.Entities;
    using UnityEngine;

    public struct BridgeObject : ICleanupComponentData
    {
        public UnityObjectRef<GameObject> Value;
        public TransformHandle Transform;
        public BridgeType Type;

        public readonly T Q<T>()
            where T : Component
        {
            return this.Value.Value.GetComponent<T>();
        }
    }
}
