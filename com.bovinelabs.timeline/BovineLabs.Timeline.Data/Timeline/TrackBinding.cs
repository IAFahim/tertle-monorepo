// <copyright file="TrackBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;

    /// <summary> Component assigned to a Track entity indicating which entity it targets. </summary>
    public struct TrackBinding : IComponentData
    {
        /// <summary> The entity this track is bound to. </summary>
        public Entity Value;
    }
}
