// <copyright file="EssenceTestsFixture.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Tests
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Actions;
    using BovineLabs.Essence.Data.Builders;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Tests;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Base test fixture for essence system tests, providing common setup and helper methods.
    /// </summary>
    public abstract class EssenceTestsFixture : ReactionTestFixture
    {
        /// <summary>
        /// Creates a test entity with stat components and initial values using StatsBuilder.
        /// </summary>
        /// <param name="stats">Array of stat modifiers for default values.</param>
        /// <returns>Entity with stat components configured.</returns>
        protected Entity CreateStatEntity(params StatModifier[] stats)
        {
            var entity = this.Manager.CreateEntity();
            var commands = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);

            using var statsBuilder = new StatsBuilder(Allocator.Temp);
            statsBuilder.WithDefaults(new NativeArray<StatModifier>(stats, Allocator.Temp));
            statsBuilder.WithCanBeModified(true);
            statsBuilder.WithWriteEvents(true);

            statsBuilder.ApplyTo(ref commands);

            // Mark as changed to trigger processing
            this.Manager.SetComponentEnabled<StatChanged>(entity, true);

            return entity;
        }

        /// <summary>
        /// Creates a test entity with intrinsic components using IntrinsicBuilder.
        /// </summary>
        /// <param name="intrinsics">Array of intrinsic default values.</param>
        /// <returns>Entity with intrinsic components configured.</returns>
        protected Entity CreateIntrinsicEntity(params IntrinsicBuilder.Default[] intrinsics)
        {
            var entity = this.Manager.CreateEntity();
            var commands = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);

            using var intrinsicBuilder = new IntrinsicBuilder(Allocator.Temp);
            intrinsicBuilder.WithDefaults(new NativeArray<IntrinsicBuilder.Default>(intrinsics, Allocator.Temp));
            intrinsicBuilder.WithWriteEvents(true);

            intrinsicBuilder.ApplyTo(ref commands);

            return entity;
        }

        /// <summary>
        /// Creates a test entity with both stat and intrinsic components.
        /// </summary>
        /// <param name="stats">Array of stat modifiers for default values.</param>
        /// <param name="intrinsics">Array of intrinsic default values.</param>
        /// <returns>Entity with both stat and intrinsic components.</returns>
        protected Entity CreateCombinedEntity(StatModifier[] stats, IntrinsicBuilder.Default[] intrinsics)
        {
            var entity = this.Manager.CreateEntity();
            var commands = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);

            // Apply stats
            using var statsBuilder = new StatsBuilder(Allocator.Temp);
            statsBuilder.WithDefaults(new NativeArray<StatModifier>(stats, Allocator.Temp));
            statsBuilder.WithCanBeModified(true);
            statsBuilder.WithWriteEvents(true);
            statsBuilder.ApplyTo(ref commands);

            // Apply intrinsics
            using var intrinsicBuilder = new IntrinsicBuilder(Allocator.Temp);
            intrinsicBuilder.WithDefaults(new NativeArray<IntrinsicBuilder.Default>(intrinsics, Allocator.Temp));
            intrinsicBuilder.WithWriteEvents(true);
            intrinsicBuilder.ApplyTo(ref commands);

            // Mark as changed to trigger processing
            this.Manager.SetComponentEnabled<StatChanged>(entity, true);

            return entity;
        }

        /// <summary>
        /// Creates a test entity with both stat and intrinsic components using tuples.
        /// </summary>
        /// <param name="statModifiers">Array of stat key-value pairs.</param>
        /// <param name="intrinsics">Array of intrinsic configurations (key, value, minStat, maxStat).</param>
        /// <returns>Entity with both stat and intrinsic components.</returns>
        protected Entity CreateCombinedEntity(StatModifier[] statModifiers, (IntrinsicKey Key, int Value)[] intrinsics)
        {
            var entity = this.Manager.CreateEntity();
            var commands = new EntityManagerCommands(this.Manager, entity, this.BlobAssetStore);

            using var statsBuilder = new StatsBuilder(Allocator.Temp);
            statsBuilder.WithDefaults(new NativeArray<StatModifier>(statModifiers, Allocator.Temp));
            statsBuilder.WithCanBeModified(true);
            statsBuilder.WithWriteEvents(true);
            statsBuilder.ApplyTo(ref commands);

            // Apply intrinsics - convert tuples to IntrinsicBuilder.Default
            var intrinsicDefaults = new IntrinsicBuilder.Default[intrinsics.Length];
            for (int i = 0; i < intrinsics.Length; i++)
            {
                intrinsicDefaults[i] = new IntrinsicBuilder.Default(intrinsics[i].Key, intrinsics[i].Value);
            }

            using var intrinsicBuilder = new IntrinsicBuilder(Allocator.Temp);
            intrinsicBuilder.WithDefaults(new NativeArray<IntrinsicBuilder.Default>(intrinsicDefaults, Allocator.Temp));
            intrinsicBuilder.WithWriteEvents(true);
            intrinsicBuilder.ApplyTo(ref commands);

            // Mark as changed to trigger processing
            this.Manager.SetComponentEnabled<StatChanged>(entity, true);

            return entity;
        }

        /// <summary>
        /// Adds stat modifiers to an entity.
        /// </summary>
        /// <param name="entity">Target entity.</param>
        /// <param name="modifiers">Array of stat modifiers to add.</param>
        protected void AddStatModifiers(Entity entity, params StatModifier[] modifiers)
        {
            var modifierBuffer = this.Manager.GetBuffer<StatModifiers>(entity);
            foreach (var modifier in modifiers)
            {
                modifierBuffer.Add(new StatModifiers
                {
                    SourceEntity = Entity.Null,
                    Value = modifier,
                });
            }

            // Mark as changed
            this.Manager.SetComponentEnabled<StatChanged>(entity, true);
        }

        /// <summary>
        /// Creates test intrinsic configuration singleton.
        /// </summary>
        /// <param name="intrinsicConfigs">Array of intrinsic configurations.</param>
        /// <returns>IntrinsicConfig component with configured blob asset.</returns>
        protected EssenceConfig CreateTestIntrinsicConfig(
            (IntrinsicKey Key, int DefaultValue, int Min, int Max, StatKey MinStat, StatKey MaxStat)[] intrinsicConfigs)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<EssenceConfig.Data>();

            // Build intrinsic data
            var intrinsicBuilder = builder.AllocateHashMap(ref root.IntrinsicDatas, intrinsicConfigs.Length, 2);

            var statMap = new NativeMultiHashMap<StatKey, EssenceConfig.StatLimit>(intrinsicConfigs.Length, Allocator.Temp);

            foreach (var (key, defaultValue, min, max, minStat, maxStat) in intrinsicConfigs)
            {
                intrinsicBuilder.Add(key, new EssenceConfig.IntrinsicData
                {
                    DefaultValue = defaultValue,
                    Event = default,
                    Min = min,
                    Max = max,
                    MinStatKey = minStat,
                    MaxStatKey = maxStat,
                });

                if (minStat != 0)
                {
                    statMap.Add(minStat, new EssenceConfig.StatLimit
                    {
                        Intrinsic = key,
                        IsMin = true,
                    });
                }

                if (maxStat != 0)
                {
                    statMap.Add(maxStat, new EssenceConfig.StatLimit
                    {
                        Intrinsic = key,
                        IsMin = false,
                    });
                }
            }

            // Build stat limits
            var statBuilder = builder.AllocateMultiHashMap(ref root.StatsLimitIntrinsics,  statMap.Count, 2);
            foreach (var s in statMap)
            {
                statBuilder.Add(s.Key, s.Value);
            }

            var blobAsset = builder.CreateBlobAssetReference<EssenceConfig.Data>(Allocator.Persistent);
            builder.Dispose();

            this.BlobAssetStore.TryAdd(ref blobAsset);

            return new EssenceConfig { Value = blobAsset };
        }

        /// <summary>
        /// Sets up the intrinsic configuration singleton in the world.
        /// </summary>
        /// <param name="config">The intrinsic configuration to set up.</param>
        protected void SetupIntrinsicConfig(EssenceConfig config)
        {
            var configEntity = this.Manager.CreateEntity();
            this.Manager.AddComponentData(configEntity, config);
        }

        /// <summary>
        /// Helper to create a StatModifier for testing.
        /// </summary>
        /// <param name="statKey">The stat to modify.</param>
        /// <param name="value">The modification value.</param>
        /// <param name="modifyType">The type of modification.</param>
        /// <returns>A StatModifier with the specified configuration.</returns>
        protected static StatModifier CreateStatModifier(StatKey statKey, float value, StatModifyType modifyType)
        {
            var sm = new StatModifier
            {
                Type = statKey,
                ModifyType = modifyType,
            };

            if (modifyType == StatModifyType.Added)
            {
                sm.Value = (int)value;
            }
            else
            {
                sm.ValueFloat = value;
            }

            return sm;
        }

        /// <summary>
        /// Helper to create an IntrinsicBuilder.Default for testing.
        /// </summary>
        /// <param name="key">The intrinsic key.</param>
        /// <param name="value">The default value.</param>
        /// <returns>An IntrinsicBuilder.Default with the specified configuration.</returns>
        protected static IntrinsicBuilder.Default CreateIntrinsicDefault(IntrinsicKey key, int value)
        {
            return new IntrinsicBuilder.Default(key, value);
        }

        /// <summary>
        /// Creates a reaction entity with ActionStat buffer for testing action systems.
        /// The entity is set up as "newly activated" (Active enabled, ActivePrevious disabled).
        /// </summary>
        /// <param name="target">The target entity for the reaction.</param>
        /// <param name="actionStats">Array of ActionStat components to add.</param>
        /// <returns>Entity configured for action system testing.</returns>
        protected Entity CreateActionStatEntity(Entity target, params ActionStat[] actionStats)
        {
            var entity = this.CreateReactionEntity(target);

            var actionStatBuffer = this.Manager.AddBuffer<ActionStat>(entity);
            foreach (var actionStat in actionStats)
            {
                actionStatBuffer.Add(actionStat);
            }

            return entity;
        }

        /// <summary>
        /// Creates a reaction entity with stat capabilities (can receive stat modifiers).
        /// Useful when the reaction entity itself needs to be a target.
        /// </summary>
        /// <param name="target">The primary target entity for the reaction.</param>
        /// <param name="withStatComponents">Whether to add stat components to the reaction entity.</param>
        /// <returns>Entity configured for reaction testing with optional stat capabilities.</returns>
        protected Entity CreateReactionEntityWithStats(Entity target = default, bool withStatComponents = true)
        {
            var entity = this.CreateReactionEntity(target);

            if (withStatComponents)
            {
                this.Manager.AddBuffer<StatModifiers>(entity);
                this.Manager.AddComponentData(entity, new StatChanged());
            }

            return entity;
        }

        /// <summary>
        /// Runs a single system and completes all tracked jobs.
        /// </summary>
        /// <param name="system">The system to update.</param>
        protected void RunSystem(SystemHandle system)
        {
            system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }

        /// <summary>
        /// Creates a subscriber entity with ConditionActive and ConditionValues components for condition testing.
        /// </summary>
        /// <returns>Entity configured for condition subscriber testing.</returns>
        protected Entity CreateConditionSubscriberEntity()
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionActive),
                typeof(ConditionValues));

            var entity = this.Manager.CreateEntity(archetype);

            // Initialize enough condition values for our tests
            this.Manager.GetBuffer<ConditionValues>(entity).ResizeInitialized(32);

            return entity;
        }

        /// <summary>
        /// Creates a stat entity with EventSubscriber buffer for condition testing.
        /// </summary>
        /// <param name="stats">Array of stat modifiers for default values.</param>
        /// <returns>Entity with stat components and EventSubscriber buffer.</returns>
        protected Entity CreateStatEntityWithSubscriber(params StatModifier[] stats)
        {
            var entity = this.CreateStatEntity(stats);
            this.Manager.AddBuffer<EventSubscriber>(entity);
            this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            return entity;
        }

        /// <summary>
        /// Creates an intrinsic entity with EventSubscriber buffer for condition testing.
        /// </summary>
        /// <param name="intrinsics">Array of intrinsic default values.</param>
        /// <returns>Entity with intrinsic components and EventSubscriber buffer.</returns>
        protected Entity CreateIntrinsicEntityWithSubscriber(params IntrinsicBuilder.Default[] intrinsics)
        {
            var entity = this.CreateIntrinsicEntity(intrinsics);
            this.Manager.AddBuffer<EventSubscriber>(entity);
            this.Manager.AddBuffer<ConditionComparisonValue>(entity);
            return entity;
        }

        /// <summary>
        /// Creates an ActionStat component with fixed value for testing.
        /// </summary>
        /// <param name="statKey">The stat to modify.</param>
        /// <param name="value">The fixed value to apply.</param>
        /// <param name="modifyType">The type of modification.</param>
        /// <param name="target">The target type.</param>
        /// <returns>Configured ActionStat component.</returns>
        protected static ActionStat CreateFixedActionStat(StatKey statKey, int value, StatModifyType modifyType, Target target)
        {
            return new ActionStat
            {
                Type = statKey,
                ModifyType = modifyType,
                ValueType = StatValueType.Fixed,
                Target = target,
                Fixed = new ActionStat.ValueUnion { Int = value },
            };
        }

        /// <summary>
        /// Creates an ActionStat component with linear value for testing.
        /// </summary>
        /// <param name="statKey">The stat to modify.</param>
        /// <param name="conditionIndex">Index into the condition values buffer.</param>
        /// <param name="fromMin">Input range minimum.</param>
        /// <param name="fromMax">Input range maximum.</param>
        /// <param name="toMin">Output range minimum.</param>
        /// <param name="toMax">Output range maximum.</param>
        /// <param name="modifyType">The type of modification.</param>
        /// <param name="target">The target type.</param>
        /// <returns>Configured ActionStat component.</returns>
        protected static ActionStat CreateLinearActionStat(StatKey statKey, byte conditionIndex, int fromMin, int fromMax,
            ActionStat.ValueUnion toMin, ActionStat.ValueUnion toMax, StatModifyType modifyType, Target target)
        {
            return new ActionStat
            {
                Type = statKey,
                ModifyType = modifyType,
                ValueType = StatValueType.Linear,
                Target = target,
                Linear = new ActionStat.LinearData
                {
                    Index = conditionIndex,
                    FromMin = fromMin,
                    FromMax = fromMax,
                    ToMin = toMin,
                    ToMax = toMax,
                },
            };
        }

    }
}
