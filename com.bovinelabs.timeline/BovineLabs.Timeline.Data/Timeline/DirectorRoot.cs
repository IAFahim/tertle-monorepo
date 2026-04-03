// <copyright file="DirectorRoot.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;

    /// <summary> Component assigned to a Clip entity to reference the root director that owns it. </summary>
    public struct DirectorRoot : IComponentData
    {
        /// <summary> The director entity that owns this clip. </summary>
        public Entity Director;
    }
}
