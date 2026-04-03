// <copyright file="ServiceState.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.States
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.States;
    using Unity.Entities;
    using Unity.Properties;

    public struct ServiceState : IState<BitArray256>
    {
        [CreateProperty]
        public BitArray256 Value { get; set; }
    }

    public struct ServiceStatePrevious : IComponentData
    {
        public BitArray256 Value;
    }
}
