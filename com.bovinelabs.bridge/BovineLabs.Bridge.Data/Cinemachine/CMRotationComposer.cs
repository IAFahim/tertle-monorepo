// <copyright file="CMRotationComposer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    public struct CMRotationComposer : IComponentData
    {
        /// <summary>
        /// Target offset from the object's center in LOCAL space which
        /// the Composer tracks. Use this to fine-tune the tracking target position
        /// when the desired area is not in the tracked object's center
        /// </summary>
        public float3 TargetOffset;

        /// <summary>
        /// This setting will instruct the composer to adjust its target offset based
        /// on the motion of the target.  The composer will look at a point where it estimates
        /// the target will be a little into the future.
        /// </summary>
        public LookaheadSettings Lookahead;

        /// <summary>
        /// How aggressively the camera tries to follow the target in screen space.
        /// Small numbers are more responsive, rapidly orienting the camera to keep the target in
        /// the dead zone. Larger numbers give a more heavy slowly responding camera.
        /// Using different vertical and horizontal settings can yield a wide range of camera behaviors.
        /// </summary>
        public Vector2 Damping;

        /// <summary> Settings for screen-space composition </summary>
        public ScreenComposerSettings Composition;

        /// <summary> Settings for screen-space composition </summary>
        public bool CenterOnActivate;
    }
}
#endif