// <copyright file="AudioDistortionFilterData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    public struct AudioDistortionFilterData : IComponentData
    {
        public float DistortionLevel;
    }
}
