// <copyright file="CMFreeLookModifier.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Entities;

    public struct CMFreeLookModifier : IComponentData
    {
        public float Easing;
    }

    public enum CMFreeLookModifierType : byte
    {
        Tilt,
        Lens,
        PositionDamping,
        Composition,
        Distance,
        Noise,
    }
}
#endif