// <copyright file="VolumeColorLookup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;

    public struct VolumeColorLookup : IComponentData
    {
        public UnityObjectRef<Texture> Texture;
        public float Contribution;

        public bool Active;
        public bool TextureOverride;
        public bool ContributionOverride;
    }
}
#endif
