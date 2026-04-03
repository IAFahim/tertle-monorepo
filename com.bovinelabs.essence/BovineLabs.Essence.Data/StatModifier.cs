// <copyright file="StatModifier.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Properties;

    /// <summary>
    /// A stat modifier that can be applied to modify stat values with different calculation types.
    /// </summary>
    public struct StatModifier
    {
        /// <summary> Gets the stat type this affects. It matches the index in the stat schemas. </summary>
        public StatKey Type;
        public StatModifyType ModifyType;

        internal uint ValueRaw;

        [CreateProperty(ReadOnly = true)]
        public int Value
        {
            get => UnsafeUtility.As<uint, int>(ref this.ValueRaw);
            set => UnsafeUtility.As<uint, int>(ref this.ValueRaw) = value;
        }

        [CreateProperty(ReadOnly = true)]
        public float ValueFloat
        {
            get => UnsafeUtility.As<uint, float>(ref this.ValueRaw);
            set => UnsafeUtility.As<uint, float>(ref this.ValueRaw) = value;
        }
    }
}
