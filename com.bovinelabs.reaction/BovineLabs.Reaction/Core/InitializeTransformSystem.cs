// <copyright file="InitializeTransformSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Core
{
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.Utility;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary>
    /// Initializes <see cref="LocalTransform"/> (and <see cref="LocalToWorld"/> for linked hierarchies)
    /// for newly created reaction entities based on target entity transforms and configuration data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="InitializeSystemGroup"/> on server and local worlds only,
    /// processing entities with <see cref="InitializeEntity"/> components. It configures entity transforms
    /// based on relationships with target entities.
    /// </para>
    /// <para>
    /// The system performs sophisticated transform initialization by:
    /// 1. Building a lookup map from <see cref="InitializeTransform"/> singleton buffer data
    /// 2. Matching entities by their <see cref="ObjectId"/> to find initialization configuration
    /// 3. Resolving "From" and "To" target entities using the <see cref="Targets"/> system
    /// 4. Calculating position, rotation, and scale based on configuration and target transforms
    /// 5. Optionally applying the calculated transform to the entity's existing transform
    /// </para>
    /// <para>
    /// The system supports flexible transform initialization modes:
    /// - **Position**: Can be set from either target, or remain at zero
    /// - **Rotation**: Can copy from targets, calculate direction between targets, or remain identity
    /// - **Scale**: Can copy from targets, use distance between targets, or remain uniform
    /// - **Transform Combination**: Can apply calculated transform on top of existing transform
    /// </para>
    /// <para>
    /// Special handling for different entity hierarchies:
    /// - Entities with <see cref="Parent"/> components use <see cref="LocalToWorld"/> matrices
    /// - Root entities use <see cref="LocalTransform"/> directly
    /// - Missing transforms default to identity and log warnings
    /// </para>
    /// <para>
    /// This system is commonly used for projectiles, effects, and other entities that need to be
    /// positioned and oriented relative to other entities at spawn time.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal)]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct InitializeTransformSystem : ISystem
    {
        private EntityQuery initializeQuery;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InitializeTransform>();

            this.initializeQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<InitializeEntity>()
                .WithAllRW<LocalTransform>()
                .WithAll<ObjectId, Targets>()
                .WithOptions(EntityQueryOptions.FilterWriteGroup)
                .Build(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var map = SystemAPI.QueryBuilder().WithAll<InitializeTransform>().Build()
                .GetSingletonBufferNoSync<InitializeTransform>(true)
                .AsHashMap<InitializeTransform, ObjectId, InitializeTransform.Data>();

            state.Dependency = new InitializeTransformObjectJob
                {
                    EntityHandle = SystemAPI.GetEntityTypeHandle(),
                    LocalTransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(),
                    LocalToWorldHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(),
                    ObjectIdHandle = SystemAPI.GetComponentTypeHandle<ObjectId>(true),
                    TargetsHandle = SystemAPI.GetComponentTypeHandle<Targets>(true),
                    LinkedEntityGroupHandle = SystemAPI.GetBufferTypeHandle<LinkedEntityGroup>(true),
                    PostTransformMatrixHandle = SystemAPI.GetComponentTypeHandle<PostTransformMatrix>(true),

                    LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(true),
                    Parents = SystemAPI.GetComponentLookup<Parent>(true),
                    PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
                    LocalToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>(),
                    InitializeTransforms = map,
                    Logger = SystemAPI.GetSingleton<BLLogger>(),
                }
                .Schedule(this.initializeQuery, state.Dependency);
        }

        [BurstCompile]
        private unsafe struct InitializeTransformObjectJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle EntityHandle;

            [NativeDisableContainerSafetyRestriction]
            public ComponentTypeHandle<LocalTransform> LocalTransformHandle;

            [NativeDisableContainerSafetyRestriction]
            public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

            [ReadOnly]
            public ComponentTypeHandle<ObjectId> ObjectIdHandle;

            [ReadOnly]
            public ComponentTypeHandle<Targets> TargetsHandle;

            [ReadOnly]
            public BufferTypeHandle<LinkedEntityGroup> LinkedEntityGroupHandle;

            [ReadOnly]
            public ComponentTypeHandle<PostTransformMatrix> PostTransformMatrixHandle;

            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransforms;

            [ReadOnly]
            public ComponentLookup<Parent> Parents;

            [ReadOnly]
            public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;

            public ComponentLookup<LocalToWorld> LocalToWorlds;

            [ReadOnly]
            public DynamicHashMap<ObjectId, InitializeTransform.Data> InitializeTransforms;

            public BLLogger Logger;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetEntityDataPtrRO(this.EntityHandle);
                var localTransforms = (LocalTransform*)chunk.GetRequiredComponentDataPtrRW(ref this.LocalTransformHandle);
                var objectIds = (ObjectId*)chunk.GetRequiredComponentDataPtrRO(ref this.ObjectIdHandle);
                var targets = (Targets*)chunk.GetRequiredComponentDataPtrRO(ref this.TargetsHandle);

                var linkedEntityGroupAccessor = chunk.GetBufferAccessorRO(ref this.LinkedEntityGroupHandle);
                var postTransformMatrices = chunk.GetComponentDataPtrRO(ref this.PostTransformMatrixHandle);
                var localToWorlds = chunk.GetComponentDataPtrRW(ref this.LocalToWorldHandle);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var i))
                {
                    var objectId = objectIds[i];
                    if (!this.InitializeTransforms.TryGetValue(objectId, out var data))
                    {
                        continue;
                    }

                    var entity = entities[i];
                    this.TryGetTargetTransform(entity, data.From, targets[i], out var from);
                    this.TryGetTargetTransform(entity, data.To, targets[i], out var to);

                    LocalTransform transform = default;

                    transform.Position = data.Position switch
                    {
                        InitializeTransform.Data.PositionInit.From => from.Position,
                        InitializeTransform.Data.PositionInit.To => to.Position,
                        InitializeTransform.Data.PositionInit.None => float3.zero,
                        _ => float3.zero,
                    };

                    transform.Rotation = data.Rotation switch
                    {
                        InitializeTransform.Data.RotationInit.From => from.Rotation,
                        InitializeTransform.Data.RotationInit.To => to.Rotation,
                        InitializeTransform.Data.RotationInit.Direction => quaternion.LookRotationSafe(math.normalizesafe(to.Position - from.Position),
                            from.Up()),
                        InitializeTransform.Data.RotationInit.DirectionInverse => quaternion.LookRotationSafe(math.normalizesafe(from.Position - to.Position),
                            to.Up()),
                        InitializeTransform.Data.RotationInit.None => quaternion.identity,
                        _ => quaternion.identity,
                    };

                    transform.Scale = data.Scale switch
                    {
                        InitializeTransform.Data.ScaleInit.From => from.Scale,
                        InitializeTransform.Data.ScaleInit.To => to.Scale,
                        InitializeTransform.Data.ScaleInit.Distance => math.distance(from.Position, to.Position),
                        InitializeTransform.Data.ScaleInit.None => 1,
                        _ => 1,
                    };

                    if (data.ApplyInitialTransform)
                    {
                        var ltw = math.mul(transform.ToMatrix(), localTransforms[i].ToMatrix());
                        transform = LocalTransform.FromMatrix(ltw);
                    }

                    localTransforms[i] = transform;

                    if (linkedEntityGroupAccessor.Length > 0)
                    {
                        TransformUtility.SetupLocalToWorld(
                            linkedEntityGroupAccessor[i],
                            ref this.LocalTransforms,
                            ref this.Parents,
                            ref this.PostTransformMatrixLookup,
                            ref this.LocalToWorlds);
                    }
                    else if (localToWorlds != null)
                    {
                        var worldMatrix = transform.ToMatrix();
                        if (postTransformMatrices != null)
                        {
                            worldMatrix = math.mul(worldMatrix, postTransformMatrices[i].Value);
                        }

                        localToWorlds[i] = new LocalToWorld { Value = worldMatrix };
                    }
                }
            }

            private void TryGetTargetTransform(Entity self, Target target, Targets targets, out LocalTransform transform)
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
                    transform = LocalTransform.Identity;
                    return;
                }

                if (Hint.Unlikely(this.Parents.HasComponent(entity)))
                {
                    if (Hint.Likely(this.LocalToWorlds.TryGetComponent(entity, out var localToWorld)))
                    {
                        transform = LocalTransform.FromMatrix(localToWorld.Value);
                        return;
                    }
                }
                else
                {
                    if (Hint.Likely(this.LocalTransforms.TryGetComponent(entity, out transform)))
                    {
                        return;
                    }
                }

                transform = LocalTransform.Identity;
                this.Logger.LogWarning($"Target {entity.ToFixedString()} from {(byte)target} on {self.ToFixedString()} does not have a LocalTransform ");
            }
        }
    }
}
