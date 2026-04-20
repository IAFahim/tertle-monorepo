// <copyright file="CameraMatrixShiftSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Camera
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    public partial struct CameraMatrixShiftSyncSystem : ISystem
    {
        static unsafe CameraMatrixShiftSyncSystem()
        {
            Burst.CameraViewSpaceOffset.Data = new BurstTrampoline(&CameraViewSpaceOffsetChangedPacked);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (cameraBridge, cameraViewSpaceOffset) in SystemAPI
                .Query<RefRO<CameraBridge>, RefRO<CameraViewSpaceOffset>>()
                .WithChangeFilter<CameraBridge, CameraViewSpaceOffset>())
            {
                Burst.CameraViewSpaceOffset.Data.Invoke(cameraBridge.ValueRO, cameraViewSpaceOffset.ValueRO);
            }
        }

        private static unsafe void CameraViewSpaceOffsetChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref readonly var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<CameraBridge, CameraViewSpaceOffset>>(argumentsPtr, argumentsSize);
            ref readonly var cameraBridge = ref arguments.First;
            ref readonly var offset = ref arguments.Second;
            var camera = cameraBridge.Value.Value;
            if (camera == null)
            {
                return;
            }

            ref readonly var projectionOffset = ref offset.ProjectionCenterOffset;

            if (math.any(projectionOffset != float2.zero))
            {
                camera.projectionMatrix = CalculateOffCenterProjection(camera, projectionOffset);
            }
            else
            {
                camera.ResetProjectionMatrix();
            }
        }

        private static Matrix4x4 CalculateOffCenterProjection(Camera camera, float2 centerOffset)
        {
            var near = camera.nearClipPlane;
            var far = camera.farClipPlane;
            var aspect = camera.aspect <= 0f ? 1f : camera.aspect;

            if (camera.orthographic)
            {
                var halfHeight = camera.orthographicSize;
                var halfWidth = halfHeight * aspect;
                var x = centerOffset.x * halfWidth;
                var y = centerOffset.y * halfHeight;

                var left = -halfWidth + x;
                var right = halfWidth + x;
                var bottom = -halfHeight + y;
                var top = halfHeight + y;

                return Matrix4x4.Ortho(left, right, bottom, top, near, far);
            }
            else
            {
                if (near <= 0f)
                {
                    return camera.projectionMatrix;
                }

                var halfHeight = near * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                var halfWidth = halfHeight * aspect;
                var x = centerOffset.x * halfWidth;
                var y = centerOffset.y * halfHeight;

                var left = -halfWidth + x;
                var right = halfWidth + x;
                var bottom = -halfHeight + y;
                var top = halfHeight + y;

                return Matrix4x4.Frustum(left, right, bottom, top, near, far);
            }
        }

        private static class Burst
        {
            public static readonly SharedStatic<BurstTrampoline> CameraViewSpaceOffset =
                SharedStatic<BurstTrampoline>.GetOrCreate<CameraMatrixShiftSyncSystem, CameraViewSpaceOffset>();
        }
    }
}
