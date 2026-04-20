// <copyright file="VolumeLensDistortion.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;

    public struct VolumeLensDistortion : IComponentData
    {
        public float Intensity;
        public float XMultiplier;
        public float YMultiplier;
        public Vector2 Center;
        public float Scale;

        public bool Active;
        public bool IntensityOverride;
        public bool XMultiplierOverride;
        public bool YMultiplierOverride;
        public bool CenterOverride;
        public bool ScaleOverride;
    }
}
#endif
