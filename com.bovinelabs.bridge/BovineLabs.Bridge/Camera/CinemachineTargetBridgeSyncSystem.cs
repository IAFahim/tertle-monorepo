// <copyright file="CinemachineTargetBridgeSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Camera
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Transforms;

    [UpdateInGroup(typeof(BridgeTransformSyncSystemGroup))]
    [UpdateBefore(typeof(BridgeTransformSyncSystem))]
    public partial struct CinemachineTargetBridgeSyncSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SyncTargetsJob { LocalToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>() }.Schedule(state.Dependency);
            state.Dependency = new SyncVolumeSettingsTargetsJob { LocalToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>() }.Schedule(state.Dependency);

#if UNITY_SPLINES
            state.Dependency = new SyncSplineDollyLookAtTargetsJob { LocalToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>() }.Schedule(state.Dependency);
#endif
        }

        [BurstCompile]
        private partial struct SyncTargetsJob : IJobEntity
        {
            public ComponentLookup<LocalToWorld> LocalToWorlds;

            private void Execute(in CMCamera camera, in CMCameraTargetBridgeObjects targetBridges)
            {
                if (this.LocalToWorlds.TryGetComponent(camera.TrackingTarget, out var trackingTarget))
                {
                    this.LocalToWorlds[targetBridges.TrackingTargetBridge] = trackingTarget;
                }

                if (this.LocalToWorlds.TryGetComponent(camera.LookAtTarget, out var lookAtTarget))
                {
                    this.LocalToWorlds[targetBridges.LookAtTargetBridge] = lookAtTarget;
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(CMCameraEnabled))]
        private partial struct SyncVolumeSettingsTargetsJob : IJobEntity
        {
            public ComponentLookup<LocalToWorld> LocalToWorlds;

            private void Execute(in CMVolumeSettings settings, in CMVolumeSettingsFocusBridge focusBridge)
            {
                if (this.LocalToWorlds.TryGetComponent(settings.FocusTarget, out var focusTarget))
                {
                    this.LocalToWorlds[focusBridge.Value] = focusTarget;
                }
            }
        }

#if UNITY_SPLINES
        [BurstCompile]
        [WithAll(typeof(CMCameraEnabled))]
        private partial struct SyncSplineDollyLookAtTargetsJob : IJobEntity
        {
            public ComponentLookup<LocalToWorld> LocalToWorlds;

            private void Execute(
                in DynamicBuffer<CMSplineDollyLookAtTarget> targets, in DynamicBuffer<CMSplineDollyLookAtTargetBridge> targetBridges)
            {
                var targetArray = targets.AsNativeArray();
                var targetBridgeArray = targetBridges.AsNativeArray();

                for (var index = 0; index < targetArray.Length; index++)
                {
                    if (this.LocalToWorlds.TryGetComponent(targetArray[index].LookAt, out var lookAtTarget))
                    {
                        this.LocalToWorlds[targetBridgeArray[index].Value] = lookAtTarget;
                    }
                }
            }
        }
#endif
    }
}
#endif
