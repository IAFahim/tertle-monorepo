// <copyright file="BridgeTypeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge
{
    using BovineLabs.Bridge.Data;
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
#endif
    using BovineLabs.Bridge.Data.Lighting;
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Core.Groups;
    using Unity.Burst;
    using Unity.Entities;
#if UNITY_SPLINES
    using BovineLabs.Bridge.Data.Spline;
#endif

    [WorldSystemFilter(BridgeWorlds.All)]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public partial struct BridgeTypeSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI
                .QueryBuilder()
                .WithAny<LightData>()
#if UNITY_URP
                .WithAny<VolumeSettings>()
#endif
#if UNITY_CINEMACHINE
                .WithAny<CMCamera, CMCameraTargetBridgeObject>()
#endif
#if UNITY_SPLINES
                .WithAny<AddSplineBridge>()
#endif
                .WithNoneChunkComponent<BridgeType>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build();

            var chunks = query.ToArchetypeChunkArray(state.WorldUpdateAllocator);
            state.EntityManager.AddChunkComponentData(query, new BridgeType());

            foreach (var chunk in chunks)
            {
                var types = UnityComponentType.None;

                CheckType<LightData>(chunk, ref types, UnityComponentType.Light);
#if UNITY_URP
                CheckType<VolumeSettings>(chunk, ref types, UnityComponentType.Volume);
#endif
#if UNITY_CINEMACHINE
                CheckType<CMCamera>(chunk, ref types, UnityComponentType.Cinemachine);
                var cinemachine = (types & UnityComponentType.Cinemachine) != 0
                    ? GetCinemachineType(chunk)
                    : CMCameraRuntimeType.None;
#endif
#if UNITY_SPLINES
                CheckType<AddSplineBridge>(chunk, ref types, UnityComponentType.Spline);
#endif

                state.EntityManager.SetChunkComponentData(chunk, new BridgeType
                {
                    Types = types,
#if UNITY_CINEMACHINE
                    Cinemachine = cinemachine,
#endif
                });
            }
        }

        private static void CheckType<T>(in ArchetypeChunk chunk, ref UnityComponentType currentType, UnityComponentType typeToAdd)
            where T : unmanaged, IComponentData
        {
            if (chunk.Has<T>())
            {
                currentType |= typeToAdd;
            }
        }

#if UNITY_CINEMACHINE
        private static CMCameraRuntimeType GetCinemachineType(in ArchetypeChunk chunk)
        {
            var type = CMCameraRuntimeType.None;

            CheckCinemachineType<CMCamera>(chunk, ref type, CMCameraRuntimeType.Camera);
            CheckCinemachineType<CMFollow>(chunk, ref type, CMCameraRuntimeType.Follow);
            CheckCinemachineType<CMPositionComposer>(chunk, ref type, CMCameraRuntimeType.PositionComposer);
            CheckCinemachineType<CMRotationComposer>(chunk, ref type, CMCameraRuntimeType.RotationComposer);
            CheckCinemachineType<CMThirdPersonFollow>(chunk, ref type, CMCameraRuntimeType.ThirdPersonFollow);
            CheckCinemachineType<CMOrbitFollow>(chunk, ref type, CMCameraRuntimeType.OrbitFollow);
            CheckCinemachineType<CMFreeLookModifier>(chunk, ref type, CMCameraRuntimeType.FreeLookModifier);
            CheckCinemachineType<CMRotateWithFollowTarget>(chunk, ref type, CMCameraRuntimeType.RotateWithFollowTarget);
            CheckCinemachineType<CMHardLockToTarget>(chunk, ref type, CMCameraRuntimeType.HardLockToTarget);
            CheckCinemachineType<CMHardLookAt>(chunk, ref type, CMCameraRuntimeType.HardLookAt);
            CheckCinemachineType<CMPanTilt>(chunk, ref type, CMCameraRuntimeType.PanTilt);
            CheckCinemachineType<CMBasicMultiChannelPerlin>(chunk, ref type, CMCameraRuntimeType.BasicMultiChannelPerlin);
            CheckCinemachineType<CMGroupFraming>(chunk, ref type, CMCameraRuntimeType.GroupFraming);
            CheckCinemachineType<CMFollowZoom>(chunk, ref type, CMCameraRuntimeType.FollowZoom);
            CheckCinemachineType<CMCameraOffset>(chunk, ref type, CMCameraRuntimeType.CameraOffset);
            CheckCinemachineType<CMRecomposer>(chunk, ref type, CMCameraRuntimeType.Recomposer);
            CheckCinemachineType<CMVolumeSettings>(chunk, ref type, CMCameraRuntimeType.VolumeSettings);
#if UNITY_SPLINES
            CheckCinemachineType<CMSplineDolly>(chunk, ref type, CMCameraRuntimeType.SplineDolly);
            CheckCinemachineType<CMSplineDollyLookAtTargets>(chunk, ref type, CMCameraRuntimeType.SplineDollyLookAtTargets);
#endif

            return type;
        }

        private static void CheckCinemachineType<T>(in ArchetypeChunk chunk, ref CMCameraRuntimeType currentType, CMCameraRuntimeType typeToAdd)
            where T : unmanaged, IComponentData
        {
            if (chunk.Has<T>())
            {
                currentType |= typeToAdd;
            }
        }
#endif
    }
}
