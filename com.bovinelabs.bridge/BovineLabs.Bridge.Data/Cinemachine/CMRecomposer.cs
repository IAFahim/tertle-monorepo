// <copyright file="CMRecomposer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    public struct CMRecomposer : IComponentData
    {
        public CinemachineCore.Stage ApplyAfter;
        public float Tilt;
        public float Pan;
        public float Dutch;
        public float ZoomScale;
        public float FollowAttachment;
        public float LookAtAttachment;
    }
}
#endif