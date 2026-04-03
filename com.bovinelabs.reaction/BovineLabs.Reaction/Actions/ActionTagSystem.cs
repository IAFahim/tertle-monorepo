// <copyright file="ActionTagSystem.cs" company="BovineLabs">
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
    /// Manages the addition of tag components to entities when reactions become active, with reference counting support for multiple sources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveEnabledSystemGroup"/> and processes entities that have just become active
    /// (those with the <see cref="Active"/> component but without <see cref="ActivePrevious"/>).
    /// </para>
    /// <para>
    /// The system performs the following operations for each activated entity:
    /// 1. Iterates through all <see cref="ActionTag"/> actions in the entity's buffer
    /// 2. Resolves the stable type hash to a <see cref="ComponentType"/> using a pre-built map of all valid tag types
    /// 3. Determines the target entity using the <see cref="Targets"/> system
    /// 4. Increments the reference count for the tag component on the target entity
    /// 5. Adds the component to the target entity only if this is the first source requesting it
    /// </para>
    /// <para>
    /// The system maintains a reference counting system to support multiple reactions adding the same tag to the same entity.
    /// Tags are only removed when all sources that added them have been deactivated, handled by <see cref="ActionTagDeactivatedSystem"/>.
    /// All valid tag types are determined using <see cref="ReactionValidationUtil.ValidateTypeInfoForTag"/> during system creation.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveEnabledSystemGroup))]
    public partial struct ActionTagSystem : ISystem
    {
        private NativeParallelHashMap<ulong, ComponentType> typeIndices;
        private NativeHashMap<EntityComponentKey, byte> appliedTags;

        internal static NativeParallelHashMap<ulong, ComponentType> CreateStableMap()
        {
            var map = new NativeParallelHashMap<ulong, ComponentType>(64, Allocator.Persistent);
            foreach (var t in TypeManager.AllTypes)
            {
                if (ReactionValidationUtil.ValidateTypeInfoForTag(t))
                {
                    map.Add(t.StableTypeHash, ComponentType.FromTypeIndex(t.TypeIndex));
                }
            }

            return map;
        }

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.typeIndices = CreateStableMap();
            this.appliedTags = new NativeHashMap<EntityComponentKey, byte>(0, Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new Singleton
            {
                AppliedTags = this.appliedTags,
            });

            state.AddDependency<Singleton>();
        }

        /// <inheritdoc />
        public void OnDestroy(ref SystemState state)
        {
            this.typeIndices.Dispose();
            this.appliedTags.Dispose();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBufferSystem = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>();
            var addTags = new NativeQueue<EntityComponentKey>(state.WorldUpdateAllocator);

            new ActivatedJob
                {
                    AddTags = addTags.AsParallelWriter(),
                    TypeIndices = this.typeIndices,
                    TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                    Logger = SystemAPI.GetSingleton<BLLogger>(),
                }
                .ScheduleParallel();

            state.Dependency = new ApplyJob
                {
                    AddTags = addTags,
                    CommandBuffer = commandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged),
                    AppliedTags = this.appliedTags,
                }
                .Schedule(state.Dependency);
        }

        internal struct Singleton : IComponentData
        {
            public NativeHashMap<EntityComponentKey, byte> AppliedTags;
        }

        [BurstCompile]
        [WithAll(typeof(Active))]
        [WithDisabled(typeof(ActivePrevious))]
        private partial struct ActivatedJob : IJobEntity
        {
            public NativeQueue<EntityComponentKey>.ParallelWriter AddTags;

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
        private struct ApplyJob : IJob
        {
            public NativeQueue<EntityComponentKey> AddTags;
            public EntityCommandBuffer CommandBuffer;
            public NativeHashMap<EntityComponentKey, byte> AppliedTags;

            public void Execute()
            {
                while (this.AddTags.TryDequeue(out var key))
                {
                    var result = this.AppliedTags.TryGetValue(key, out var count);
                    Check.Assume(count != 255, "Trying to add the same tag from more than 255 sources");
                    Check.Assume(!result || count != 0, "Tag wasn't cleaned up from hash map");
                    count++;

                    if (count == 1)
                    {
                        this.CommandBuffer.AddComponent(key.Entity, key.Component);
                        this.AppliedTags.Add(key, count);
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
