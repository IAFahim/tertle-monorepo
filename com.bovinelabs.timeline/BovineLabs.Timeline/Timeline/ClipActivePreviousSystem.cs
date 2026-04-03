// <copyright file="ClipActivePreviousSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Timeline.Data;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// System that copies the current ClipActive state to ClipActivePrevious. This allows detecting clip activation and deactivation events.
    /// </summary>
    [UpdateInGroup(typeof(TimelineSystemGroup), OrderLast = true)]
    public partial struct ClipActivePreviousSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithPresentRW<ClipActivePrevious>().WithPresent<ClipActive>().Build();
            state.Dependency = new SetPreviousJob
            {
                ClipActivePreviousHandle = SystemAPI.GetComponentTypeHandle<ClipActivePrevious>(),
                ClipActiveHandle = SystemAPI.GetComponentTypeHandle<ClipActive>(true),
            }.ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private struct SetPreviousJob : IJobChunk
        {
            public ComponentTypeHandle<ClipActivePrevious> ClipActivePreviousHandle;

            [ReadOnly]
            public ComponentTypeHandle<ClipActive> ClipActiveHandle;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                chunk.CopyEnableMaskFrom(ref this.ClipActivePreviousHandle, ref this.ClipActiveHandle);
            }
        }
    }
}
