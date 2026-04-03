// <copyright file="IntrinsicValidationSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence
{
    using BovineLabs.Essence.Data;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Entities;

    /// <summary>
    /// Validates and restricts intrinsic values based on stat limits to ensure intrinsics
    /// remain within valid ranges defined by current stat values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs last in the StatChangedSystemGroup to ensure all stat calculations
    /// are complete before validating intrinsic ranges. It processes entities with changed
    /// stats and updates any intrinsics that are limited by those stat values.
    /// </para>
    /// <para>
    /// The validation process:
    /// 1. Iterates through all stats on entities with StatChanged
    /// 2. Finds intrinsics that are limited by each stat (min/max constraints)
    /// 3. Restricts intrinsic values to stay within the stat-defined limits
    /// 4. Updates intrinsic ranges to reflect new constraints
    /// </para>
    /// </remarks>
    [UpdateBefore(typeof(StatChangedResetSystem))]
    [UpdateInGroup(typeof(StatChangedSystemGroup), OrderLast = true)]
    public partial struct IntrinsicValidationSystem : ISystem
    {
        private IntrinsicWriter.TypeHandle intrinsicWriterTypeHandle;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.intrinsicWriterTypeHandle.Create(ref state);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.intrinsicWriterTypeHandle.Update(ref state, SystemAPI.GetSingleton<EssenceConfig>());

            var query = SystemAPI.QueryBuilder().WithAllRW<Intrinsic>().WithAll<Stat, StatChanged>().Build();

            state.Dependency = new IntrinsicValidationJob
                {
                    IntrinsicWriterHandle = this.intrinsicWriterTypeHandle,
                }
                .ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private struct IntrinsicValidationJob : IJobChunk
        {
            public IntrinsicWriter.TypeHandle IntrinsicWriterHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var intrinsicWriters = this.IntrinsicWriterHandle.Resolve(chunk);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var entityInChunkIndex))
                {
                    // When we update stats, our intrinsic ranges might need updating
                    IntrinsicWriter intrinsicWriter = intrinsicWriters[entityInChunkIndex];

                    ref var si = ref this.IntrinsicWriterHandle.EssenceConfig.Value.Value.StatsLimitIntrinsics;
                    foreach (var stat in intrinsicWriter.Stats)
                    {
                        if (!si.TryGetFirstValue(stat.Key, out var intrinsic, out var it))
                        {
                            continue;
                        }

                        do
                        {
                            if (intrinsic.Ref.IsMin)
                            {
                                intrinsicWriter.RestrictMin(intrinsic.Ref.Intrinsic, stat.Value.Value);
                            }
                            else
                            {
                                intrinsicWriter.RestrictMax(intrinsic.Ref.Intrinsic, stat.Value.Value);
                            }
                        }
                        while (si.TryGetNextValue(out intrinsic, ref it));
                    }
                }
            }
        }
    }
}
