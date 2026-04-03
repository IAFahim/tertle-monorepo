// <copyright file="ConditionStatWriteSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Core;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Writes current stat values to the condition system for reaction processing.
    /// This system updates condition values with current stat data when entities are marked as dirty.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the ConditionWriteEventsGroup and processes entities with StatConditionDirty
    /// enabled. It writes stat values to condition subscribers that are listening for stat-based conditions,
    /// enabling reactions to trigger based on stat value changes.
    /// </para>
    /// <para>
    /// The write process:
    /// 1. Processes entities with EventSubscriber buffers and dirty stat flags
    /// 2. Iterates through condition subscribers looking for stat-type conditions
    /// 3. Reads current stat values from the entity's Stat buffer
    /// 4. Writes stat values to the condition system using ReactionUtil
    /// 5. Clears the StatConditionDirty flag
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ConditionWriteEventsGroup))]
    public partial struct ConditionStatWriteSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<EventSubscriber>()
                .WithAll<Stat>()
                .WithAllRW<StatConditionDirty>()
                .Build();

            state.Dependency = new WriteStatStateJob
            {
                ConditionActives = SystemAPI.GetComponentLookup<ConditionActive>(),
                ConditionValues = SystemAPI.GetBufferLookup<ConditionValues>(),
                ConditionComparisonValues = SystemAPI.GetBufferLookup<ConditionComparisonValue>(true),
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.StatType),
                EventSubscriberHandle = SystemAPI.GetBufferTypeHandle<EventSubscriber>(true),
                StatHandle = SystemAPI.GetBufferTypeHandle<Stat>(true),
                StatConditionDirtyHandle = SystemAPI.GetComponentTypeHandle<StatConditionDirty>(),
            }.ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private struct WriteStatStateJob : IJobChunk
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<ConditionActive> ConditionActives;

            [NativeDisableParallelForRestriction]
            public BufferLookup<ConditionValues> ConditionValues;

            [ReadOnly]
            public BufferLookup<ConditionComparisonValue> ConditionComparisonValues;

            public byte ConditionType;

            [ReadOnly]
            public BufferTypeHandle<EventSubscriber> EventSubscriberHandle;

            [ReadOnly]
            public BufferTypeHandle<Stat> StatHandle;

            public ComponentTypeHandle<StatConditionDirty> StatConditionDirtyHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var eventSubscribersAccessor = chunk.GetBufferAccessor(ref this.EventSubscriberHandle);
                var statsAccessor = chunk.GetBufferAccessor(ref this.StatHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    chunk.SetComponentEnabled(ref this.StatConditionDirtyHandle, entityIndexInChunk, false);

                    var eventSubscribers = eventSubscribersAccessor[entityIndexInChunk].AsNativeArrayRO();
                    var stats = statsAccessor[entityIndexInChunk].AsMap();

                    foreach (var subscriber in eventSubscribers)
                    {
                        if (Hint.Likely(subscriber.ConditionType != this.ConditionType))
                        {
                            continue;
                        }

                        var value = (int)stats.Get(subscriber.Key).Value;
                        ReactionUtil.WriteState(subscriber, value, this.ConditionComparisonValues, this.ConditionActives, this.ConditionValues);
                    }
                }
            }
        }
    }
}
