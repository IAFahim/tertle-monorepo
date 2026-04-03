// <copyright file="ConditionIntrinsicWriteSystem.cs" company="BovineLabs">
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
    /// Writes current intrinsic values to the condition system for reaction processing.
    /// This system updates condition values with current intrinsic data when entities are marked as dirty.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the ConditionWriteEventsGroup and processes entities with IntrinsicConditionDirty
    /// enabled. It writes intrinsic values to condition subscribers that are listening for intrinsic-based conditions,
    /// enabling reactions to trigger based on intrinsic value changes.
    /// </para>
    /// <para>
    /// The write process:
    /// 1. Processes entities with EventSubscriber buffers and dirty intrinsic flags
    /// 2. Iterates through condition subscribers looking for intrinsic-type conditions
    /// 3. Reads current intrinsic values from the entity's Intrinsic buffer
    /// 4. Writes intrinsic values to the condition system using ReactionUtil
    /// 5. Clears the IntrinsicConditionDirty flag
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ConditionWriteEventsGroup))]
    public partial struct ConditionIntrinsicWriteSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<EventSubscriber>()
                .WithAll<Intrinsic>()
                .WithAllRW<IntrinsicConditionDirty>()
                .Build();

            state.Dependency = new WriteIntrinsicStateJob
            {
                ConditionActives = SystemAPI.GetComponentLookup<ConditionActive>(),
                ConditionValues = SystemAPI.GetBufferLookup<ConditionValues>(),
                ConditionComparisonValues = SystemAPI.GetBufferLookup<ConditionComparisonValue>(true),
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.IntrinsicType),
                EventSubscriberHandle = SystemAPI.GetBufferTypeHandle<EventSubscriber>(true),
                IntrinsicHandle = SystemAPI.GetBufferTypeHandle<Intrinsic>(true),
                IntrinsicConditionDirtyHandle = SystemAPI.GetComponentTypeHandle<IntrinsicConditionDirty>(),
            }.ScheduleParallel(query, state.Dependency);
        }

        [BurstCompile]
        private struct WriteIntrinsicStateJob : IJobChunk
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
            public BufferTypeHandle<Intrinsic> IntrinsicHandle;

            public ComponentTypeHandle<IntrinsicConditionDirty> IntrinsicConditionDirtyHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var eventSubscribersAccessor = chunk.GetBufferAccessor(ref this.EventSubscriberHandle);
                var intrinsicsAccessor = chunk.GetBufferAccessor(ref this.IntrinsicHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    chunk.SetComponentEnabled(ref this.IntrinsicConditionDirtyHandle, entityIndexInChunk, false);

                    var eventSubscribers = eventSubscribersAccessor[entityIndexInChunk].AsNativeArrayRO();
                    var intrinsics = intrinsicsAccessor[entityIndexInChunk].AsMap();

                    foreach (var subscriber in eventSubscribers)
                    {
                        if (Hint.Likely(subscriber.ConditionType != this.ConditionType))
                        {
                            continue;
                        }

                        var value = intrinsics.GetOrDefault(subscriber.Key);
                        ReactionUtil.WriteState(subscriber, value, this.ConditionComparisonValues, this.ConditionActives, this.ConditionValues);
                    }
                }
            }
        }
    }
}
