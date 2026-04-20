// <copyright file="SplineSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_SPLINES
namespace BovineLabs.Bridge
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Spline;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine.Pool;
    using UnityEngine.Splines;

    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    public partial struct SplineSyncSystem : ISystem
    {
        static unsafe SplineSyncSystem()
        {
            Burst.Splines.Data = new BurstTrampoline(&SplinesChangedPacked);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (splines, bridge) in SystemAPI.Query<RefRO<Splines>, RefRO<BridgeObject>>().WithAll<AddSplineBridge>().WithChangeFilter<Splines>())
            {
                Burst.Splines.Data.Invoke(bridge.ValueRO, splines.ValueRO);
            }
        }

        private static unsafe void SplinesChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, Splines>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            ref var blobSplines = ref component.Value.Value;
            using var pool = ListPool<Spline>.Get(out var list);

            for (var i = 0; i < blobSplines.Length; ++i)
            {
                list.Add(blobSplines[i].ToSpline());
            }

            bridge.Q<SplineContainer>().Splines = list;
        }

        private static class Burst
        {
            public static readonly SharedStatic<BurstTrampoline> Splines =
                SharedStatic<BurstTrampoline>.GetOrCreate<SplineSyncSystem, Splines>();
        }
    }
}
#endif
