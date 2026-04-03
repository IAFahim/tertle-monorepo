// <copyright file="CinemachineBrainRuntimeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Core;
    using BovineLabs.Core.Camera;
    using Unity.Cinemachine;
    using Unity.Entities;
    using UnityEngine;

    // Common not to make brain an entity so this system syncs it up
    [UpdateInGroup(typeof(CameraSystemGroup))]
    [UpdateAfter(typeof(CameraMainSystem))]
    public partial class CinemachineBrainRuntimeSystem : SystemBase
    {
        private EntityQuery query;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.query = SystemAPI.QueryBuilder().WithAll<CameraMain, Camera>().WithNone<CMBrain, CinemachineBrain>().Build();
            this.RequireForUpdate(this.query);
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var cameraEntity = this.query.GetSingletonEntity();
            var camera = this.EntityManager.GetComponentObject<Camera>(cameraEntity);

            var brain = camera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                SystemAPI.GetSingleton<BLLogger>().LogErrorString("No Brain found on main camera");
                this.Enabled = false;
                return;
            }

            this.EntityManager.AddComponentObject(cameraEntity, brain);
            this.EntityManager.AddComponentData(cameraEntity, new CMBrain(brain));
        }
    }
}
#endif