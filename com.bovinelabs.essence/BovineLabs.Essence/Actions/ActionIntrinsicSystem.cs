// <copyright file="ActionIntrinsicSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Actions
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Applies intrinsic value changes to target entities when actions activate.
    /// This system processes newly activated actions and applies their intrinsic modifications to affected entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the ActiveEnabledSystemGroup and processes actions that have just become
    /// active (Active enabled, ActivePrevious disabled). It efficiently batches intrinsic changes
    /// by target entity and intrinsic type to minimize writer conflicts and improve efficiency.
    /// </para>
    /// <para>
    /// The activation process:
    /// 1. Identifies newly activated actions with ActionIntrinsic components
    /// 2. Resolves target entities using the Targets system
    /// 3. Batches intrinsic changes by target entity and intrinsic type
    /// 4. Accumulates multiple changes to the same intrinsic on the same entity
    /// 5. Applies all accumulated changes using the IntrinsicWriter system
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveEnabledSystemGroup))]
    public partial struct ActionIntrinsicSystem : ISystem
    {
        private NativeParallelMultiHashMapFallback<Entity, IntrinsicAmount> intrinsicChanges;
        private NativeList<Entity> uniqueKeys;
        private IntrinsicWriter.Lookup intrinsicWriters;

        /// <inheritdoc />
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.intrinsicChanges = new NativeParallelMultiHashMapFallback<Entity, IntrinsicAmount>(64, Allocator.Persistent);
            this.uniqueKeys = new NativeList<Entity>(64, Allocator.Persistent);
            this.intrinsicWriters.Create(ref state);
        }

        /// <inheritdoc />
        public void OnDestroy(ref SystemState state)
        {
            this.intrinsicChanges.Dispose();
            this.uniqueKeys.Dispose();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.intrinsicWriters.Update(ref state, SystemAPI.GetSingleton<EssenceConfig>());

            state.Dependency = new ActivatedJob
            {
                ApplyIntrinsicInstances = this.intrinsicChanges.AsWriter(),
                TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = this.intrinsicChanges.Apply(state.Dependency, out var reader);

            state.Dependency = new GetKeysJob
            {
                UniqueKeys = this.uniqueKeys,
                GroupChanges = reader,
            }.Schedule(state.Dependency);

            state.Dependency = new ApplyJob
            {
                Keys = this.uniqueKeys,
                GroupChanges = reader,
                IntrinsicWriters = this.intrinsicWriters,
            }.Schedule(this.uniqueKeys, 64, state.Dependency);

            state.Dependency = this.intrinsicChanges.Clear(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Active))]
        [WithDisabled(typeof(ActivePrevious))]
        private partial struct ActivatedJob : IJobEntity
        {
            public NativeParallelMultiHashMapFallback<Entity, IntrinsicAmount>.ParallelWriter ApplyIntrinsicInstances;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            private void Execute(Entity entity, in DynamicBuffer<ActionIntrinsic> effectIntrinsics, in Targets targets)
            {
                var effectIntrinsicArray = effectIntrinsics.AsNativeArrayRO();

                foreach (var action in effectIntrinsicArray)
                {
                    var target = targets.Get(action.Target, entity, this.TargetsCustoms);
                    var key = new IntrinsicAmount(action.Intrinsic, action.Amount);
                    this.ApplyIntrinsicInstances.Add(target, key);
                }
            }
        }

        [BurstCompile]
        private struct GetKeysJob : IJob
        {
            public NativeList<Entity> UniqueKeys;

            [ReadOnly]
            public NativeParallelMultiHashMap<Entity, IntrinsicAmount>.ReadOnly GroupChanges;

            public void Execute()
            {
                this.GroupChanges.GetUniqueKeyArray(this.UniqueKeys);
            }
        }

        [BurstCompile]
        private struct ApplyJob : IJobParallelForDefer
        {
            [ReadOnly]
            public NativeList<Entity> Keys;

            [ReadOnly]
            public NativeParallelMultiHashMap<Entity, IntrinsicAmount>.ReadOnly GroupChanges;

            [NativeDisableParallelForRestriction]
            public IntrinsicWriter.Lookup IntrinsicWriters;

            [NativeDisableContainerSafetyRestriction]
            private NativeList<IntrinsicAmount> values;

            public void Execute(int index)
            {
                var key = this.Keys[index];

                if (Hint.Unlikely(!this.IntrinsicWriters.TryGet(key, out var intrinsicWriter)))
                {
                    return;
                }

                if (Hint.Unlikely(!this.values.IsCreated))
                {
                    // We use a list instead of a hashmap as we assume for most cases,
                    // the amount of intrinsic changes a single entity will get in a frame is small
                    this.values = new NativeList<IntrinsicAmount>(Allocator.Temp);
                }

                this.values.Clear();

                this.GroupChanges.TryGetFirstValue(key, out var value, out var it);
                this.values.Add(value);

                while (this.GroupChanges.TryGetNextValue(out value, ref it))
                {
                    var existingIndex = this.values.IndexOf(value);

                    if (Hint.Unlikely(existingIndex == -1))
                    {
                        this.values.Add(value);
                    }
                    else
                    {
                        this.values.ElementAt(existingIndex).Amount += value.Amount;
                    }
                }

                foreach (var i in this.values)
                {
                    intrinsicWriter.Add(i.Intrinsic, i.Amount);
                }
            }
        }

        private struct IntrinsicAmount : IEquatable<IntrinsicAmount>
        {
            public readonly IntrinsicKey Intrinsic;
            public int Amount;

            public IntrinsicAmount(IntrinsicKey intrinsic, int amount)
            {
                this.Intrinsic = intrinsic;
                this.Amount = amount;
            }

            public bool Equals(IntrinsicAmount other)
            {
                return this.Intrinsic.Equals(other.Intrinsic);
            }

            public override int GetHashCode()
            {
                return this.Intrinsic.GetHashCode();
            }
        }
    }
}
