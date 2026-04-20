// <copyright file="TransformUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;
    using UnityEngine.Jobs;

    public static unsafe class TransformUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(Transform transform, LocalToWorld ltw)
        {
            transform.localPosition = ltw.Position;

            // We need to use the safe version as the vectors will not be normalized if there is some scale
            transform.localRotation = quaternion.LookRotationSafe(ltw.Forward, ltw.Up);
            transform.localScale = (*(Matrix4x4*)&ltw).lossyScale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set(TransformAccess transform, LocalToWorld ltw)
        {
            transform.localPosition = ltw.Position;

            // We need to use the safe version as the vectors will not be normalized if there is some scale
            transform.localRotation = quaternion.LookRotationSafe(ltw.Forward, ltw.Up);
            transform.localScale = (*(Matrix4x4*)&ltw).lossyScale;
        }
    }
}
