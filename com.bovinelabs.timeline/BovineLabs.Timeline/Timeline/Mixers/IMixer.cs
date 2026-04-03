// <copyright file="IMixer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using Unity.Entities;

    /// <summary>
    /// Defines interpolation and additive blending operations for a value type.
    /// </summary>
    /// <typeparam name="T">The value type to mix.</typeparam>
    public interface IMixer<T> : IComponentData
        where T : unmanaged
    {
        /// <summary>Interpolates between two values.</summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <param name="s">The interpolation factor.</param>
        /// <returns>The interpolated value.</returns>
        T Lerp(in T a, in T b, in float s);

        /// <summary>Adds two values for additive blending.</summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>The additive blend result.</returns>
        T Add(in T a, in T b);
    }
}
