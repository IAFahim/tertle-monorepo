// <copyright file="InitializeTargetsSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Core
{
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Initializes target entity references for newly created reaction entities based on their object definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs first in the <see cref="InitializeSystemGroup"/> on server and local worlds only
    /// (OrderFirst = true) because other initialization systems depend on having correctly initialized
    /// target references. It processes entities with <see cref="InitializeEntity"/> components.
    /// </para>
    /// <para>
    /// The system performs target initialization by:
    /// 1. Building a lookup map from <see cref="InitializeTarget"/> singleton buffer data
    /// 2. Matching entities by their <see cref="ObjectId"/> to find initialization data
    /// 3. Resolving the specified target using the <see cref="Targets"/> system
    /// 4. Updating the entity's <see cref="Targets.Target"/> field with the resolved entity reference
    /// 5. Preserving the previous target if resolution fails (Entity.Null result)
    /// </para>
    /// <para>
    /// This system is essential for reactions that need to reference specific entities as their targets,
    /// such as conditions that monitor other entities or actions that affect specific targets.
    /// The target initialization must occur before other systems that depend on these references,
    /// hence the OrderFirst priority.
    /// </para>
    /// <para>
    /// The system requires the <see cref="InitializeTarget"/> singleton to be present and will not
    /// update when it's missing, allowing for conditional target initialization.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal)]
    [UpdateInGroup(typeof(InitializeSystemGroup), OrderFirst = true)] // Because other systems use target
    public partial struct InitializeTargetsSystem : ISystem
    {
        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InitializeTarget>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var map = SystemAPI.QueryBuilder().WithAll<InitializeTarget>().Build()
                .GetSingletonBufferNoSync<InitializeTarget>(true)
                .AsHashMap<InitializeTarget, ObjectId, InitializeTarget.Data>();

            new InitializeTargetObjectJob
                {
                    InitializeTargets = map,
                    TargetsCustoms = SystemAPI.GetComponentLookup<TargetsCustom>(true),
                }
                .Schedule();
        }

        [BurstCompile]
        [WithAll(typeof(InitializeEntity))]
        private partial struct InitializeTargetObjectJob : IJobEntity
        {
            [ReadOnly]
            public DynamicHashMap<ObjectId, InitializeTarget.Data> InitializeTargets;

            [ReadOnly]
            public ComponentLookup<TargetsCustom> TargetsCustoms;

            private void Execute(Entity entity, in ObjectId objectId, ref Targets targets)
            {
                if (!this.InitializeTargets.TryGetValue(objectId, out var data))
                {
                    return;
                }

                var previousTarget = targets.Target;

                targets.Target = targets.Get(data.Target, entity, this.TargetsCustoms);

                if (targets.Target == Entity.Null)
                {
                    targets.Target = previousTarget;
                }
            }
        }
    }
}
