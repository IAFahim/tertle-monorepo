// <copyright file="DirectorBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;

    /// <summary> Buffer added to the root director which has a collection of all bindings applied to all tracks. </summary>
    [InternalBufferCapacity(0)]
    public struct DirectorBinding : IBufferElementData
    {
        /// <summary> The unique identifier for the track. </summary>
        public TrackId TrackIdentifier;

        /// <summary> The track entity associated with this binding. </summary>
        public Entity TrackEntity;
    }
}
