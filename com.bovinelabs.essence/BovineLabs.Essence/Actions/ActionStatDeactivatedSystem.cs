// <copyright file="ActionStatDeactivatedSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Actions
{
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Actions;
    using BovineLabs.Reaction.Core;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Removes stat modifiers from target entities when actions deactivate.
    /// This system processes actions that have just been deactivated and removes their stat modifiers from affected entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the ActiveDisabledSystemGroup and processes actions that have just become
    /// inactive (Active disabled, ActivePrevious enabled). It efficiently removes stat modifiers
    /// by tracking unique targets and their modifier counts to avoid redundant processing.
    /// </para>
    /// <para>
    /// The deactivation process:
    /// 1. Identifies newly deactivated actions with ActionStat components
    /// 2. Groups action stats by unique target to count total modifiers per target
    /// 3. Resolves target entities using the Targets system
    /// 4. Removes matching stat modifiers from target entities' StatModifiers buffers
    /// 5. Enables StatChanged component to trigger stat recalculation
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveDisabledSystemGroup))]
    public partial struct ActionStatDeactivatedSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var removeStats = new NativeQueue<RemoveStatModifier>(state.WorldUpdateAllocator);
            var targetsCustom = SystemAPI.GetComponentLookup<TargetsCustom>(true);

            state.Dependency = new DeactivateJob
                {
                    TargetsCustoms = targetsCustom,
                    RemoveStatModifiers = removeStats.AsParallelWriter(),
                }
                .ScheduleParallel(state.Dependency);

            state.Dependency = new ApplyJob
                {
                    RemoveStatModifiers = removeStats,
                    StatModifiers = SystemAPI.GetBufferLookup<StatModifiers>(),
                    StatChangeds = SystemAPI.GetComponentLookup<StatChanged>(),
                }
                .Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ActivePrevious))]
        [WithDisabled(typeof(Active))]
        private partial struct DeactivateJob : IJobEntity
        {
            public NativeQueue<RemoveStatModifier>.ParallelWriter RemoveStatModifiers;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            private void Execute(Entity entity, in DynamicBuffer<ActionStat> actionStats, in Targets targets)
            {
                var uniqueTargets = ReactionUtil.GetUniqueTargets(actionStats);

                foreach (var uniqueTarget in uniqueTargets)
                {
                    var target = targets.Get(uniqueTarget.Target, entity, this.TargetsCustoms);

                    this.RemoveStatModifiers.Enqueue(new RemoveStatModifier
                    {
                        Source = entity,
                        Target = target,
                        Count = uniqueTarget.Count,
                    });
                }
            }
        }

        [BurstCompile]
        private struct ApplyJob : IJob
        {
            public NativeQueue<RemoveStatModifier> RemoveStatModifiers;

            public ComponentLookup<StatChanged> StatChangeds;

            public BufferLookup<StatModifiers> StatModifiers;

            public void Execute()
            {
                // Remove first so less to iterate
                while (this.RemoveStatModifiers.TryDequeue(out var remove))
                {
                    if (!this.StatModifiers.TryGetBuffer(remove.Target, out var statModifierBuffer))
                    {
                        continue;
                    }

                    this.StatChangeds.SetComponentEnabled(remove.Target, true);

                    var statModifiers = statModifierBuffer.AsNativeArray();
                    var count = remove.Count;

                    for (var i = statModifiers.Length - 1; i >= 0; i--)
                    {
                        if (statModifiers[i].SourceEntity != remove.Source)
                        {
                            continue;
                        }

                        statModifierBuffer.RemoveAtSwapBack(i);
                        if (--count == 0)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private struct RemoveStatModifier
        {
            public Entity Target;
            public Entity Source;
            public int Count;
        }
    }
}
