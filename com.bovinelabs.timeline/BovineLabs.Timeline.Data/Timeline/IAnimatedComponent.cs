// <copyright file="IAnimatedComponent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;

    /// <summary> Interface for components that can be animated by timeline clips. </summary>
    /// <typeparam name="T"> The unmanaged type that represents the animated value. </typeparam>
    public interface IAnimatedComponent<out T> : IComponentData
        where T : unmanaged
    {
        /// <summary> Gets the value to use when the component is not being animated. </summary>
        /// <value>The value used when no clip is driving the component.</value>
        T Value { get; }
    }
}
