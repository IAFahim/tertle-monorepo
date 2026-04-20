// <copyright file="VolumeSplitToning.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;

    public struct VolumeSplitToning : IComponentData
    {
        public Color Shadows;
        public Color Highlights;
        public float Balance;

        public bool Active;
        public bool ShadowsOverride;
        public bool HighlightsOverride;
        public bool BalanceOverride;
    }
}
#endif
