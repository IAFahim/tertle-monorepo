// <copyright file="VolumeSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine.Rendering;

    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    public partial struct VolumeSyncSystem : ISystem
    {
        static unsafe VolumeSyncSystem()
        {
            Burst.VolumeSettings.Data = new BurstTrampoline(&VolumeSettingsChangedPacked);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (settings, bridge) in SystemAPI.Query<RefRO<VolumeSettings>, RefRO<BridgeObject>>().WithChangeFilter<VolumeSettings>())
            {
                Burst.VolumeSettings.Data.Invoke(bridge.ValueRO, settings.ValueRO);
            }
        }

        private static unsafe void VolumeSettingsChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeSettings>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();

            volume.weight = component.Weight;
            volume.priority = component.Priority;
            volume.blendDistance = component.BlendDistance;
            volume.isGlobal = component.IsGlobal;

            volume.sharedProfile = component.Profile.Value;
        }

        private static class Burst
        {
            public static readonly SharedStatic<BurstTrampoline> VolumeSettings =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeSyncSystem, VolumeSettings>();
        }
    }
}
#endif