// <copyright file="StatChangedResetSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence
{
    using BovineLabs.Essence.Data;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Entities;

    /// <summary>
    /// Resets StatChanged flags at the end of each frame to prepare for the next frame's processing.
    /// This system ensures StatChanged components are disabled after all stat-related systems have processed them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs last in the StatChangedSystemGroup to ensure all other systems
    /// that depend on StatChanged flags have completed their work before the flags are reset.
    /// The StatChanged component uses enableable components for efficient processing.
    /// </para>
    /// <para>
    /// The reset process:
    /// 1. Finds all entities with enabled StatChanged components
    /// 2. Disables the StatChanged component on all found entities
    /// 3. Prepares entities for potential stat changes in subsequent frames
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(StatChangedSystemGroup), OrderLast = true)]
    public partial struct StatChangedResetSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<StatChanged>().Build();

            state.Dependency = new StatChangedResetJob
            {
                StatChangedHandle = SystemAPI.GetComponentTypeHandle<StatChanged>(),
            }.ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private struct StatChangedResetJob : IJobChunk
        {
            public ComponentTypeHandle<StatChanged> StatChangedHandle;

            /// <inheritdoc/>
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                chunk.SetComponentEnabledForAll(ref this.StatChangedHandle, false);
            }
        }
    }
}
