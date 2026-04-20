// <copyright file="VolumeFilmGrain.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    public struct VolumeFilmGrain : IComponentData
    {
        public FilmGrainLookup Type;
        public float Intensity;
        public float Response;
        public UnityObjectRef<Texture> Texture;

        public bool Active;
        public bool TypeOverride;
        public bool IntensityOverride;
        public bool ResponseOverride;
        public bool TextureOverride;
    }
}
#endif
