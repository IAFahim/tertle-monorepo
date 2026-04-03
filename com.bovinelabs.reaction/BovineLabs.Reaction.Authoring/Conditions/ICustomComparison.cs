// <copyright file="ICustomComparison.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Conditions
{
    using System;
    using System.Collections.Generic;
    using Unity.Entities;

    /// <summary>
    /// Enables authoring-side custom comparisons to contribute their runtime data during baking.
    /// </summary>
    public interface ICustomComparison
    {
        /// <summary>
        /// Writes any supporting data required by the comparison into the baker.
        /// Use <paramref name="bakedData"/> to cache objects keyed by type so repeated comparisons avoid duplicating work.
        /// </summary>
        /// <param name="baker">The baker executing conversion.</param>
        /// <param name="bakedData">A cache of baked objects that implementations can reuse instead of adding them multiple times.</param>
        /// <param name="index">The event value index reserved for this comparison.</param>
        void Bake(IBaker baker, Dictionary<Type, object> bakedData, byte index);
    }
}
