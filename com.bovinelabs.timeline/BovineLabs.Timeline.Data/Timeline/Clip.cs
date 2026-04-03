// <copyright file="Clip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;

    /// <summary> Component assigned to a Clip entity to reference its parent Track. </summary>
    public struct Clip : IComponentData
    {
        /// <summary> The track entity the clip belongs to. </summary>
        public Entity Track;
    }
}
