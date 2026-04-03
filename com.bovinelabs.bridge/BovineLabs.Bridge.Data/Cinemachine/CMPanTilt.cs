// <copyright file="CMPanTilt.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    public struct CMPanTilt : IComponentData
    {
        public CinemachinePanTilt.ReferenceFrames ReferenceFrame;
        public CinemachinePanTilt.RecenterTargetModes RecenterTarget;
        public InputAxis PanAxis;
        public InputAxis TiltAxis;
    }
}
#endif