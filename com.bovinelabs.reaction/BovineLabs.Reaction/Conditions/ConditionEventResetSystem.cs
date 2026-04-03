// <copyright file="ConditionEventResetSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Conditions
{
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Entities;

    /// <summary>
    /// Resets condition states at the end of each frame to manage event-based and temporary conditions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ConditionsSystemGroup"/> after <see cref="ConditionAllActiveSystem"/>
    /// to ensure condition evaluation completes before any reset operations. It processes entities with
    /// both <see cref="ConditionActive"/> and <see cref="ConditionReset"/> components.
    /// </para>
    /// <para>
    /// The system performs selective condition resetting by applying a bitwise AND operation between
    /// the current <see cref="ConditionActive"/> state and the <see cref="ConditionReset"/> mask.
    /// This allows fine-grained control over which conditions persist across frames and which are reset.
    /// </para>
    /// <para>
    /// This reset mechanism is essential for:
    /// - Event-based conditions that should only be active for a single frame
    /// - Temporary conditions that need to be cleared after evaluation
    /// - Preventing condition states from accumulating inappropriately over time
    /// </para>
    /// <para>
    /// The system uses change filters to optimize performance, only processing entities where
    /// <see cref="ConditionActive"/> has been modified during the current frame.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ConditionsSystemGroup))]
    [UpdateAfter(typeof(ConditionAllActiveSystem))]
    public partial struct ConditionEventResetSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ResetJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithChangeFilter(typeof(ConditionActive))]
        private partial struct ResetJob : IJobEntity
        {
            private static void Execute(ref ConditionActive active, in ConditionReset reset)
            {
                active.Value = active.Value.BitAnd(reset.Value);
            }
        }
    }
}
