// <copyright file="StatCalculationSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence
{
    using BovineLabs.Essence.Data;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Calculates final stat values by applying modifiers to base stat values.
    /// This system processes entities with changed stats and recalculates their final values
    /// by combining base stat defaults with accumulated stat modifiers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs first in the StatChangedSystemGroup to ensure stat calculations
    /// are completed before other systems depend on the updated values.
    /// </para>
    /// <para>
    /// The calculation process:
    /// 1. Collects all stat modifiers from the StatModifiers buffer
    /// 2. Adds base default values from StatDefaults
    /// 3. Applies the combined modifiers to produce final stat values
    /// 4. Marks entities as dirty for condition system updates if applicable
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(StatChangedSystemGroup), OrderFirst = true)]
    public partial struct StatCalculationSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAllRW<Stat, StatModifiers>().WithAll<StatDefaults, StatChanged>().Build();

            state.Dependency = new CalculateStatsJob
                {
                    StatHandle = SystemAPI.GetBufferTypeHandle<Stat>(),
                    StatModifiersHandle = SystemAPI.GetBufferTypeHandle<StatModifiers>(),
                    StatDefaultsHandle = SystemAPI.GetComponentTypeHandle<StatDefaults>(true),
                    StatsConditionDirtyHandle = SystemAPI.GetComponentTypeHandle<StatConditionDirty>(),
                }
                .ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private unsafe struct CalculateStatsJob : IJobChunk
        {
            public BufferTypeHandle<Stat> StatHandle;

            [ReadOnly]
            public BufferTypeHandle<StatModifiers> StatModifiersHandle;

            [ReadOnly]
            public ComponentTypeHandle<StatDefaults> StatDefaultsHandle;
            public ComponentTypeHandle<StatConditionDirty> StatsConditionDirtyHandle; // optional

            private StatModifierCalculator statModifierCalculator;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Per thread memory allocation
                if (!this.statModifierCalculator.IsCreated)
                {
                    this.statModifierCalculator = new StatModifierCalculator(Allocator.Temp);
                }

                var statAccessor = chunk.GetBufferAccessor(ref this.StatHandle);
                var statModifiersAccessor = chunk.GetBufferAccessor(ref this.StatModifiersHandle);
                var statDefaultss = (StatDefaults*)chunk.GetRequiredComponentDataPtrRO(ref this.StatDefaultsHandle);

                var hasStatsDirty = chunk.Has(ref this.StatsConditionDirtyHandle);
                var statsDirty = chunk.GetEnabledMask(ref this.StatsConditionDirtyHandle);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var entityInChunkIndex))
                {
                    var statMap = statAccessor[entityInChunkIndex].AsMap();
                    var statModifierBuffer = statModifiersAccessor[entityInChunkIndex];
                    var statDefaults = statDefaultss[entityInChunkIndex];

                    this.statModifierCalculator.Reset();

                    // Add our modifiers
                    var modifiers = statModifierBuffer.AsNativeArray();
                    for (var index = 0; index < modifiers.Length; index++)
                    {
                        this.statModifierCalculator.Add(modifiers[index].Value);
                    }

                    // Add any base
                    ref var baseValue = ref statDefaults.Value.Value.Default;
                    for (var index = 0; index < baseValue.Length; index++)
                    {
                        this.statModifierCalculator.Add(baseValue[index]);
                    }

                    // Apply to our map
                    this.statModifierCalculator.ApplyTo(ref statMap);

                    if (hasStatsDirty)
                    {
                        statsDirty[entityInChunkIndex] = true;
                    }
                }
            }
        }
    }
}
