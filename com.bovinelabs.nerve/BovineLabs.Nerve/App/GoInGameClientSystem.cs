// <copyright file="GoInGameClientSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.App
{
    using BovineLabs.Core;
    using BovineLabs.Nerve.Data.Rpc;
    using BovineLabs.Nerve.Rpc;
    using Unity.Entities;
    using Unity.NetCode;

    [WorldSystemFilter(Worlds.ClientLocal)]
    [UpdateInGroup(typeof(RpcSendSystemGroup))]
    public partial class ClientGoInGameSystem : SystemBase
    {
        private EntityQuery query;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.query = BovineLabsBootstrap.RequireConnectionApproval.Data
                ? SystemAPI.QueryBuilder().WithAll<NetworkId, ConnectionApproved>().WithNone<NetworkStreamInGame>().Build()
                : SystemAPI.QueryBuilder().WithAll<NetworkId>().WithNone<NetworkStreamInGame>().Build();

            this.RequireForUpdate(this.query);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var entity = this.query.GetSingletonEntity();
            if (!this.World.IsHost())
            {
                // When it's host we let the normal GoInGameServerSystem handle it
                this.EntityManager.AddComponent<NetworkStreamInGame>(entity);
            }

            NetUtility.CreateRPC<GoInGameRequest>(ref this.CheckedStateRef, entity);

            // This system only needs to run once so lets stop it updating again
            this.World.GetExistingSystemManaged<RpcSendSystemGroup>().RemoveSystemFromUpdateList(this);
        }
    }
}
