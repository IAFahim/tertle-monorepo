// <copyright file="RelevanceConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.App
{
    using Unity.Entities;

    public struct RelevanceConfig : IComponentData
    {
        public float ClampExtents; // Half size
        public float ExpandExtents;
    }
}
