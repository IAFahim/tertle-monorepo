// <copyright file="ConditionInitializeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Conditions
{
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Initializes condition subscriptions and global condition registrations for newly created entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="InitializeSystemGroup"/> on server and local worlds only,
    /// processing entities with <see cref="InitializeEntity"/> or <see cref="InitializeSubSceneEntity"/>
    /// components. It establishes the subscription relationships needed for condition-based reactions.
    /// </para>
    /// <para>
    /// The system performs two primary initialization tasks:
    /// 1. **Global Condition Registration**: Entities with <see cref="ConditionGlobal"/> buffers
    ///    register themselves in the shared global conditions lookup, making them available
    ///    as targets for global condition subscriptions
    /// 2. **Condition Subscription**: Entities with <see cref="ConditionMeta"/> create
    ///    <see cref="EventSubscriber"/> entries on their target entities to receive condition updates
    /// </para>
    /// <para>
    /// The subscription initialization process:
    /// 1. Iterates through all conditions defined in the entity's <see cref="ConditionMeta"/>
    /// 2. Resolves target entities using the <see cref="Targets"/> system or global lookups
    /// 3. Creates <see cref="EventSubscriber"/> entries on target entities with condition criteria
    /// 4. Adds entities to target <see cref="LinkedEntityGroup"/> buffers for destruction cleanup
    ///    when <see cref="ConditionData.DestroyOnTargetDestroyed"/> is enabled
    /// </para>
    /// <para>
    /// This system maintains a persistent <see cref="NativeHashMap"/> of global conditions shared
    /// with <see cref="ConditionDestroySystem"/> for proper lifecycle management.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal)]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct ConditionInitializeSystem : ISystem
    {
        private NativeHashMap<ConditionGlobal, Entity> globalConditions;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.globalConditions = new NativeHashMap<ConditionGlobal, Entity>(0, Allocator.Persistent);
            state.EntityManager.AddComponentData(state.SystemHandle, new Singleton { GlobalConditions = this.globalConditions });
            state.AddDependency<Singleton>();
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
            this.globalConditions.Dispose();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new InitializeGlobalJob { GlobalConditions = this.globalConditions }.Schedule();

            new InitializeJob
                {
                    GlobalConditions = this.globalConditions,
                    EventSubscribers = SystemAPI.GetBufferLookup<EventSubscriber>(),
                    LinkedEntityGroups = SystemAPI.GetBufferLookup<LinkedEntityGroup>(),
                    TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                    Logger = SystemAPI.GetSingleton<BLLogger>(),
                }
                .Schedule();
        }

        internal struct Singleton : IComponentData
        {
            public NativeHashMap<ConditionGlobal, Entity> GlobalConditions;
        }

        [BurstCompile]
        [WithAny(typeof(InitializeEntity), typeof(InitializeSubSceneEntity))]
        private partial struct InitializeGlobalJob : IJobEntity
        {
            public NativeHashMap<ConditionGlobal, Entity> GlobalConditions;

            private void Execute(Entity entity, in DynamicBuffer<ConditionGlobal> globalConditions)
            {
                foreach (var condition in globalConditions)
                {
                    this.GlobalConditions.Add(new ConditionGlobal(condition.Key, condition.ConditionType), entity);
                }
            }
        }

        [BurstCompile]
        [WithAny(typeof(InitializeEntity), typeof(InitializeSubSceneEntity))]
        private partial struct InitializeJob : IJobEntity
        {
            [ReadOnly]
            public NativeHashMap<ConditionGlobal, Entity> GlobalConditions;

            public BufferLookup<EventSubscriber> EventSubscribers;
            public BufferLookup<LinkedEntityGroup> LinkedEntityGroups;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            public BLLogger Logger;

            private void Execute(Entity entity, in ConditionMeta conditionMeta, in Targets targets)
            {
                ref var metaData = ref conditionMeta.Value.Value;

                for (byte i = 0; i < metaData.Conditions.Length; i++)
                {
                    ref readonly var c = ref metaData.Conditions[i];

                    var subscription = new EventSubscriber
                    {
                        Subscriber = entity,
                        Key = c.Key,
                        ConditionType = c.ConditionType,
                        Feature = c.Feature,
                        Index = i,
                        Operation = c.Operation,
                        CustomComparison = c.CustomComparison,
                        ValueIndex = c.ValueIndex,
                    };

                    Entity target;

                    // None is assumed to be global
                    if (c.Target == Target.None)
                    {
                        if (!this.GlobalConditions.TryGetValue(new ConditionGlobal(c.Key, c.ConditionType), out target))
                        {
                            this.Logger.LogError($"Trying to use global condition ({c.Key}, {c.ConditionType}) that hasn't been setup");
                            continue;
                        }
                    }
                    else
                    {
                        target = targets.Get(c.Target, entity, this.TargetsCustoms);
                        if (target == Entity.Null)
                        {
                            this.Logger.LogError($"Trying to use target {c.Target} that resulted in a Entity.Null");
                            continue;
                        }
                    }

                    this.EventSubscribers[target].Add(subscription);

                    if (c.DestroyOnTargetDestroyed)
                    {
                        this.LinkedEntityGroups[target].Add(new LinkedEntityGroup { Value = entity });
                    }
                }
            }
        }
    }
}
