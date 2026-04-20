// <copyright file="LightData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Lighting
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Data that mirrors the core properties of a managed <see cref="UnityEngine.Light"/>.
    /// </summary>
    public struct LightData : IComponentData
    {
        public Color Color;
        public float Intensity;
        public float ColorTemperature;
    }
}
