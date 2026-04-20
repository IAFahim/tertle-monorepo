// <copyright file="VolumeVignette.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Vignette overrides for a <see cref="UnityEngine.Rendering.VolumeProfile"/>.
    /// </summary>
    public struct VolumeVignette : IComponentData
    {
        public Color Color;
        public Vector2 Center;
        public float Intensity;
        public float Smoothness;

        public bool Rounded;
        public bool Active;
        public bool ColorOverride;
        public bool CenterOverride;
        public bool IntensityOverride;
        public bool SmoothnessOverride;
        public bool RoundedOverride;
    }
}
