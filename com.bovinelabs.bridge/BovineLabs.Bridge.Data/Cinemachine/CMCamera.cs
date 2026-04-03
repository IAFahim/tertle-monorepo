// <copyright file="CMCamera.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    public struct CMCamera : IComponentData
    {
        public static readonly CMCamera Default = new()
        {
            Priority = default,
            FieldOfView = 40,
            OrthographicSize = 10,
            NearClipPlane = 0.1f,
            FarClipPlane = 5000f,
            Dutch = 0,
            OutputChannel = OutputChannels.Default,
            BlendHint = default,
        };

        public bool Enabled;

        public Entity TrackingTarget;
        public Entity LookAtTarget;
        public bool CustomLookAtTarget;

        public PrioritySettings Priority;
        public OutputChannels OutputChannel;
        public CinemachineCore.BlendHints BlendHint;

        public CinemachineVirtualCameraBase.StandbyUpdateMode StandbyUpdate;

        // Lens
        public float FieldOfView; // 1 to 179
        public float NearClipPlane;
        public float FarClipPlane;
        public float Dutch;
        public LensSettings.OverrideModes ModeOverride;
        public float OrthographicSize;
    }
}
#endif