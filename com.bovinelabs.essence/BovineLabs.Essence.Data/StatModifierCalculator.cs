// <copyright file="StatModifierCalculator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    [NoAlias]
    internal struct StatModifierCalculator : IDisposable
    {
        [NativeDisableContainerSafetyRestriction]
        private NativeHashMap<StatKey, Sum> values;

        /// <summary> Initializes a new instance of the <see cref="StatModifierCalculator" /> struct. </summary>
        /// <remarks> By default memory is uninitialized and <see cref="Reset" /> needs to be called before first use. </remarks>
        /// <param name="allocator"> What to allocate the containers with. </param>
        public StatModifierCalculator(Allocator allocator)
        {
            this.values = new NativeHashMap<StatKey, Sum>(0, allocator);
        }

        public bool IsCreated => this.values.IsCreated;

        /// <summary>
        /// Resets the calculator by clearing all accumulated stat modifications.
        /// </summary>
        public void Reset()
        {
            this.values.Clear();
        }

        /// <summary>
        /// Disposes the calculator and releases any allocated resources.
        /// </summary>
        public void Dispose()
        {
            this.values.Dispose();
        }

        /// <summary>
        /// Adds a stat modifier to the calculator for processing.
        /// </summary>
        /// <param name="modifier">The stat modifier to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in StatModifier modifier)
        {
            switch (modifier.ModifyType)
            {
                case StatModifyType.Added:
                    this.AddAdded(modifier.Type, modifier.Value);
                    break;
                case StatModifyType.Additive:
                    this.AddIncreased(modifier.Type, modifier.ValueFloat);
                    break;
                case StatModifyType.Multiplicative:
                    this.AddMultiplicative(modifier.Type, modifier.ValueFloat);
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Applies all accumulated stat modifications to the provided stats hash map.
        /// </summary>
        /// <param name="stats">The stats hash map to apply modifications to.</param>
        public void ApplyTo(ref DynamicHashMap<StatKey, StatValue> stats)
        {
            stats.Clear();

            using var e = this.values.GetEnumerator();
            while (e.MoveNext())
            {
                var statMod = e.Current;

                var combinedAdd = statMod.Value.Added;
                var combinedMulti = (float)(statMod.Value.Increased * statMod.Value.More);

                ref var stat = ref stats.GetOrAddRef(statMod.Key, StatValue.Default);
                stat = new StatValue { Added = combinedAdd, Multi = combinedMulti };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddAdded(StatKey index, int added)
        {
            this.GetSum(index).Added += added;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddIncreased(StatKey index, float increased)
        {
            this.GetSum(index).Increased += increased;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddMultiplicative(StatKey index, float more)
        {
            this.GetSum(index).More *= 1 + more;
        }

        private ref Sum GetSum(StatKey index)
        {
            return ref this.values.GetOrAddRef(index, Sum.Default);
        }

        /// <summary>
        /// Accumulates different types of stat modifiers for final calculation.
        /// </summary>
        public struct Sum
        {
            public static readonly Sum Default = new() { Increased = 1f, More = 1f };

            public int Added;

            // Temp double for precision
            public double Increased;
            public double More;
        }
    }
}
