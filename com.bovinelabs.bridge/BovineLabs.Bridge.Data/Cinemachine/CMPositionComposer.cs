// <copyright file="CMPositionComposer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct CMPositionComposer : IComponentData
    {
        public float CameraDistance;
        public float DeadZoneDepth;
        public ScreenComposerSettings Composition;
        public float3 TargetOffset;
        public float3 Damping;
        public LookaheadSettings Lookahead;
        public bool CenterOnActivate;
    }
}
#endif