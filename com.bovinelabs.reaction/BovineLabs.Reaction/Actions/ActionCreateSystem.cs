// <copyright file="ActionCreateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actions
{
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Groups;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Instantiates new entity prefabs when reactions become active, managing the lifecycle of created entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="ActiveEnabledSystemGroup"/> and processes entities that have just become active
    /// (those with the <see cref="Active"/> component but without <see cref="ActivePrevious"/>).
    /// </para>
    /// <para>
    /// The system performs the following operations for each activated entity:
    /// 1. Iterates through all <see cref="ActionCreate"/> actions in the entity's buffer
    /// 2. Instantiates the specified prefab using the <see cref="ObjectDefinitionRegistry"/>
    /// 3. Resolves the target entity using the <see cref="Targets"/> system
    /// 4. Copies target information to the newly created entity
    /// 5. If <see cref="ActionCreate.DestroyOnDisabled"/> is true, adds the created entity to the parent's
    ///    <see cref="LinkedEntityGroup"/> and <see cref="ActionCreated"/> buffer for lifecycle management
    /// </para>
    /// <para>
    /// Created entities are automatically destroyed by <see cref="ActionCreateDeactivatedSystem"/> when the parent
    /// reaction is deactivated, provided they were marked with <see cref="ActionCreate.DestroyOnDisabled"/>.
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(ActiveEnabledSystemGroup))]
    public partial struct ActionCreateSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBufferSystem = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>();
            var commandBuffer = commandBufferSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ActivatedJob
                {
                    CommandBuffer = commandBuffer,
                    ObjectDefinitions = SystemAPI.GetSingleton<ObjectDefinitionRegistry>(),
                    TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                }
                .ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(Active))]
        [WithDisabled(typeof(ActivePrevious))]
        private partial struct ActivatedJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            public ObjectDefinitionRegistry ObjectDefinitions;

            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in DynamicBuffer<ActionCreate> actionCreates, in Targets targets)
            {
                foreach (var create in actionCreates.AsNativeArrayRO())
                {
                    var prefab = this.ObjectDefinitions[create.Id];
                    var createdEntity = this.CommandBuffer.Instantiate(chunkIndex, prefab);

                    var target = targets.Get(create.Target, entity, this.TargetsCustoms);
                    var instTargets = targets.Copy(entity, target);

                    this.CommandBuffer.SetComponent(chunkIndex, createdEntity, instTargets);

                    if (create.DestroyOnDisabled)
                    {
                        this.CommandBuffer.AppendToBuffer(chunkIndex, entity, new LinkedEntityGroup { Value = createdEntity });
                        this.CommandBuffer.AppendToBuffer(chunkIndex, entity, new ActionCreated { Value = createdEntity });
                    }
                }
            }
        }
    }
}
