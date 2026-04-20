// <copyright file="InputDebug.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Debug
{
    using Unity.Entities;

    public partial struct InputDebug : IComponentData
    {
        [InputActionDown]
        public bool TimeScaleDouble;

        [InputActionDown]
        public bool TimeScaleHalve;
    }
}
