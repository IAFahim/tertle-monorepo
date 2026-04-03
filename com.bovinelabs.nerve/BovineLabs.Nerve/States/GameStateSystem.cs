// <copyright file="GameStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.States
{
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.States;
    using BovineLabs.Nerve.Data.States;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    [UpdateInGroup(typeof(GameStateSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(Worlds.ClientLocal | Worlds.Service)]
    public partial struct GameStateSystem : ISystem, ISystemStartStop
    {
        private StateFlagModel impl;

        public NativeParallelHashMap<byte, ComponentType>.ReadOnly States => this.impl.States;

        /// <inheritdoc />
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.impl = state.WorldUnmanaged.IsServiceWorld()
                ? new StateFlagModel(ref state, ComponentType.ReadWrite<ServiceState>(), ComponentType.ReadWrite<ServiceStatePrevious>())
                : new StateFlagModel(ref state, ComponentType.ReadWrite<ClientState>(), ComponentType.ReadWrite<ClientStatePrevious>());
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            this.impl.Dispose(ref state);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            this.impl.Run(ref state, ecb);
            ecb.Playback(state.EntityManager);
        }
    }
}
