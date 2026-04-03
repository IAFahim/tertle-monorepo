// <copyright file="ActiveTriggerSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actives
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Entities;

    /// <summary>
    /// Manages trigger reset and duration-only entity reactivation to support proper change filter behavior in <see cref="ActiveSystem"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveSystemGroup"/> after <see cref="ActiveSystem"/> and handles two critical
    /// post-processing tasks that ensure proper active state management across frames:
    /// </para>
    /// <para>
    /// <strong>Trigger Reset:</strong> Automatically resets all <see cref="ActiveTrigger"/> components to prevent
    /// continuous activation. Triggers are intended to be single-frame activation signals, so they must be reset
    /// immediately after processing to avoid retriggering on subsequent frames.
    /// </para>
    /// <para>
    /// <strong>Duration-Only Reactivation:</strong> Handles a specific edge case where entities with only
    /// <see cref="ActiveOnDuration"/> components become inactive when the duration expires. Without other controlling
    /// components, these entities need to be reactivated on the next frame to allow the duration system to potentially
    /// restart them. This system triggers change filters on such entities to ensure <see cref="ActiveSystem"/>
    /// processes them again.
    /// </para>
    /// <para>
    /// <strong>Technical Requirement:</strong> The primary technical reason for this separate system is that ECS change
    /// filters cannot be triggered by the same system that reads them. Since <see cref="ActiveSystem"/> uses change
    /// filters for performance optimization, any modifications needed to trigger those filters must come from a separate
    /// system running after it.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveSystemGroup))]
    [UpdateAfter(typeof(ActiveSystem))]
    public unsafe partial struct ActiveTriggerSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var triggerQuery = SystemAPI.QueryBuilder().WithAll<ActiveTrigger>().Build();

            state.Dependency = new ActiveTriggerResetJob
            {
                ActiveTriggerHandle = SystemAPI.GetComponentTypeHandle<ActiveTrigger>(),
            }.ScheduleParallel(triggerQuery, state.Dependency);
        }

        [BurstCompile]
        private struct ActiveTriggerResetJob : IJobChunk
        {
            public ComponentTypeHandle<ActiveTrigger> ActiveTriggerHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // Reset all triggers in the chunk to disabled state without triggering change filters
                ref var activeTriggersRW = ref chunk.GetRequiredEnabledBitsRW(ref this.ActiveTriggerHandle, out var ptrChunkDisabledCount);
                activeTriggersRW.ULong0 = 0;
                activeTriggersRW.ULong1 = 0;
                chunk.UpdateChunkDisabledCount(ptrChunkDisabledCount, activeTriggersRW);
            }
        }
    }
}
