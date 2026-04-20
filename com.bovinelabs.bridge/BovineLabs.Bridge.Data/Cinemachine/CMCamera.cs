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
        public BlobAssetReference<BlobString> Name;
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

    public struct CMCameraTargetBridgeObjects : IComponentData
    {
        public Entity TrackingTargetBridge;
        public Entity LookAtTargetBridge;
    }

    public struct CMCameraTargetBridgeObject : IComponentData
    {
    }
}
#endif
