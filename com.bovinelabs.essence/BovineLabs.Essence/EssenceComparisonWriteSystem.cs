// <copyright file="EssenceComparisonWriteSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence
{
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    // We need to update before any events change
    [UpdateInGroup(typeof(ConditionWriteEventsGroup), OrderFirst = true)]
    public partial struct EssenceComparisonWriteSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<Intrinsic, Stat, EventSubscriber>()
                .WithAnyRW<IntrinsicConditionDirty, StatConditionDirty>() // Only run when either stat or intrinsic is dirty
                .Build();

            state.Dependency = new WriteIntrinsicComparisonsJob
            {
                IntrinsicHandle = SystemAPI.GetBufferTypeHandle<Intrinsic>(true),
                StatHandle = SystemAPI.GetBufferTypeHandle<Stat>(true),
                EventSubscriber = SystemAPI.GetBufferTypeHandle<EventSubscriber>(),
                IntrinsicConditionDirtyHandle = SystemAPI.GetComponentTypeHandle<IntrinsicConditionDirty>(),
                StatConditionDirtyHandle = SystemAPI.GetComponentTypeHandle<StatConditionDirty>(),
                EssenceComparisonModeHandle = SystemAPI.GetBufferLookup<EssenceComparisonMode>(true),
                ConditionComparisonValueHandle = SystemAPI.GetBufferLookup<ConditionComparisonValue>(),
            }.ScheduleParallel(query, state.Dependency);
        }

        // Update any comparison values that might have changed
        [BurstCompile]
        private struct WriteIntrinsicComparisonsJob : IJobChunk
        {
            [ReadOnly]
            public BufferTypeHandle<Intrinsic> IntrinsicHandle;

            [ReadOnly]
            public BufferTypeHandle<Stat> StatHandle;

            [ReadOnly]
            public BufferTypeHandle<EventSubscriber> EventSubscriber;

            [ReadOnly]
            public BufferLookup<EssenceComparisonMode> EssenceComparisonModeHandle;

            [NativeDisableParallelForRestriction] // index will be unique
            public BufferLookup<ConditionComparisonValue> ConditionComparisonValueHandle;

            public ComponentTypeHandle<IntrinsicConditionDirty> IntrinsicConditionDirtyHandle;
            public ComponentTypeHandle<StatConditionDirty> StatConditionDirtyHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var intrinsicsAccessor = chunk.GetBufferAccessor(ref this.IntrinsicHandle);
                var statAccessor = chunk.GetBufferAccessor(ref this.StatHandle);
                var eventSubscriberAccessor = chunk.GetBufferAccessor(ref this.EventSubscriber);

                var intrinsicDirties = chunk.GetEnabledMask(ref this.IntrinsicConditionDirtyHandle);
                var statDirties = chunk.GetEnabledMask(ref this.StatConditionDirtyHandle);

                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out var entityIndexInChunk))
                {
                    var intrinsics = intrinsicsAccessor[entityIndexInChunk].AsMap();
                    var stats = statAccessor[entityIndexInChunk].AsMap();
                    var subscribers = eventSubscriberAccessor[entityIndexInChunk];

                    foreach (var subscriber in subscribers)
                    {
                        if (!subscriber.CustomComparison)
                        {
                            continue;
                        }

                        if (!this.EssenceComparisonModeHandle.TryGetBuffer(subscriber.Subscriber, out var essenceComparison))
                        {
                            continue;
                        }

                        var comparisonValues = this.ConditionComparisonValueHandle[subscriber.Subscriber];

                        if (subscriber.Operation == Equality.Between)
                        {
                            foreach (var comparison in essenceComparison)
                            {
                                if (comparison.Index != subscriber.ValueIndex.Min && comparison.Index != subscriber.ValueIndex.Max)
                                {
                                    continue;
                                }

                                UpdateValue(comparison);
                                break;
                            }
                        }
                        else
                        {
                            foreach (var comparison in essenceComparison)
                            {
                                if (comparison.Index != subscriber.ValueIndex.Value)
                                {
                                    continue;
                                }

                                UpdateValue(comparison);
                                break;
                            }
                        }

                        continue;

                        void UpdateValue(EssenceComparisonMode comparison)
                        {
                            if (comparison.IsStat)
                            {
                                var value = stats.GetOrDefault(comparison.Stat);
                                comparisonValues.ElementAt(comparison.Index).Value = (int)math.floor(value.Value);

                                // Mark intrinsic dirty to recheck any intrinsic event
                                intrinsicDirties[entityIndexInChunk] = true;
                            }
                            else
                            {
                                var value = intrinsics.GetOrDefault(comparison.Intrinsic);
                                comparisonValues.ElementAt(comparison.Index).Value = value;

                                // Mark stats dirty to recheck any stat event
                                statDirties[entityIndexInChunk] = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
