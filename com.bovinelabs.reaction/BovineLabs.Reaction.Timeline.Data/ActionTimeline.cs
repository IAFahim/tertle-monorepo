// <copyright file="ActionTimeline.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Timeline.Data
{
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct ActionTimeline : IBufferElementData
    {
        public Entity Director;
        public float InitialTime;

        public bool ResetWhenActive;
        public bool DisableTimelineOnDeactivate;
    }
}
