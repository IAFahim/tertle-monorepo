// <copyright file="InitializeStatsSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence
{
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// Initializes stat values for newly created entities by copying stats from source entities.
    /// This system allows entities to inherit stat values from other entities during initialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs only on server worlds during entity initialization. It processes entities
    /// with InitializeEntity components and copies stat values from specified source entities
    /// based on configured target relationships.
    /// </para>
    /// <para>
    /// The initialization process:
    /// 1. Looks up InitializeStats configuration for the entity's ObjectId
    /// 2. Resolves the source entity based on Target specification (Owner, Source, Target, etc.)
    /// 3. Copies all stat values from the source entity to the initializing entity
    /// 4. Only processes entities without StatModifiers to avoid modifier conflicts
    /// </para>
    /// </remarks>
    [BurstCompile]
    [WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct InitializeStatsSystem : ISystem
    {
        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InitializeStats>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var map = SystemAPI.QueryBuilder().WithAll<InitializeStats>().Build()
                .GetSingletonBufferNoSync<InitializeStats>(true)
                .AsHashMap<InitializeStats, ObjectId, InitializeStats.Data>();

            new InitializeStatsObjectJob
                {
                    Stats = SystemAPI.GetBufferLookup<Stat>(true),
                    InitializeStats = map,
                    Logger = SystemAPI.GetSingleton<BLLogger>(),
                }
                .Schedule();
        }

        [BurstCompile]
        [WithAll(typeof(InitializeEntity))]
        [WithAbsent(typeof(StatModifiers))] // Things that are modifiable can't copy stats as modifiers would never be removed
        private partial struct InitializeStatsObjectJob : IJobEntity
        {
            [ReadOnly]
            [NativeDisableContainerSafetyRestriction]
            public BufferLookup<Stat> Stats;

            [ReadOnly]
            public DynamicHashMap<ObjectId, InitializeStats.Data> InitializeStats;

            public BLLogger Logger;

            private void Execute(Entity entity, DynamicBuffer<Stat> stats, in ObjectId objectId, in Targets targets)
            {
                if (!this.InitializeStats.TryGetValue(objectId, out var data))
                {
                    return;
                }

                this.TryGetTargetStats(entity, data.Source, targets, out var source);

                if (!source.IsCreated)
                {
                    return;
                }

                stats.CopyFrom(source);
            }

            private void TryGetTargetStats(
                Entity self, Target target, Targets targets, out DynamicBuffer<Stat> stats)
            {
                var entity = target switch
                {
                    Target.None => Entity.Null,
                    Target.Target => targets.Target,
                    Target.Owner => targets.Owner,
                    Target.Source => targets.Source,
                    Target.Self => self,
                    _ => Entity.Null,
                };

                if (entity == self || target == Target.None)
                {
                    stats = default;
                    return;
                }

                if (Hint.Likely(this.Stats.TryGetBuffer(entity, out stats)))
                {
                    return;
                }

                stats = default;
                this.Logger.LogWarning($"Target {entity.ToFixedString()} from {(byte)target} on {self.ToFixedString()} does not have a Stat buffer");
            }
        }
    }
}
