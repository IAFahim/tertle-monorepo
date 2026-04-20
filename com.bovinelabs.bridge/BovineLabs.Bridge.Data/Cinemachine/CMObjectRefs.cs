// <copyright file="CMObjectRefs.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using System;

    [Flags]
    public enum CMCameraRuntimeType
    {
        None = 0,
        Camera = 1 << 0,
        Follow = 1 << 1,
        PositionComposer = 1 << 2,
        RotationComposer = 1 << 3,
        ThirdPersonFollow = 1 << 4,
        OrbitFollow = 1 << 5,
        FreeLookModifier = 1 << 6,
        RotateWithFollowTarget = 1 << 7,
        HardLockToTarget = 1 << 8,
        HardLookAt = 1 << 9,
        PanTilt = 1 << 10,
        BasicMultiChannelPerlin = 1 << 11,
        GroupFraming = 1 << 12,
        FollowZoom = 1 << 13,
        CameraOffset = 1 << 14,
        Recomposer = 1 << 15,
        VolumeSettings = 1 << 16,
#if UNITY_SPLINES
        SplineDolly = 1 << 17,
        SplineDollyLookAtTargets = 1 << 18,
#endif
    }
}
#endif
