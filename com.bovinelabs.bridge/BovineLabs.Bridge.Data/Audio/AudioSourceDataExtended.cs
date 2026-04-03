// <copyright file="AudioSourceDataExtended.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Extended data for <see cref="UnityEngine.AudioSource"/> properties that rarely change at runtime.
    /// </summary>
    public struct AudioSourceDataExtended : IComponentData
    {
        public UnityObjectRef<AudioClip> Clip;
        public float PanStereo;

        public float SpatialBlend;
        public float MinDistance;
        public float MaxDistance;
        public float DopplerLevel;
        public float Spread;
        public AudioRolloffMode RolloffMode;
        public int Priority;
        public float ReverbZoneMix;
    }
}
