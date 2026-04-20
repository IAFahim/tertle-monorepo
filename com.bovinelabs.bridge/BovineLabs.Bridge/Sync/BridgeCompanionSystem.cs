// <copyright file="BridgeCompanionSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Bridge.Data;
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
#endif
    using BovineLabs.Core.Groups;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering;
    using Object = UnityEngine.Object;
#if UNITY_URP
    using UnityEngine.Rendering.Universal;
#elif UNITY_HDRP
    using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_SPLINES
    using UnityEngine.Splines;
#endif

    [WorldSystemFilter(BridgeWorlds.All)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup), OrderFirst = true)] // need this to work on ghosts
    public partial class BridgeCompanionSystem : SystemBase
    {
        private readonly Dictionary<BridgeType, BridgeObjectPool> pools = new();
        private EntityQuery getQuery;
        private EntityQuery returnQuery;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.getQuery = SystemAPI.QueryBuilder().WithAllChunkComponent<BridgeType>().WithNone<BridgeObject>().Build();
            this.returnQuery = SystemAPI.QueryBuilder().WithAll<BridgeObject>().WithNoneChunkComponent<BridgeType>().Build();

            this.RequireAnyForUpdate(this.getQuery, this.returnQuery);
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            // Cleanup any hanging GameObjects
            this.DoReturn(SystemAPI.QueryBuilder().WithAll<BridgeObject>().Build());

            foreach (var p in this.pools.Values)
            {
                p.Dispose();
            }

            this.pools.Clear();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            this.Get();
            this.Return();
        }

        private void Get()
        {
            if (this.getQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var ecb = new EntityCommandBuffer(this.WorldUpdateAllocator);

            var entityTypeHandle = SystemAPI.GetEntityTypeHandle();
            var bridgeTypesHandle = SystemAPI.GetComponentTypeHandle<BridgeType>(true);

            foreach (var chunk in this.getQuery.ToArchetypeChunkArray(this.WorldUpdateAllocator))
            {
                var entities = chunk.GetNativeArray(entityTypeHandle);
                var bt = chunk.GetChunkComponentData(ref bridgeTypesHandle);

                if (!this.pools.TryGetValue(bt, out var item))
                {
                    var types = GetTypes(bt);

#if UNITY_EDITOR
                    var name = (BridgeObjectConfig.Flags & HideFlags.HideInHierarchy) == 0
                        ? (types.Length != 0 ? string.Join(',', types.Select(s => s.Name)) : "BridgeObject")
                        : "PooledObject";
#else
                    const string name = "PooledObject";
#endif

                    this.pools[bt] = item = new BridgeObjectPool(() => Create(types, name));
                }

                foreach (var entity in entities)
                {
                    var go = item.Get();

                    ecb.SetComponent(entity, new BridgeObject
                    {
                        Value = go,
                        Transform = go.transformHandle,
                        Type = bt,
                    });
                }
            }

            // Do the actual structural change in 1 pass which Add all the components that the ecb will write to
            this.EntityManager.AddComponent<BridgeObject>(this.getQuery);
            ecb.Playback(this.EntityManager);
        }

        private void Return()
        {
            if (this.returnQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            this.DoReturn(this.returnQuery);

            this.EntityManager.RemoveComponent<BridgeObject>(this.returnQuery);
        }

        private void DoReturn(EntityQuery query)
        {
            var bridgeObjectsHandle = SystemAPI.GetComponentTypeHandle<BridgeObject>();

            foreach (var chunk in query.ToArchetypeChunkArray(this.WorldUpdateAllocator))
            {
                var bridgeObjects = chunk.GetNativeArray(ref bridgeObjectsHandle);

                foreach (var bo in bridgeObjects)
                {
                    this.pools[bo.Type].Release(bo.Value);
                }
            }
        }

        private static Type[] GetTypes(BridgeType bt)
        {
            using var pool = UnityEngine.Pool.ListPool<Type>.Get(out var list);
            var type = bt.Types;

            if ((type & UnityComponentType.Light) != 0)
            {
                list.Add(typeof(Light));
#if UNITY_URP
                list.Add(typeof(UniversalAdditionalLightData));
#elif UNITY_HDRP
                list.Add(typeof(HDAdditionalLightData));
#endif
            }

#if UNITY_URP
            if ((type & UnityComponentType.Volume) != 0)
            {
                list.Add(typeof(Volume));
            }
#endif

#if UNITY_CINEMACHINE
            if ((type & UnityComponentType.Cinemachine) != 0)
            {
                GetCinemachineTypes(bt.Cinemachine, list);
            }
#endif

#if UNITY_SPLINES
            if ((type & UnityComponentType.Spline) != 0)
            {
                list.Add(typeof(SplineContainer));
            }
#endif

            return list.ToArray();
        }

        private static GameObject Create(Type[] types, string name)
        {
            var go = new GameObject(name, types);
#if UNITY_EDITOR
            go.hideFlags = BridgeObjectConfig.Flags;

            if (Application.isPlaying)
#endif
            {
                Object.DontDestroyOnLoad(go);
            }

            return go;
        }

#if UNITY_CINEMACHINE
        private static void GetCinemachineTypes(CMCameraRuntimeType type, List<Type> types)
        {
            if ((type & CMCameraRuntimeType.Camera) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineCamera));
            }

            if ((type & CMCameraRuntimeType.Follow) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineFollow));
            }

            if ((type & CMCameraRuntimeType.PositionComposer) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachinePositionComposer));
            }

            if ((type & CMCameraRuntimeType.RotationComposer) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineRotationComposer));
            }

            if ((type & CMCameraRuntimeType.ThirdPersonFollow) != 0)
            {
                types.Add(typeof(CinemachineThirdPersonFollowDots));
            }

#if UNITY_SPLINES
            if ((type & CMCameraRuntimeType.SplineDolly) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineSplineDolly));
            }

            if ((type & CMCameraRuntimeType.SplineDollyLookAtTargets) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineSplineDollyLookAtTargets));
            }
#endif

            if ((type & CMCameraRuntimeType.OrbitFollow) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineOrbitalFollow));
            }

            if ((type & CMCameraRuntimeType.FreeLookModifier) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineFreeLookModifier));
            }

            if ((type & CMCameraRuntimeType.RotateWithFollowTarget) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineRotateWithFollowTarget));
            }

            if ((type & CMCameraRuntimeType.HardLockToTarget) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineHardLockToTarget));
            }

            if ((type & CMCameraRuntimeType.HardLookAt) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineHardLookAt));
            }

            if ((type & CMCameraRuntimeType.PanTilt) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachinePanTilt));
            }

            if ((type & CMCameraRuntimeType.BasicMultiChannelPerlin) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineBasicMultiChannelPerlin));
            }

            if ((type & CMCameraRuntimeType.GroupFraming) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineGroupFraming));
            }

            if ((type & CMCameraRuntimeType.FollowZoom) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineFollowZoom));
            }

            if ((type & CMCameraRuntimeType.CameraOffset) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineCameraOffset));
            }

            if ((type & CMCameraRuntimeType.Recomposer) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineRecomposer));
            }

            if ((type & CMCameraRuntimeType.VolumeSettings) != 0)
            {
                types.Add(typeof(Unity.Cinemachine.CinemachineVolumeSettings));
            }
        }
#endif
    }
}
