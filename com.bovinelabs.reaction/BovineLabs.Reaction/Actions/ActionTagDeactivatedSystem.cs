// <copyright file="ActionTagDeactivatedSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actions
{
    using BovineLabs.Core;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
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
    /// Manages the removal of tag components from entities when reactions are deactivated, using reference counting to support multiple sources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveDisabledSystemGroup"/> and is created after <see cref="ActionTagSystem"/>
    /// to ensure proper system dependency ordering. It processes entities that were previously active but are now disabled
    /// (those with <see cref="ActivePrevious"/> but without <see cref="Active"/>).
    /// </para>
    /// <para>
    /// The system performs the following operations for each deactivated entity:
    /// 1. Iterates through all <see cref="ActionTag"/> actions in the entity's buffer
    /// 2. Resolves the stable type hash to a <see cref="ComponentType"/> using the shared type indices map
    /// 3. Determines the target entity using the <see cref="Targets"/> system
    /// 4. Decrements the reference count for the tag component on the target entity
    /// 5. Only removes the component when the reference count reaches zero, ensuring tags
    ///    added by multiple sources remain present until all sources are deactivated
    /// </para>
    /// <para>
    /// This system shares the applied tags tracking map with <see cref="ActionTagSystem"/> to maintain
    /// consistent reference counting across add/remove operations. Tag components are only removed when no active
    /// reactions are requesting them to be present.
    /// </para>
    /// </remarks>
    [CreateAfter(typeof(ActionTagSystem))]
    [UpdateInGroup(typeof(ActiveDisabledSystemGroup))]
    public partial struct ActionTagDeactivatedSystem : ISystem
    {
        private NativeParallelHashMap<ulong, ComponentType> typeIndices;
        private NativeHashMap<EntityComponentKey, byte> appliedTags;

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.typeIndices = ActionTagSystem.CreateStableMap();
            this.appliedTags = SystemAPI.GetSingleton<ActionTagSystem.Singleton>().AppliedTags;
            state.AddDependency<ActionTagSystem.Singleton>();
        }

        /// <inheritdoc />
        public void OnDestroy(ref SystemState state)
        {
            this.typeIndices.Dispose();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBufferSystem = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>();
            var removeTags = new NativeQueue<EntityComponentKey>(state.WorldUpdateAllocator);

            new DeactivatedJob
                {
                    RemoveTags = removeTags.AsParallelWriter(),
                    TypeIndices = this.typeIndices,
                    TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                    Logger = SystemAPI.GetSingleton<BLLogger>(),
                }
                .ScheduleParallel();

            state.Dependency = new ApplyJob
                {
                    RemoveTags = removeTags,
                    CommandBuffer = commandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged),
                    AppliedTags = this.appliedTags,
                }
                .Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ActivePrevious))]
        [WithDisabled(typeof(Active))]
        private partial struct DeactivatedJob : IJobEntity
        {
            public NativeQueue<EntityComponentKey>.ParallelWriter RemoveTags;

            [ReadOnly]
            public NativeParallelHashMap<ulong, ComponentType> TypeIndices;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            public BLLogger Logger;

            private void Execute(Entity entity, in DynamicBuffer<ActionTag> actionTags, in Targets targets)
            {
                foreach (var action in actionTags.AsNativeArrayRO())
                {
                    if (Hint.Unlikely(!this.TypeIndices.TryGetValue(action.Value, out var type)))
                    {
                        this.Logger.LogWarning($"Trying to add tag with hash {action.Value} that doesn't exist.");
                        continue;
                    }

                    if (Hint.Unlikely(entity.Equals(Entity.Null)))
                    {
                        this.Logger.LogError($"Target doesn't exist to remove tag with hash {action.Value}. This will leak memory.");
                        continue;
                    }

                    var target = targets.Get(action.Target, entity, this.TargetsCustoms);
                    this.RemoveTags.Enqueue(new EntityComponentKey(target, type));
                }
            }
        }

        [BurstCompile]
        private struct ApplyJob : IJob
        {
            public NativeQueue<EntityComponentKey> RemoveTags;

            public EntityCommandBuffer CommandBuffer;

            public NativeHashMap<EntityComponentKey, byte> AppliedTags;

            public void Execute()
            {
                while (this.RemoveTags.TryDequeue(out var key))
                {
                    var result = this.AppliedTags.TryGetValue(key, out var count);
                    Check.Assume(result, "Trying to remove tag that wasn't added");
                    Check.Assume(count != 0, "Trying to remove tag that wasn't added");
                    count--;

                    if (count == 0)
                    {
                        this.CommandBuffer.RemoveComponent(key.Entity, key.Component);
                        this.AppliedTags.Remove(key);
                    }
                    else
                    {
                        this.AppliedTags[key] = count;
                    }
                }
            }
        }
    }
}
