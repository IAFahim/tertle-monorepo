// <copyright file="ActionEnableableSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actions
{
    using BovineLabs.Core;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Manages the enabling of components on target entities when reactions become active, using reference counting to support multiple enablers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveEnabledSystemGroup"/> and processes entities that have just become active
    /// (those with the <see cref="Active"/> component but without <see cref="ActivePrevious"/>).
    /// It handles enableable components that implement <see cref="IEnableableComponent"/>.
    /// </para>
    /// <para>
    /// The system performs the following operations for each activated entity:
    /// 1. Iterates through all <see cref="ActionEnableable"/> actions in the entity's buffer
    /// 2. Resolves stable type hashes to <see cref="ComponentType"/> using a pre-built type map
    /// 3. Determines the target entity using the <see cref="Targets"/> system
    /// 4. Increments the reference count for the component on the target entity
    /// 5. Only enables the component when the reference count reaches 1 (first enabler)
    /// </para>
    /// <para>
    /// The system maintains sophisticated reference counting to handle multiple reactions enabling the same
    /// component on the same entity. Components remain enabled until all sources that enabled them have
    /// been deactivated, handled by <see cref="ActionEnableableDeactivatedSystem"/>. This prevents
    /// premature disabling when multiple reactions affect the same targets.
    /// </para>
    /// <para>
    /// Key features include:
    /// - **Dynamic Type Resolution**: Uses <see cref="ReactionEnableables"/> singleton to determine valid component types
    /// - **Reference Counting**: Tracks up to 255 sources enabling the same component
    /// - **Atomic Operations**: Uses <see cref="UnsafeEnableableLookup"/> for thread-safe component enabling
    /// - **Error Handling**: Logs warnings for invalid types or missing components
    /// - **Shared State**: Uses a singleton to share enabled component tracking with the deactivation system
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveEnabledSystemGroup))]
    public partial struct ActionEnableableSystem : ISystem, ISystemStartStop
    {
        private NativeHashMap<ulong, ComponentType> typeIndices;
        private NativeHashMap<EntityComponentKey, byte> enabledComponents;

        internal static void PopulateTypeIndices(ref SystemState state, NativeHashMap<ulong, ComponentType> typeIndices, in DynamicHashSet<ulong> allowed)
        {
            using var e = allowed.GetEnumerator();
            while (e.MoveNext())
            {
                var t = TypeManager.GetTypeIndexFromStableTypeHash(e.Current);
                typeIndices[e.Current] = ComponentType.FromTypeIndex(t);
                state.AddDependency(t);
            }
        }

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.enabledComponents = new NativeHashMap<EntityComponentKey, byte>(0, Allocator.Persistent);
            this.typeIndices = new NativeHashMap<ulong, ComponentType>(0, Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new Singleton
            {
                EnabledComponents = this.enabledComponents,
            });

            state.AddDependency<Singleton>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            var allowed = SystemAPI.GetSingletonBuffer<ReactionEnableables>().AsMap();
            PopulateTypeIndices(ref state, this.typeIndices, allowed);
        }

        /// <inheritdoc/>
        public void OnStopRunning(ref SystemState state)
        {
            state.Dependency.Complete();
            this.typeIndices.Clear();
        }

        /// <inheritdoc />
        public void OnDestroy(ref SystemState state)
        {
            this.typeIndices.Dispose();
            this.enabledComponents.Dispose();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var enabled = new NativeQueue<EntityComponentKey>(state.WorldUpdateAllocator);
            var debug = SystemAPI.GetSingleton<BLLogger>();

            new ActivatedJob
                {
                    AddTags = enabled.AsParallelWriter(),
                    TypeIndices = this.typeIndices,
                    TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                    Logger = debug,
                }
                .ScheduleParallel();

            state.Dependency = new ApplyChangesJob
                {
                    Enables = enabled,
                    UnsafeEnableableLookup = state.GetUnsafeEnableableLookup(),
                    EnabledComponents = this.enabledComponents,
                    Logger = debug,
                }
                .Schedule(state.Dependency);
        }

        internal struct Singleton : IComponentData
        {
            public NativeHashMap<EntityComponentKey, byte> EnabledComponents;
        }

        [BurstCompile]
        [WithChangeFilter(typeof(Active))]
        [WithAll(typeof(Active))]
        [WithDisabled(typeof(ActivePrevious))]
        private partial struct ActivatedJob : IJobEntity
        {
            public NativeQueue<EntityComponentKey>.ParallelWriter AddTags;

            [ReadOnly]
            public NativeHashMap<ulong, ComponentType> TypeIndices;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            public BLLogger Logger;

            private void Execute(Entity entity, in DynamicBuffer<ActionEnableable> actionEnableables, in Targets targets)
            {
                foreach (var action in actionEnableables.AsNativeArrayRO())
                {
                    if (Hint.Unlikely(!this.TypeIndices.TryGetValue(action.Value, out var type)))
                    {
                        this.Logger.LogWarning($"Trying to enable with hash {action.Value} that isn't supported");
                        continue;
                    }

                    var target = targets.Get(action.Target, entity, this.TargetsCustoms);
                    if (Hint.Unlikely(!this.TargetsCustoms.EntityExists(target)))
                    {
                        this.Logger.LogError($"Entity {entity.ToFixedString()} is trying to enable component on target {(int)action.Target} that doesn't exist.");
                        continue;
                    }

                    this.AddTags.Enqueue(new EntityComponentKey(target, type));
                }
            }
        }

        [BurstCompile]
        private struct ApplyChangesJob : IJob
        {
            public NativeQueue<EntityComponentKey> Enables;

            // We add all dependencies to IEnableableComponent
            public UnsafeEnableableLookup UnsafeEnableableLookup;

            public NativeHashMap<EntityComponentKey, byte> EnabledComponents;

            public BLLogger Logger;

            public void Execute()
            {
                while (this.Enables.TryDequeue(out var key))
                {
                    var result = this.EnabledComponents.TryGetValue(key, out var count);
                    Check.Assume(count != 255, "Trying to enabled tag from more than 255 sources");
                    Check.Assume(!result || count != 0, "Tag wasn't cleaned up from hash map");
                    count++;

                    if (count == 1)
                    {
                        this.EnabledComponents.Add(key, count);

                        if (this.UnsafeEnableableLookup.HasComponent(key.Entity, key.Component))
                        {
                            this.UnsafeEnableableLookup.SetComponentEnabled(key.Entity, key.Component, true);
                        }
                        else
                        {
                            this.Logger.LogWarning512(
                                $"Trying to enable component {key.Component.ToFixedString()} on {key.Entity.ToFixedString()} that doesn't have it");
                        }
                    }
                    else
                    {
                        this.EnabledComponents[key] = count;
                    }
                }
            }
        }
    }
}
