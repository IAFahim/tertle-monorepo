// <copyright file="CMGroupFraming.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CMGroupFraming : IComponentData
    {
        public CinemachineGroupFraming.FramingModes FramingMode;
        public float FramingSize;
        public float2 CenterOffset;
        public float Damping;
        public CinemachineGroupFraming.SizeAdjustmentModes SizeAdjustment;
        public CinemachineGroupFraming.LateralAdjustmentModes LateralAdjustment;
        public float2 FovRange;
        public float2 DollyRange;
        public float2 OrthoSizeRange;
    }
}
#endif