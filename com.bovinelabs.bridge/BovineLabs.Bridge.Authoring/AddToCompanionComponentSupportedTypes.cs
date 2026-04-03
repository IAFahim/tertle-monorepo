// <copyright file="AddToCompanionComponentSupportedTypes.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Authoring
{
    using System.Linq;
    using BovineLabs.Core.Internal;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Cinemachine;
#endif
#if UNITY_HDRP
    using UnityEngine.Rendering.HighDefinition;
#endif

    [InitializeOnLoad]
    public static class AddToCompanionComponentSupportedTypes
    {
        static AddToCompanionComponentSupportedTypes()
        {
            var types = CompanionComponentSupportedTypes
                .Types
                .Where(type => type != typeof(AudioSource)) // Remove audio source as a companion object
                .Concat(new ComponentType[]
                {
                    typeof(AudioReverbZone),
#if UNITY_CINEMACHINE
                    typeof(CinemachineBrain),
                    typeof(CinemachineCamera),
                    typeof(CinemachineFollow),
                    typeof(CinemachinePositionComposer),
                    typeof(CinemachineSplineDolly),
#if UNITY_PHYSICS
                    typeof(CinemachineThirdPersonFollowDots),
#endif
                    typeof(CinemachineOrbitalFollow),
                    typeof(CinemachineRotationComposer),
                    typeof(CinemachineFreeLookModifier),
                    typeof(CinemachinePanTilt),
                    typeof(CinemachineRotateWithFollowTarget),
                    typeof(CinemachineHardLockToTarget),
                    typeof(CinemachineHardLookAt),
                    typeof(CinemachineSplineDollyLookAtTargets),
                    typeof(CinemachineBasicMultiChannelPerlin),
                    typeof(CinemachineGroupFraming),
                    typeof(CinemachineFollowZoom),
                    typeof(CinemachineCameraOffset),
                    typeof(CinemachineRecomposer),
                    typeof(CinemachineVolumeSettings),
                    typeof(CinemachineTrackingAuthoring),
#endif
#if UNITY_HDRP
                    typeof(WaterSurface),
#endif
                })
                .ToList();

            CompanionComponentSupportedTypes.Types = types.ToArray();
        }
    }
}
