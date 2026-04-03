// <copyright file="AudioChorusFilterData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    public struct AudioChorusFilterData : IComponentData
    {
        public float DryMix;
        public float WetMix1;
        public float WetMix2;
        public float WetMix3;
        public float Delay;
        public float Rate;
        public float Depth;
    }
}
