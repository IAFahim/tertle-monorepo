// <copyright file="NetUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve
{
    using System;
    using Unity.Entities;
    using Unity.NetCode;

    public static class NetUtility
    {
        public static Entity CreateRPC(ref SystemState state, Entity connectionEntity, ComponentType componentType)
        {
            Span<ComponentType> comps = stackalloc ComponentType[2];
            comps[0] = componentType;
            comps[1] = ComponentType.ReadOnly<SendRpcCommandRequest>();
            var entity = state.EntityManager.CreateEntity(comps);
            state.EntityManager.SetComponentData(entity, new SendRpcCommandRequest { TargetConnection = connectionEntity });
            return entity;
        }

        public static Entity CreateRPC<T>(ref SystemState state, Entity connectionEntity)
            where T : unmanaged, IRpcCommand
        {
            return CreateRPC(ref state, connectionEntity, ComponentType.ReadWrite<T>());
        }

        public static Entity CreateRPC<T>(ref SystemState state, Entity connectionEntity, T componentData)
            where T : unmanaged, IRpcCommand
        {
            var entity = CreateRPC<T>(ref state, connectionEntity);
            state.EntityManager.SetComponentData(entity, componentData);
            return entity;
        }

        public static Entity CreateApprovalRPC<T>(ref SystemState state, Entity connectionEntity, T componentData)
            where T : unmanaged, IApprovalRpcCommand
        {
            var entity = CreateRPC(ref state, connectionEntity, ComponentType.ReadWrite<T>());
            state.EntityManager.SetComponentData(entity, componentData);
            return entity;
        }
    }
}
