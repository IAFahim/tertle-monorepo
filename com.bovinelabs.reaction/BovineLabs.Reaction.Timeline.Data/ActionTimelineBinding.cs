// <copyright file="ActionTimelineBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Timeline.Data
{
    using BovineLabs.Reaction.Data.Core;
    using Unity.Collections;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct ActionTimelineBinding : IBufferElementData
    {
        public byte Index;
        public Target Target;
        public FixedString64Bytes TrackIdentifier;
    }
}
