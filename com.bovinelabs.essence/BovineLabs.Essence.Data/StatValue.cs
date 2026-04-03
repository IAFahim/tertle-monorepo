// <copyright file="StatValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using Unity.Properties;

    /// <summary>
    /// The components that combine to make the final calculated stat value.
    /// </summary>
    public struct StatValue
    {
        public const float ToInt = 100f;
        public const float ToFloat = 1 / ToInt;

        public static readonly StatValue Default = new() { Multi = 1f };

        public int Added;
        public float Multi;

        /// <summary> Gets the value as if there are were no extra external sources. </summary>
        [CreateProperty]
        public float Value => this.Added * this.Multi;

        [CreateProperty]
        public float ValueFloat => this.Added * this.Multi * ToFloat;
    }
}
