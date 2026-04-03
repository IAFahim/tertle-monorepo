// <copyright file="GoInGameServerSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.App
{
    using BovineLabs.Core;
    using BovineLabs.Nerve.Data.Rpc;
    using BovineLabs.Nerve.Rpc;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.NetCode;
    using UnityEngine;

    // When server receives go in game request, go in game and delete request
    [BurstCompile]
    [WorldSystemFilter(Worlds.ServerLocal)]
    [UpdateInGroup(typeof(RpcReceivedSystemGroup))]
    public partial struct GoInGameServerSystem : ISystem
    {
        private EntityQuery query;

        /// <inheritdoc />
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.query = SystemAPI.QueryBuilder().WithAll<GoInGameRequest, ReceiveRpcCommandRequest>().Build();
            state.RequireForUpdate(this.query);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var debug = SystemAPI.GetSingleton<BLLogger>();

            var networkIdLookup = SystemAPI.GetComponentLookup<NetworkId>(true);
            var reconnectedLookup = SystemAPI.GetComponentLookup<NetworkStreamIsReconnected>(true);

            foreach (var reqSrc in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequest>())
            {
                var networkId = networkIdLookup[reqSrc.ValueRO.SourceConnection];

                // If this request is coming from a reconnecting connection we don't need to spawn and configure a player entity
                // as it has been migrated to the new host and will be reconnected to this client
                if (reconnectedLookup.HasComponent(reqSrc.ValueRO.SourceConnection))
                {
                    debug.LogDebug($"Connection '{networkId.Value}' has reconnected!");
                    continue;
                }

                ecb.AddComponent<NetworkStreamInGame>(reqSrc.ValueRO.SourceConnection);
                debug.LogDebug($"Setting connection '{reqSrc.ValueRO.SourceConnection.ToFixedString()}' to in game");
            }

            ecb.Playback(state.EntityManager);
            state.EntityManager.DestroyEntity(this.query);
        }
    }
}
