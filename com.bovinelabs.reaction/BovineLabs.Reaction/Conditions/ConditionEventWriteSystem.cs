// <copyright file="ConditionEventWriteSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Conditions
{
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Reaction.Core;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// Processes event-based conditions by evaluating event data against subscriber criteria and updating condition states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ConditionWriteEventsGroup"/> and handles the processing of
    /// event-based conditions. It processes entities with <see cref="EventSubscriber"/> buffers and
    /// <see cref="ConditionEvent"/> data, evaluating events against condition criteria.
    /// </para>
    /// <para>
    /// The system performs the following operations for each entity with events:
    /// 1. Iterates through all event subscribers registered to the entity
    /// 2. Matches event types with subscriber condition types
    /// 3. Retrieves event values from the <see cref="ConditionEvent"/> buffer
    /// 4. Performs equality checks using <see cref="ReactionUtil.EqualityCheck"/>
    /// 5. Updates subscriber <see cref="ConditionActive"/> states using atomic operations
    /// 6. Handles value accumulation and storage for conditions with features
    /// </para>
    /// <para>
    /// The system supports advanced condition features:
    /// - **Value Storage**: Conditions can store and access event values through <see cref="ConditionValues"/>
    /// - **Accumulation**: Values can be accumulated across multiple events when configured
    /// - **Atomic Updates**: Uses thread-safe atomic operations to set condition bits in parallel jobs
    /// - **Event Cleanup**: Automatically clears processed events and resets accumulation values
    /// </para>
    /// <para>
    /// Event processing is optimized for intermittent events, only processing subscribers when
    /// matching event data is available. The system resets the <see cref="EventsDirty"/> flag
    /// after processing to track when new events arrive.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ConditionWriteEventsGroup))]
    public partial struct ConditionEventWriteSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<EventSubscriber>()
                .WithAllRW<ConditionEvent>()
                .WithAllRW<EventsDirty>()
                .Build();

            state.Dependency = new WriteEventsJob
            {
                ConditionActives = SystemAPI.GetComponentLookup<ConditionActive>(),
                ConditionValues = SystemAPI.GetBufferLookup<ConditionValues>(),
                ConditionComparisonValues = SystemAPI.GetBufferLookup<ConditionComparisonValue>(true),
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.EventType),
                EventSubscriberHandle = SystemAPI.GetBufferTypeHandle<EventSubscriber>(true),
                ConditionEventHandle = SystemAPI.GetBufferTypeHandle<ConditionEvent>(),
                EventsDirtyHandle = SystemAPI.GetComponentTypeHandle<EventsDirty>(),
            }.ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private struct WriteEventsJob : IJobChunk
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

            public BufferTypeHandle<ConditionEvent> ConditionEventHandle;

            public ComponentTypeHandle<EventsDirty> EventsDirtyHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var eventSubscribersAccessor = chunk.GetBufferAccessor(ref this.EventSubscriberHandle);
                var conditionEventsAccessor = chunk.GetBufferAccessor(ref this.ConditionEventHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    chunk.SetComponentEnabled(ref this.EventsDirtyHandle, entityIndexInChunk, false);

                    var eventSubscribers = eventSubscribersAccessor[entityIndexInChunk].AsNativeArrayRO();
                    var conditionEventBuffer = conditionEventsAccessor[entityIndexInChunk];
                    var conditionEvents = conditionEventBuffer.AsMap();

                    foreach (var subscriber in eventSubscribers)
                    {
                        if (subscriber.ConditionType != this.ConditionType)
                        {
                            continue;
                        }

                        // The assumption is events should be intermittent
                        if (!conditionEvents.TryGetValue(subscriber.Key, out var value))
                        {
                            continue;
                        }

                        Check.Assume(subscriber.Index < ConditionActive.MaxConditions);

                        var isAccumulate = false;
                        NativeArray<int> values = default;

                        if (Hint.Unlikely(subscriber.Feature.HasValue()))
                        {
                            values = this.ConditionValues[subscriber.Subscriber].AsNativeArray().Reinterpret<int>();

                            if (subscriber.Feature.IsAccumulate())
                            {
                                isAccumulate = true;
                                value += values[subscriber.Index];
                            }

                            values[subscriber.Index] = value;
                        }

                        if (Hint.Unlikely(!subscriber.Feature.HasCondition()))
                        {
                            continue;
                        }

                        var match = ReactionUtil.EqualityCheck(subscriber, this.ConditionComparisonValues, value);

                        if (match)
                        {
                            ref var conditions = ref this.ConditionActives.GetRefRW(subscriber.Subscriber).ValueRW.Value;
                            ref var bitField = ref UnsafeUtility.As<BitArray32, uint>(ref conditions);

#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS
                            Common.InterlockedOr(ref bitField, 1u << subscriber.Index);
#else
                            throw new System.Exception("UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS not set");
#endif

                            // If we are accumulate we should reset // TODO make respect RESET NO?
                            // TODO move reset to conditionActive trigger I think
                            // OR RESET?
                            if (Hint.Unlikely(isAccumulate))
                            {
                                values[subscriber.Index] = 0;
                            }
                        }
                    }

                    conditionEvents.Clear();
                }
            }
        }
    }
}
