// <copyright file="AudioSourceData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    public struct AudioSourceData : IComponentData
    {
        public float Volume;
        public float Pitch;
    }
}
