// <copyright file="PauseRequestServerSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Pause
{
    using BovineLabs.Core;
    using BovineLabs.Core.Pause;
    using BovineLabs.Nerve.Data.Pause;
    using BovineLabs.Nerve.Rpc;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.NetCode;

    [WorldSystemFilter(Worlds.ServerLocal)]
    [UpdateInGroup(typeof(RpcReceivedSystemGroup))]
    public partial struct PauseRequestServerSystem : ISystem, IUpdateWhilePaused
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.HandleRequests(ref state);
            this.HandleTogglePause(ref state);
        }

        private void HandleRequests(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<PauseRequest, ReceiveRpcCommandRequest>().Build();

            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }

            // We queue operations to avoid multiple RPCs in same frame
            var operations = new NativeQueue<(Entity Entity, bool Pause)>(state.WorldUpdateAllocator);

            foreach (var (request, pause) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<PauseRequest>>())
            {
                operations.Enqueue(new(request.ValueRO.SourceConnection, pause.ValueRO.Value));
            }

            while (operations.TryDequeue(out var op))
            {
                var isPaused = state.EntityManager.HasComponent<ConnectionPause>(op.Entity);
                if (isPaused)
                {
                    if (!op.Pause)
                    {
                        state.EntityManager.RemoveComponent<ConnectionPause>(op.Entity);
                    }
                }
                else
                {
                    if (op.Pause)
                    {
                        state.EntityManager.AddComponent<ConnectionPause>(op.Entity);
                    }
                }
            }

            state.EntityManager.DestroyEntity(query);
        }

        private void HandleTogglePause(ref SystemState state)
        {
            var isPaused = SystemAPI.HasComponent<PauseGame>(state.SystemHandle);
            var pauseRequestedQuery = SystemAPI.QueryBuilder().WithAll<ConnectionPause>().Build();
            var pauseRequests = pauseRequestedQuery.CalculateEntityCount();

            var connections = SystemAPI.QueryBuilder().WithAll<NetworkId>().Build().CalculateEntityCount();

            if (isPaused)
            {
                if (pauseRequests == 0 || pauseRequests != connections)
                {
                    state.EntityManager.RemoveComponent<PauseGame>(state.SystemHandle);
                }
            }
            else
            {
                // All connections pause
                if (pauseRequests > 0 && pauseRequests == connections)
                {
                    state.EntityManager.AddComponent<PauseGame>(state.SystemHandle);
                }
            }
        }
    }
}
