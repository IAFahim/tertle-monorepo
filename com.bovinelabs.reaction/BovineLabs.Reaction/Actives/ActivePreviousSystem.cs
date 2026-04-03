// <copyright file="ActivePreviousSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actives
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Tracks the previous frame's active state to enable change detection for reaction activation and deactivation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs first in the <see cref="ActiveSystemGroup"/> (OrderFirst = true) to ensure that
    /// the previous state is captured before any other systems modify the current <see cref="Active"/> state.
    /// </para>
    /// <para>
    /// The system maintains the following component relationship:
    /// - <see cref="ActivePrevious"/> - Stores the enabled state of <see cref="Active"/> from the previous frame
    /// - <see cref="Active"/> - The current active state being tracked
    /// </para>
    /// <para>
    /// This previous state tracking is essential for action systems to determine when reactions are
    /// activated (Active=true, ActivePrevious=false) or deactivated (Active=false, ActivePrevious=true),
    /// allowing them to apply or reverse their effects accordingly. Without this system, action systems
    /// would not be able to differentiate between newly activated reactions and those that were already active.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveSystemGroup), OrderFirst = true)]
    public partial struct ActivePreviousSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithPresentRW<ActivePrevious>().WithPresent<Active>().Build();
            state.Dependency = new SetPreviousJob
            {
                ActivePreviousHandle = SystemAPI.GetComponentTypeHandle<ActivePrevious>(),
                ActiveHandle = SystemAPI.GetComponentTypeHandle<Active>(true),
            }.ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private struct SetPreviousJob : IJobChunk
        {
            public ComponentTypeHandle<ActivePrevious> ActivePreviousHandle;

            [ReadOnly]
            public ComponentTypeHandle<Active> ActiveHandle;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                chunk.CopyEnableMaskFrom(ref this.ActivePreviousHandle, ref this.ActiveHandle);
            }
        }
    }
}
