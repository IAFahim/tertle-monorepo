// <copyright file="IntrinsicConditionDirty.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using Unity.Entities;

    /// <summary>
    /// A tag component indicating that intrinsic condition values need to be recalculated.
    /// </summary>
    public struct IntrinsicConditionDirty : IComponentData, IEnableableComponent
    {
    }
}
