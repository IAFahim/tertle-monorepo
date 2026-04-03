// <copyright file="IActionWithTarget.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Actions
{
    using BovineLabs.Reaction.Data.Core;

    /// <summary>
    /// Interface for action components that specify a target entity for their operations.
    /// </summary>
    public interface IActionWithTarget
    {
        Target Target { get; }
    }
}
