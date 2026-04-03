// <copyright file="ConditionSubscribedSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence
{
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Conditions;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Ensures that newly registered condition subscribers receive current stat and intrinsic values.
    /// This system marks entities as dirty when new condition subscribers are added so their current state is written to the condition system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the ConditionWriteEventsGroup before ConditionEventWriteSystem and handles
    /// the case where condition subscribers are added to entities that already have stat or intrinsic values.
    /// It ensures these existing values are written to the new subscribers by marking entities as dirty.
    /// </para>
    /// <para>
    /// The subscription process:
    /// 1. Detects changes to EventSubscriber buffers using change filters
    /// 2. Checks if new subscribers are listening for stat or intrinsic conditions
    /// 3. Enables StatConditionDirty or IntrinsicConditionDirty flags as appropriate
    /// 4. Allows condition write systems to process the newly dirty entities
    /// 5. Runs separate jobs for stat and intrinsic subscribers in parallel
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ConditionWriteEventsGroup))]
    [UpdateBefore(typeof(ConditionEventWriteSystem))]
    public partial struct ConditionSubscribedSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statDependency = new StatSubscriptionJob
            {
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.StatType),
            }.ScheduleParallel(state.Dependency);

            var intrinsicDependency = new IntrinsicSubscriptionJob
            {
                ConditionType = ConditionTypes.NameToKey(ConditionTypes.IntrinsicType),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = JobHandle.CombineDependencies(statDependency, intrinsicDependency);
        }

        [BurstCompile]
        [WithAll(typeof(Stat))]
        [WithDisabled(typeof(StatConditionDirty))] // No need to trigger if already triggered this frame
        [WithChangeFilter(typeof(EventSubscriber))] // TODO probably should use an enable component
        private partial struct StatSubscriptionJob : IJobEntity
        {
            public byte ConditionType;

            private void Execute(in DynamicBuffer<EventSubscriber> eventSubscribers, EnabledRefRW<StatConditionDirty> statDirty)
            {
                foreach (var subscriber in eventSubscribers)
                {
                    if (subscriber.ConditionType == this.ConditionType)
                    {
                        statDirty.ValueRW = true;
                        return;
                    }
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(Intrinsic))]
        [WithDisabled(typeof(IntrinsicConditionDirty))]
        [WithChangeFilter(typeof(EventSubscriber))]
        private partial struct IntrinsicSubscriptionJob : IJobEntity
        {
            public byte ConditionType;

            private void Execute(in DynamicBuffer<EventSubscriber> eventSubscribers, EnabledRefRW<IntrinsicConditionDirty> intrinsicDirty)
            {
                foreach (var subscriber in eventSubscribers)
                {
                    if (subscriber.ConditionType == this.ConditionType)
                    {
                        intrinsicDirty.ValueRW = true;
                        return;
                    }
                }
            }
        }
    }
}
