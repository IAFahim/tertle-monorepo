// <copyright file="ClipWeight.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    /// <summary> The current assigned weight of a clip. </summary>
    public struct ClipWeight : IComponentData
    {
        /// <summary> The weight value, typically between 0 and 1. </summary>
        public float Value;
    }

    /// <summary> Animation curve for the assigned weight of a clip. </summary>
    public struct AnimatedClipWeight : IComponentData
    {
        /// <summary> The blob curve sampler used to evaluate weight over time. </summary>
        public BlobCurveSampler Value;
    }
}
