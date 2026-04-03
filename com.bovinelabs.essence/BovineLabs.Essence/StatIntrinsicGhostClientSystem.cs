// <copyright file="StatIntrinsicGhostClientSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Essence
{
    using BovineLabs.Core.Groups;
    using BovineLabs.Essence.Data;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Client-side system that receives and applies stat and intrinsic data from the server.
    /// This system converts ghost data received from the server back into local stat and intrinsic buffers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs only on clients in the BeginSimulationSystemGroup and processes entities
    /// with updated ghost data from the server. It converts StatGhost and IntrinsicGhost data
    /// back into the local Stat and Intrinsic buffers for client-side processing.
    /// </para>
    /// <para>
    /// The reception process:
    /// 1. Detects changes to StatGhost and IntrinsicGhost buffers from server updates
    /// 2. Clears existing local stat and intrinsic buffers for updated entities
    /// 3. Efficiently copies ghost data back to local buffers using batch operations
    /// 4. Runs separate jobs for stat and intrinsic reception in parallel
    /// 5. Local buffers are now synchronized with server state
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct StatIntrinsicGhostClientSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dependency1 = new StatGhostJob().ScheduleParallel(state.Dependency);
            var dependency2 = new IntrinsicGhostJob().ScheduleParallel(state.Dependency);
            state.Dependency = JobHandle.CombineDependencies(dependency1, dependency2);
        }

        [BurstCompile]
        [WithChangeFilter(typeof(StatGhost))]
        public partial struct StatGhostJob : IJobEntity
        {
            private static void Execute(DynamicBuffer<Stat> stats, in DynamicBuffer<StatGhost> statGhosts)
            {
                var statMap = stats.AsMap();
                statMap.Clear();

                var slice = statGhosts.AsNativeArray().Slice();
                var keys = slice.SliceWithStride<StatKey>();
                var values = slice.SliceWithStride<StatValue>(UnsafeUtility.SizeOf<StatKey>());

                statMap.AddBatchUnsafe(keys, values);
            }
        }

        [BurstCompile]
        [WithChangeFilter(typeof(IntrinsicGhost))]
        public partial struct IntrinsicGhostJob : IJobEntity
        {
            private static void Execute(DynamicBuffer<Intrinsic> intrinsics, in DynamicBuffer<IntrinsicGhost> intrinsicGhosts)
            {
                var intrinsicMap = intrinsics.AsMap();
                intrinsicMap.Clear();

                var slice = intrinsicGhosts.AsNativeArray().Slice();
                var keys = slice.SliceWithStride<IntrinsicKey>();
                var values = slice.SliceWithStride<int>(UnsafeUtility.SizeOf<IntrinsicKey>());

                intrinsicMap.AddBatchUnsafe(keys, values);
            }
        }
    }
}
#endif
