// <copyright file="CameraBridge.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Camera
{
    using Unity.Entities;
    using UnityEngine;

    public struct CameraBridge : IComponentData
    {
        public UnityObjectRef<Camera> Value;
    }
}