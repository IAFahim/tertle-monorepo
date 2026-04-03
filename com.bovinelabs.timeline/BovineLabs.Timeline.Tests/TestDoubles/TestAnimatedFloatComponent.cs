// <copyright file="TestAnimatedFloatComponent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.TestDoubles
{
    using BovineLabs.Timeline.Data;
    using Unity.Entities;

    public struct TestAnimatedFloatComponent : IComponentData, IAnimatedComponent<float>
    {
        public float CurrentValue;

        public float Value => this.CurrentValue;
    }
}
