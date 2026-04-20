// <copyright file="VolumePaniniProjection.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using Unity.Entities;

    public struct VolumePaniniProjection : IComponentData
    {
        public float Distance;
        public float CropToFit;

        public bool Active;
        public bool DistanceOverride;
        public bool CropToFitOverride;
    }
}
#endif
