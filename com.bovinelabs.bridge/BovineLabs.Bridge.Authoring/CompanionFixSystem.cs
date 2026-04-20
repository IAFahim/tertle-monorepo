// <copyright file="CompanionFixSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring
{
    using System.Linq;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;
#if UNITY_HDRP
    using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_URP
    using UnityEngine.Rendering.Universal;
#endif
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Cinemachine;
#endif

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    public partial struct CompanionFixSystem : ISystem
    {
        private ComponentTypeSet componentSet;
        private EntityQuery query;

        public void OnCreate(ref SystemState state)
        {
            var components = new FixedList128Bytes<ComponentType>();

            foreach (var t in CompanionRemove.ToRemove)
            {
                components.Add(t);
            }

            this.query = new EntityQueryBuilder(Allocator.Temp).WithAny(ref components).Build(ref state);
            this.componentSet = new ComponentTypeSet(components);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.RemoveComponent(this.query, this.componentSet);
        }
    }

    [InitializeOnLoad]
    public static class CompanionRemove
    {
        public static readonly ComponentType[] ToRemove =
        {
#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
            typeof(AudioSource),
#endif
            typeof(Light), typeof(UnityEngine.Rendering.Volume),
#if UNITY_HDRP
            typeof(HDAdditionalLightData),
#endif
#if UNITY_URP
            typeof(UniversalAdditionalLightData),
#endif
        };

        static CompanionRemove()
        {
            var types = CompanionComponentSupportedTypes
                .Types
                .Where(type => !ToRemove.Contains(type))
                .Concat(new ComponentType[]
                {
#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
                    typeof(AudioReverbZone),
#endif
// #if UNITY_CINEMACHINE
//                     typeof(CinemachineCamera),
//                     typeof(CinemachineFollow),
//                     typeof(CinemachinePositionComposer),
//                     typeof(CinemachineSplineDolly),
// #if UNITY_PHYSICS
//                     typeof(CinemachineThirdPersonFollowDots),
// #endif
//                     typeof(CinemachineOrbitalFollow),
//                     typeof(CinemachineRotationComposer),
//                     typeof(CinemachineFreeLookModifier),
//                     typeof(CinemachinePanTilt),
//                     typeof(CinemachineRotateWithFollowTarget),
//                     typeof(CinemachineHardLockToTarget),
//                     typeof(CinemachineHardLookAt),
//                     typeof(CinemachineSplineDollyLookAtTargets),
//                     typeof(CinemachineBasicMultiChannelPerlin),
//                     typeof(CinemachineGroupFraming),
//                     typeof(CinemachineFollowZoom),
//                     typeof(CinemachineCameraOffset),
//                     typeof(CinemachineRecomposer),
//                     typeof(CinemachineVolumeSettings),
// #endif
#if UNITY_HDRP
                    typeof(WaterSurface),
#endif
                })
                .ToList();

            CompanionComponentSupportedTypes.Types = types.ToArray();
        }
    }
}
