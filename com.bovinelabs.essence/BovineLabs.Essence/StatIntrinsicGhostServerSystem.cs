// <copyright file="StatIntrinsicGhostServerSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Essence
{
    using BovineLabs.Essence.Data;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Jobs;

    /// <summary>
    /// Server-side system that prepares stat and intrinsic data for network replication to clients.
    /// This system converts stat and intrinsic buffers into ghost format for network transmission.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs only on the server in the LateSimulationSystemGroup and processes entities
    /// with changed stat or intrinsic data. It converts the data into StatGhost and IntrinsicGhost
    /// formats that can be efficiently replicated over the network to connected clients.
    /// </para>
    /// <para>
    /// The replication process:
    /// 1. Detects changes to Stat and Intrinsic buffers using change filters
    /// 2. Clears existing ghost buffers for entities with changes
    /// 3. Copies all current stat and intrinsic values to their respective ghost buffers
    /// 4. Runs separate jobs for stat and intrinsic replication in parallel
    /// 5. Prepared ghost data is sent to clients by Unity Netcode systems
    /// </para>
    /// </remarks>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct StatIntrinsicGhostServerSystem : ISystem
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
        [WithChangeFilter(typeof(Stat))]
        public partial struct StatGhostJob : IJobEntity
        {
            private static void Execute(DynamicBuffer<StatGhost> statGhosts, in DynamicBuffer<Stat> stats)
            {
                statGhosts.Clear();

                var statMap = stats.AsMap();

                foreach (var stat in statMap)
                {
                    // TODO add filtering support
                    statGhosts.Add(new StatGhost
                    {
                        Key = stat.Key,
                        Value = stat.Value,
                    });
                }
            }
        }

        [BurstCompile]
        [WithChangeFilter(typeof(Intrinsic))]
        public partial struct IntrinsicGhostJob : IJobEntity
        {
            private static void Execute(DynamicBuffer<IntrinsicGhost> intrinsicGhosts, in DynamicBuffer<Intrinsic> intrinsics)
            {
                intrinsicGhosts.Clear();

                var intrinsicMap = intrinsics.AsMap();

                foreach (var intrinsic in intrinsicMap)
                {
                    // TODO add filtering support
                    intrinsicGhosts.Add(new IntrinsicGhost
                    {
                        Key = intrinsic.Key,
                        Value = intrinsic.Value,
                    });
                }
            }
        }
    }
}
#endif
