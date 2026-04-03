// <copyright file="ActionCreateDeactivatedSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actions
{
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Manages the destruction of entities created by reactions when they become inactive, ensuring proper cleanup and state propagation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveDisabledSystemGroup"/> and processes entities that were previously active
    /// but are now inactive (those with <see cref="ActivePrevious"/> but without <see cref="Active"/>).
    /// It handles the cleanup of entities created by <see cref="ActionCreateSystem"/> during reaction activation.
    /// </para>
    /// <para>
    /// The system performs the following operations for each deactivated entity:
    /// 1. Iterates through all entities in the <see cref="ActionCreated"/> buffer (entities created during activation)
    /// 2. Removes each created entity from the parent's <see cref="LinkedEntityGroup"/> buffer
    /// 3. Enables the <see cref="DestroyEntity"/> component on each created entity to mark it for destruction
    /// 4. Recursively propagates deactivation to child entities that are also active reactions
    /// 5. Clears the <see cref="ActionCreated"/> buffer to complete the cleanup process
    /// </para>
    /// <para>
    /// The system includes sophisticated hierarchical state propagation:
    /// - When a created entity is also an active reaction, it disables its <see cref="Active"/> component
    /// - This triggers cascading deactivation down the entity hierarchy
    /// - Child entities with their own <see cref="ActionCreated"/> buffers are processed recursively
    /// - The recursive cleanup ensures complete hierarchy cleanup in a single frame
    /// </para>
    /// <para>
    /// Key safety features include:
    /// - **Lifecycle Management**: Removes entities from <see cref="LinkedEntityGroup"/> before destruction
    /// - **Assertion Checking**: Validates that created entities are properly tracked in linked entity groups
    /// - **Recursive Propagation**: Ensures complete cleanup of nested reaction hierarchies
    /// - **State Validation**: Checks for proper <see cref="DestroyEntity"/> component presence
    /// </para>
    /// <para>
    /// This system ensures that when reactions deactivate, all entities they created are properly
    /// cleaned up without leaving orphaned entities or inconsistent game state.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveDisabledSystemGroup), OrderFirst = true)]
    public partial struct ActionCreateDeactivatedSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var toDeactivate = new NativeList<Entity>(state.WorldUpdateAllocator);

            state.Dependency = new DeactivatedJob
                {
                    ToDeactivate = toDeactivate,
                    DestroyEntities = SystemAPI.GetComponentLookup<DestroyEntity>(),
                    Actives = SystemAPI.GetComponentLookup<Active>(),
                    LinkedEntityGroups = SystemAPI.GetBufferLookup<LinkedEntityGroup>(),
                    ActionCreateds = SystemAPI.GetBufferLookup<ActionCreated>(),
                }
                .Schedule(state.Dependency);

            state.Dependency = new DeactivateEntitiesJob
            {
                ToDeactivate = toDeactivate,
                Actives = SystemAPI.GetComponentLookup<Active>(),
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(ActivePrevious))]
        [WithDisabled(typeof(Active))]
        private partial struct DeactivatedJob : IJobEntity
        {
            public NativeList<Entity> ToDeactivate;

            public ComponentLookup<DestroyEntity> DestroyEntities;

            [ReadOnly]
            public ComponentLookup<Active> Actives;

            [NativeDisableContainerSafetyRestriction]
            public BufferLookup<LinkedEntityGroup> LinkedEntityGroups;

            [NativeDisableContainerSafetyRestriction]
            public BufferLookup<ActionCreated> ActionCreateds;

            private void Execute(DynamicBuffer<ActionCreated> effectCreated, DynamicBuffer<LinkedEntityGroup> linkedEntityGroup)
            {
                var leg = linkedEntityGroup.Reinterpret<Entity>();
                var created = effectCreated.AsNativeArray().Reinterpret<Entity>();

                foreach (var entity in created)
                {
                    var index = leg.AsNativeArray().IndexOf(entity);
                    Check.Assume(index != -1, "Entity was created but not added to the linked entity group");

                    // Remove it from LEG as it no longer controls it's lifecycle.
                    leg.RemoveAtSwapBack(index);

                    Check.Assume(this.DestroyEntities.HasComponent(entity), "Entity marked as destroy doesn't have DestroyEntity");

                    this.DestroyEntities.SetComponentEnabled(entity, true);

                    // If child is also an active entity, we need to propagate these changes down
                    // If it's not active it would have already been handled by this system, so we ignore it
                    var active = this.Actives.GetEnabledRefROOptional<Active>(entity);
                    if (!active.IsValid || !active.ValueRO)
                    {
                        continue;
                    }

                    this.ToDeactivate.Add(entity);

                    // If it has its own buffer, propagate changes down, so they clear up this frame
                    if (this.ActionCreateds.TryGetBuffer(entity, out var newEffectCreated))
                    {
                        Check.Assume(this.LinkedEntityGroups.HasBuffer(entity), "Somehow entity has EffectCreated but not LinkedEntityGroup");
                        this.Execute(newEffectCreated, this.LinkedEntityGroups[entity]);
                    }
                }

                effectCreated.Clear();
            }
        }

        [BurstCompile]
        private struct DeactivateEntitiesJob : IJob
        {
            [ReadOnly]
            public NativeList<Entity> ToDeactivate;

            public ComponentLookup<Active> Actives;

            public void Execute()
            {
                foreach (var entity in this.ToDeactivate)
                {
                    this.Actives.SetComponentEnabled(entity, false);
                }
            }
        }
    }
}