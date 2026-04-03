// <copyright file="ActiveCancel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Enableable component that causes active reactions and their duration to be cancelled when conditions no longer pass.
    /// </summary>
    public struct ActiveCancel : IComponentData, IEnableableComponent
    {
    }
}
