// <copyright file="VolumeShadowsMidtonesHighlights.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;

    public struct VolumeShadowsMidtonesHighlights : IComponentData
    {
        public Vector4 Shadows;
        public Vector4 Midtones;
        public Vector4 Highlights;
        public float ShadowsStart;
        public float ShadowsEnd;
        public float HighlightsStart;
        public float HighlightsEnd;

        public bool Active;
        public bool ShadowsOverride;
        public bool MidtonesOverride;
        public bool HighlightsOverride;
        public bool ShadowsStartOverride;
        public bool ShadowsEndOverride;
        public bool HighlightsStartOverride;
        public bool HighlightsEndOverride;
    }
}
#endif
