// <copyright file="ActionEnableableDeactivatedSystem.cs" company="BovineLabs">
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
    /// Handles the disabling of enableable components when reactions are deactivated, managing reference counting to support multiple enablers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveDisabledSystemGroup"/> and is created after <see cref="ActionEnableableSystem"/>
    /// to ensure proper system dependency ordering. It processes entities that were previously active but are now disabled
    /// (those with <see cref="ActivePrevious"/> but without <see cref="Active"/>).
    /// </para>
    /// <para>
    /// The system performs the following operations for each deactivated entity:
    /// 1. Iterates through all <see cref="ActionEnableable"/> actions in the entity's buffer
    /// 2. Resolves the stable type hash to a <see cref="ComponentType"/> using the shared type indices
    /// 3. Determines the target entity using the <see cref="Targets"/> system
    /// 4. Decrements the reference count for the component on the target entity
    /// 5. Only disables the component when the reference count reaches zero, ensuring components
    ///    enabled by multiple sources remain enabled until all sources are deactivated
    /// </para>
    /// <para>
    /// This system shares the enabled components tracking map with <see cref="ActionEnableableSystem"/> to maintain
    /// consistent reference counting across enable/disable operations. Components are only disabled when no active
    /// reactions are requesting them to be enabled.
    /// </para>
    /// </remarks>
    [CreateAfter(typeof(ActionEnableableSystem))]
    [UpdateInGroup(typeof(ActiveDisabledSystemGroup))]
    public partial struct ActionEnableableDeactivatedSystem : ISystem, ISystemStartStop
    {
        private NativeHashMap<ulong, ComponentType> typeIndices;
        private NativeHashMap<EntityComponentKey, byte> enabledComponents;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.typeIndices = new NativeHashMap<ulong, ComponentType>(0, Allocator.Persistent);
            this.enabledComponents = SystemAPI.GetSingleton<ActionEnableableSystem.Singleton>().EnabledComponents;
            state.AddDependency<ActionEnableableSystem.Singleton>();
        }

        /// <inheritdoc/>
        public void OnDestroy(ref SystemState state)
        {
            this.typeIndices.Dispose();
        }

        /// <inheritdoc/>
        public void OnStartRunning(ref SystemState state)
        {
            var allowed = SystemAPI.GetSingletonBuffer<ReactionEnableables>().AsMap();
            ActionEnableableSystem.PopulateTypeIndices(ref state, this.typeIndices, allowed);
        }

        /// <inheritdoc/>
        public void OnStopRunning(ref SystemState state)
        {
            this.typeIndices.Clear();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var disabled = new NativeQueue<EntityComponentKey>(state.WorldUpdateAllocator);
            var debug = SystemAPI.GetSingleton<BLLogger>();

            new DeactivateJob
                {
                    RemoveTags = disabled.AsParallelWriter(),
                    TypeIndices = this.typeIndices,
                    TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                    Logger = debug,
                }
                .ScheduleParallel();

            state.Dependency = new ApplyChangesJob
                {
                    Disables = disabled,
                    UnsafeEnableableLookup = state.GetUnsafeEnableableLookup(),
                    EnabledComponents = this.enabledComponents,
                }
                .Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithChangeFilter(typeof(Active))]
        [WithAll(typeof(ActivePrevious))]
        [WithDisabled(typeof(Active))]
        private partial struct DeactivateJob : IJobEntity
        {
            public NativeQueue<EntityComponentKey>.ParallelWriter RemoveTags;

            [ReadOnly]
            public NativeHashMap<ulong, ComponentType> TypeIndices;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            public BLLogger Logger;

            private void Execute(Entity entity, in DynamicBuffer<ActionEnableable> actionEnableables, in Targets targets)
            {
                foreach (var tag in actionEnableables.AsNativeArrayRO())
                {
                    if (Hint.Unlikely(!this.TypeIndices.TryGetValue(tag.Value, out var type)))
                    {
                        this.Logger.LogWarning($"Trying to disable with hash {tag.Value} that isn't supported.");
                        continue;
                    }

                    var target = targets.Get(tag.Target, entity, this.TargetsCustoms);
                    this.RemoveTags.Enqueue(new EntityComponentKey(target, type));
                }
            }
        }

        [BurstCompile]
        private struct ApplyChangesJob : IJob
        {
            public NativeQueue<EntityComponentKey> Disables;

            // We add all dependencies to IEnableableComponent
            public UnsafeEnableableLookup UnsafeEnableableLookup;

            public NativeHashMap<EntityComponentKey, byte> EnabledComponents;

            public void Execute()
            {
                while (this.Disables.TryDequeue(out var key))
                {
                    var result = this.EnabledComponents.TryGetValue(key, out var count);
                    Check.Assume(result, "Trying disable that wasn't enabled");
                    Check.Assume(count != 0, "Trying to remove tag that wasn't added");
                    count--;

                    if (count == 0)
                    {
                        this.EnabledComponents.Remove(key);

                        if (this.UnsafeEnableableLookup.HasComponent(key.Entity, key.Component))
                        {
                            this.UnsafeEnableableLookup.SetComponentEnabled(key.Entity, key.Component, false);
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
