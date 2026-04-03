using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Unity.NetCode
{
    /// <summary>
    /// <para>
    /// Present only in client worlds. Responsible for destroying spawned ghosts when a despawn
    /// request/command is received from the server.
    /// </para>
    /// <para>Clients are not responsible for destroying ghost entities (and thus should never). The server is
    /// responsible for notifying the client about which ghosts should be destroyed (as part of the snapshot protocol).
    /// </para>
    /// <para>
    /// When a despawn command is received, the ghost entity is queued into a despawn queue. Two distinct despawn
    /// queues exist: one for interpolated, and one for the predicted ghosts.
    /// </para>
    /// <para>
    /// The above distinction is necessary because interpolated ghosts timeline (<see cref="NetworkTime.InterpolationTick"/>)
    /// is in the past in respect to both the server and client timeline (the current simulated tick).
    /// When a snapshot with a despawn command (for an interpolated ghost) is received, the server tick at which the entity has been destroyed
    /// (on the server) may be still in the future (for this client), and therefore the client must wait until the <see cref="NetworkTime.InterpolationTick"/>
    /// is greater or equal the despawning tick to actually despawn the ghost.
    /// </para>
    /// <para>
    /// Predicted entities, on the other hand, can be despawned only when the current <see cref="NetworkTime.ServerTick"/> is
    /// greater than or equal to the despawn tick of the server. Therefore, if the client is running ahead (as it should be),
    /// predicted ghosts will be destroyed as soon as their despawn request is pulled out of the snapshot
    /// (i.e. later on that same frame).
    /// </para>
    /// </summary>
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct GhostDespawnSystem : ISystem
    {
        NativeQueue<DelayedDespawnGhost> m_InterpolatedDespawnQueue;
        NativeQueue<DelayedDespawnGhost> m_PredictedDespawnQueue;
        ComponentLookup<GhostGameObjectLink> m_GameObjectLookup;
        BufferLookup<GameObjectDespawnTracking> m_DelayedDespawnLookup;
        Entity m_DelayedGODespawnEntity;

        internal struct DelayedDespawnGhost
        {
            public SpawnedGhost ghost;
            public NetworkTick tick;
        }

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            if (state.WorldUnmanaged.IsHost())
            {
                state.Enabled = false;
                return;
            }

            var singleton = state.EntityManager.CreateEntity(ComponentType.ReadWrite<GhostDespawnQueues>());
            state.EntityManager.SetName(singleton, "GhostLifetimeComponent-Singleton");
            m_InterpolatedDespawnQueue = new NativeQueue<DelayedDespawnGhost>(Allocator.Persistent);
            m_PredictedDespawnQueue = new NativeQueue<DelayedDespawnGhost>(Allocator.Persistent);

            m_DelayedGODespawnEntity = state.EntityManager.CreateEntity(typeof(GameObjectDespawnTracking));
            m_DelayedDespawnLookup = state.GetBufferLookup<GameObjectDespawnTracking>();

            SystemAPI.SetSingleton(new GhostDespawnQueues
            {
                InterpolatedDespawnQueue = m_InterpolatedDespawnQueue,
                PredictedDespawnQueue = m_PredictedDespawnQueue,
            });
            m_GameObjectLookup = state.GetComponentLookup<GhostGameObjectLink>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            state.CompleteDependency();
            m_InterpolatedDespawnQueue.Dispose();
            m_PredictedDespawnQueue.Dispose();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<NetworkStreamInGame>())
            {
                state.CompleteDependency();
                m_PredictedDespawnQueue.Clear();
                m_InterpolatedDespawnQueue.Clear();
                return;
            }

            if (state.WorldUnmanaged.IsThinClient())
                return;

            // TODO-release handle hybrid scenario where entity is destroyed first server side. GO needs to react to this and self destruct (or have a system to handle it for us)
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            m_DelayedDespawnLookup.Update(ref state);

            var allGameObjectDespawns = m_DelayedDespawnLookup[m_DelayedGODespawnEntity];

            allGameObjectDespawns.Clear();
            var spawnedGhostMap = SystemAPI.GetSingletonRW<SpawnedGhostEntityMap>().ValueRO.SpawnedGhostMapRW;
            m_GameObjectLookup.Update(ref state);
            state.Dependency = new DespawnJob
            {
                spawnedGhostMap = spawnedGhostMap,
                interpolatedDespawnQueue = m_InterpolatedDespawnQueue,
                predictedDespawnQueue = m_PredictedDespawnQueue,
                interpolatedTick = networkTime.InterpolationTick,
                predictedTick = networkTime.ServerTick,
                commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                isGo = m_GameObjectLookup,
                // Delay the GameObject destruction to a subsequent managed system since we can't burst GameObject Destroy right now TODO-next@domino after domino: merge that system back here
                despawnTrackingLookup = m_DelayedDespawnLookup,
                despawnSingleton = m_DelayedGODespawnEntity,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        struct DespawnJob : IJob
        {
            public NativeQueue<DelayedDespawnGhost> interpolatedDespawnQueue;
            public NativeParallelHashMap<SpawnedGhost, Entity> spawnedGhostMap;
            public NativeQueue<DelayedDespawnGhost> predictedDespawnQueue;
            public NetworkTick interpolatedTick, predictedTick;
            public EntityCommandBuffer commandBuffer;
            public ComponentLookup<GhostGameObjectLink> isGo;
            [NativeDisableParallelForRestriction] public BufferLookup<GameObjectDespawnTracking> despawnTrackingLookup;
            public Entity despawnSingleton;

            [BurstCompile]
            public void Execute()
            {
                var despawnTracking = despawnTrackingLookup[despawnSingleton];
                while (interpolatedDespawnQueue.Count > 0 &&
                       !interpolatedDespawnQueue.Peek().tick.IsNewerThan(interpolatedTick))
                {
                    var spawnedGhost = interpolatedDespawnQueue.Dequeue();
                    if (spawnedGhostMap.TryGetValue(spawnedGhost.ghost, out var ent))
                    {
                        if (isGo.HasComponent(ent))
                        {
                            despawnTracking.Add(new() { oneDespawn = spawnedGhost });
                        }
                        else
                        {
                            commandBuffer.DestroyEntity(ent);
                            spawnedGhostMap.Remove(spawnedGhost.ghost);
                        }
                    }
                }

                while (predictedDespawnQueue.Count > 0 &&
                       !predictedDespawnQueue.Peek().tick.IsNewerThan(predictedTick))
                {
                    var spawnedGhost = predictedDespawnQueue.Dequeue();
                    if (spawnedGhostMap.TryGetValue(spawnedGhost.ghost, out var ent))
                    {
                        if (isGo.HasComponent(ent))
                        {
                            despawnTracking.Add(new() { oneDespawn = spawnedGhost });
                        }
                        else
                        {
                            commandBuffer.DestroyEntity(ent);
                            spawnedGhostMap.Remove(spawnedGhost.ghost);
                        }
                    }
                }
            }
        }
    }

    internal struct GameObjectDespawnTracking : IBufferElementData
    {
        public GhostDespawnSystem.DelayedDespawnGhost oneDespawn;
    }

    // TODO-next@trunk once we're in trunk, check slack thread see if they were able to get to it: GO despawn doesn't have APIs for burst compatible GO destruction. Raised this on slack. Disabling burst for now, since this is really just a system that schedules a job that's itself bursted anyway. But should come back to this if/when that's available. Slack thread https://unity.slack.com/archives/C0575F6KEAY/p1757546583041179
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateAfter(typeof(GhostDespawnSystem))]
    internal partial class GhostGameObjectDespawnManagedSystem : SystemBase
    {
        EntityQuery m_DelayedDespawnQuery;
        protected override void OnCreate()
        {
            if (World.IsHost())
            {
                this.Enabled = false;
                return;
            }
            RequireForUpdate<GhostGameObjectLink>();
            m_DelayedDespawnQuery = this.GetEntityQuery(typeof(GameObjectDespawnTracking));
            RequireForUpdate<GameObjectDespawnTracking>();
        }

        protected override void OnUpdate()
        {
            var spawnedGhostMap = SystemAPI.GetSingletonRW<SpawnedGhostEntityMap>().ValueRO.SpawnedGhostMapRW;
            var trackingEntity = m_DelayedDespawnQuery.GetSingletonEntity();
            var GODespawnTracking = EntityManager.GetBuffer<GameObjectDespawnTracking>(trackingEntity, isReadOnly: true).ToNativeArray(Allocator.Temp); // Using ToNativeArray since there seems to be a bug with DynamicBuffer's safety handles right now. Slack thread https://unity.slack.com/archives/C0575F6KEAY/p1772128053576089

            for (int i = 0; i < GODespawnTracking.Length; i++)
            {
                var spawnedGhost = GODespawnTracking[i];
                if (spawnedGhostMap.TryGetValue(spawnedGhost.oneDespawn.ghost, out var ent))
                {
                    var goIdToDespawn = EntityManager.GetComponentData<GhostGameObjectLink>(ent)
                        .AssociatedGameObject;

                    GameObject.DestroyImmediate(Resources.EntityIdToObject(goIdToDespawn));
                    spawnedGhostMap.Remove(spawnedGhost.oneDespawn.ghost);

                    // This should be the last release, as all the other OnDestroy should have been called by the DestroyImmediate above.
                    // This in turn removes the GhostGameObjectLink cleanup component
                    GhostEntityMapping.ReleaseGameObjectEntityReference(goIdToDespawn, worldIsCreated: true);
                }
            }
        }
    }
}
