// <copyright file="InputBoundsSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Input
{
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Bridge.Input;
    using BovineLabs.Core.Utility;
    using BovineLabs.Nerve.Data.Input;
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.NetCode;

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InputSystemGroup))]
    public partial struct InputBoundsSystem : ISystem
    {
        private EntityQuery inputQuery;

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.inputQuery = SystemAPI.QueryBuilder().WithAllRW<InputBounds>().WithAll<GhostOwnerIsLocal>().Build();
            state.RequireForUpdate(this.inputQuery);
            state.RequireForUpdate<CameraFrustumCorners>();
        }

        /// <inheritdoc />
        [BurstCompile]
        public unsafe void OnUpdate(ref SystemState state)
        {
            this.inputQuery.CompleteDependency();
            var corners = SystemAPI.GetSingleton<CameraFrustumCorners>();
            ref var inputBounds = ref this.inputQuery.GetSingletonRW<InputBounds>().ValueRW;

            mathex.minMax((float3*)UnsafeUtility.AddressOf(ref corners), 8, out var minMax);
            inputBounds.Min = minMax.Min;
            inputBounds.Max = minMax.Max;
        }
    }
}
