// <copyright file="GetListenerPositionJob.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using BovineLabs.Bridge.Data.Camera;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    [BurstCompile]
    [WithAll(typeof(CameraMain))] // TODO
    public partial struct GetListenerPositionJob : IJobEntity
    {
        public NativeReference<float3> ListenerPosition;

        private void Execute(in LocalTransform transform)
        {
            this.ListenerPosition.Value = transform.Position;
        }
    }
}
#endif
