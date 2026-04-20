// <copyright file="BridgeType.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data
{
    using System;
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
#endif
    using Unity.Entities;

    [Flags]
    public enum UnityComponentType
    {
        None = 0,
        Light = 1 << 0,
#if UNITY_URP
        Volume = 1 << 1,
#endif
#if UNITY_CINEMACHINE
        Cinemachine = 1 << 2,
#endif

#if UNITY_SPLINES
        Spline = 1 << 30,
#endif
    }

    // Chunk component
    public struct BridgeType : IComponentData, IEquatable<BridgeType>
    {
        public UnityComponentType Types;
#if UNITY_CINEMACHINE
        public CMCameraRuntimeType Cinemachine;
#endif

        public bool Equals(BridgeType other)
        {
            return this.Types == other.Types
#if UNITY_CINEMACHINE
                   && this.Cinemachine == other.Cinemachine
#endif
                ;
        }

        public override int GetHashCode()
        {
            var hash = (int)this.Types;
#if UNITY_CINEMACHINE
            hash = (hash * 397) ^ (int)this.Cinemachine;
#endif
            return hash;
        }
    }
}
