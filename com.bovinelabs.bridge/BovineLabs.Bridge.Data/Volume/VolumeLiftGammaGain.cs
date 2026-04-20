// <copyright file="VolumeLiftGammaGain.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;
    using UnityEngine;

    public struct VolumeLiftGammaGain : IComponentData
    {
        public Vector4 Lift;
        public Vector4 Gamma;
        public Vector4 Gain;

        public bool Active;
        public bool LiftOverride;
        public bool GammaOverride;
        public bool GainOverride;
    }
}
#endif
