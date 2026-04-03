// <copyright file="ConditionDestroySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Conditions
{
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Reaction.Actives;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Handles proper cleanup of condition subscriptions and global condition registrations when entities are destroyed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="DestroySystemGroup"/> on server and local worlds only, updating
    /// after <see cref="ConditionInitializeSystem"/> and before <see cref="ActiveDisableOnDestroySystem"/>
    /// to ensure proper cleanup sequencing during entity destruction.
    /// </para>
    /// <para>
    /// The system performs two critical cleanup operations:
    /// 1. **Global Condition Cleanup**: Removes global condition registrations from the shared lookup
    ///    when entities providing global conditions are destroyed
    /// 2. **Subscription Cleanup**: Unsubscribes destroyed condition entities from all their target
    ///    entities' <see cref="EventSubscriber"/> buffers and <see cref="LinkedEntityGroup"/> entries
    /// </para>
    /// <para>
    /// The subscription cleanup process:
    /// 1. Iterates through all conditions defined in the entity's <see cref="ConditionMeta"/>
    /// 2. Resolves target entities using the <see cref="Targets"/> system
    /// 3. Removes event subscriptions and linked entity group entries from each target
    /// 4. Optimizes by skipping cleanup when target entities are also being destroyed
    /// </para>
    /// <para>
    /// This system prevents orphaned subscriptions and ensures that destroyed condition entities
    /// don't leave references in target entities' buffers, maintaining system integrity during
    /// complex entity destruction scenarios.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal)]
    [CreateAfter(typeof(ConditionInitializeSystem))]
    [UpdateBefore(typeof(ActiveDisableOnDestroySystem))]
    [UpdateInGroup(typeof(DestroySystemGroup))]
    public partial struct ConditionDestroySystem : ISystem
    {
        private NativeHashMap<ConditionGlobal, Entity> globalConditions;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.globalConditions = SystemAPI.GetSingleton<ConditionInitializeSystem.Singleton>().GlobalConditions;
            state.AddDependency<ConditionInitializeSystem.Singleton>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new GlobalDestroyJob { GlobalConditions = this.globalConditions }.Schedule();

            new DestroyJob
                {
                    GlobalConditions = this.globalConditions,
                    EventSubscribers = SystemAPI.GetBufferLookup<EventSubscriber>(),
                    LinkedEntityGroups = SystemAPI.GetBufferLookup<LinkedEntityGroup>(),
                    DestroyEntitys = SystemAPI.GetComponentLookup<DestroyEntity>(true),
                    TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                }
                .Schedule();
        }

        [BurstCompile]
        [WithAll(typeof(DestroyEntity))]
        private partial struct GlobalDestroyJob : IJobEntity
        {
            public NativeHashMap<ConditionGlobal, Entity> GlobalConditions;

            private void Execute(in DynamicBuffer<ConditionGlobal> globalConditions)
            {
                foreach (var condition in globalConditions)
                {
                    this.GlobalConditions.Remove(condition);
                }
            }
        }

        /// <summary> Destroying a condition requires unsubscribing from all targets. </summary>
        [BurstCompile]
        [WithAll(typeof(DestroyEntity))]
        private partial struct DestroyJob : IJobEntity
        {
            [ReadOnly]
            public NativeHashMap<ConditionGlobal, Entity> GlobalConditions;

            public BufferLookup<EventSubscriber> EventSubscribers;
            public BufferLookup<LinkedEntityGroup> LinkedEntityGroups;

            [ReadOnly]
            public ComponentLookup<DestroyEntity> DestroyEntitys;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            private void Execute(Entity entity, in ConditionMeta conditionMeta, in Targets targets)
            {
                ref var metaData = ref conditionMeta.Value.Value;

                var subscriptions = new FixedList512Bytes<(Entity Target, byte Count, byte DestroyCount)>();

                this.GetAllSubscriptionTargets(ref subscriptions, ref metaData, entity, targets);

                foreach (var kvp in subscriptions)
                {
                    // Target is also destroyed, no need to cleanup buffers.
                    // LEG would have been done for us in DestroyOnDestroySystem and EventSubscriber is unimportant
                    var destroy = this.DestroyEntitys.GetEnabledRefROOptional<DestroyEntity>(kvp.Target);
                    if (destroy is { IsValid: true, ValueRO: true })
                    {
                        continue;
                    }

                    if (this.EventSubscribers.TryGetBuffer(kvp.Target, out var es))
                    {
                        var count = kvp.Count;
                        var esn = es.AsNativeArray();
                        for (var j = esn.Length - 1; j >= 0 && count > 0; j--)
                        {
                            if (esn[j].Subscriber == entity)
                            {
                                es.RemoveAtSwapBack(j);
                                count--;
                            }
                        }
                    }

                    if (this.LinkedEntityGroups.TryGetBuffer(kvp.Target, out var leg))
                    {
                        var count = kvp.DestroyCount;
                        var legn = leg.AsNativeArray();
                        for (var j = legn.Length - 1; j >= 0 && count > 0; j--)
                        {
                            if (legn[j].Value == entity)
                            {
                                leg.RemoveAtSwapBack(j);
                                count--;
                            }
                        }
                    }
                }
            }

            private void GetAllSubscriptionTargets(ref FixedList512Bytes<(Entity Target, byte Count, byte DestroyCount)> subscriptions, ref ConditionMetaData metaData,
                Entity entity, in Targets targets)
            {
                var uniqueTargets = new FixedList64Bytes<(Target Target, byte Count, byte DestroyCount)>();

                for (byte i = 0; i < metaData.Conditions.Length; i++)
                {
                    ref var condition = ref metaData.Conditions[i];

                    // Global Conditions
                    if (condition.Target == Target.None)
                    {
                        this.AddGlobal(ref subscriptions, ref condition);
                    }
                    else
                    {
                        AddTarget(ref uniqueTargets, ref condition);
                    }
                }

                // Get the actual entity from Target and merge into the subscription list
                foreach (var u in uniqueTargets)
                {
                    var target = targets.Get(u.Target, entity, this.TargetsCustoms);
                    subscriptions.Add((target, u.Count, u.DestroyCount));
                }
            }

            private void AddGlobal(ref FixedList512Bytes<(Entity Target, byte Count, byte DestroyCount)> subscriptions, ref ConditionData condition)
            {
                if (!this.GlobalConditions.TryGetValue(new ConditionGlobal(condition.Key, condition.ConditionType), out var target))
                {
                    // Global target destroyed already
                    return;
                }

                var contains = false;

                for (var index = 0; index < subscriptions.Length; index++)
                {
                    ref var t = ref subscriptions.ElementAt(index);
                    if (t.Target != target)
                    {
                        continue;
                    }

                    t.Count++;
                    if (condition.DestroyOnTargetDestroyed)
                    {
                        t.DestroyCount++;
                    }

                    contains = true;
                    break;
                }

                if (!contains)
                {
                    subscriptions.Add((target, 1, (byte)(condition.DestroyOnTargetDestroyed ? 1 : 0)));
                }
            }

            private static void AddTarget(ref FixedList64Bytes<(Target Target, byte Count, byte DestroyCount)> uniqueTargets, ref ConditionData c)
            {
                var contains = false;

                for (var index = 0; index < uniqueTargets.Length; index++)
                {
                    ref var t = ref uniqueTargets.ElementAt(index);
                    if (t.Target != c.Target)
                    {
                        continue;
                    }

                    t.Count++;
                    if (c.DestroyOnTargetDestroyed)
                    {
                        t.DestroyCount++;
                    }

                    contains = true;
                    break;
                }

                if (!contains)
                {
                    uniqueTargets.Add((c.Target, 1, (byte)(c.DestroyOnTargetDestroyed ? 1 : 0)));
                }
            }
        }
    }
}
