// <copyright file="AssemblyInfo.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;
#if UNITY_CINEMACHINE
using BovineLabs.Bridge.Data.Cinemachine;
using Unity.Cinemachine;
#endif
#if UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

[assembly: InternalsVisibleTo("BovineLabs.Bridge")]
[assembly: InternalsVisibleTo("BovineLabs.Bridge.Authoring")]
[assembly: InternalsVisibleTo("BovineLabs.Bridge.Debug")]
[assembly: InternalsVisibleTo("BovineLabs.Bridge.Editor")]
[assembly: InternalsVisibleTo("BovineLabs.Bridge.Tests")]

[assembly: RegisterUnityEngineComponentType(typeof(AudioReverbZone))]

#if UNITY_CINEMACHINE
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineTrackingAuthoring))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineBrain))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineCamera))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineFollow))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachinePositionComposer))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineRotationComposer))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineSplineDolly))]
#if UNITY_PHYSICS
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineThirdPersonFollowDots))]
#endif
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineOrbitalFollow))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineFreeLookModifier))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachinePanTilt))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineRotateWithFollowTarget))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineHardLockToTarget))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineHardLookAt))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineSplineDollyLookAtTargets))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineBasicMultiChannelPerlin))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineGroupFraming))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineFollowZoom))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineCameraOffset))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineRecomposer))]
[assembly: RegisterUnityEngineComponentType(typeof(CinemachineVolumeSettings))]
#endif
#if UNITY_HDRP
[assembly: RegisterUnityEngineComponentType(typeof(WaterSurface))]
#endif