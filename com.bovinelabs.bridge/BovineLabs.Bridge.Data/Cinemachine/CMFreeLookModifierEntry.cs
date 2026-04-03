// <copyright file="CMFreeLookModifierEntry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    public struct CMFreeLookModifierEntry : IBufferElementData
    {
        public CMFreeLookModifierType Type;

        public float TiltTop;
        public float TiltBottom;

        public CMFreeLookModifierLensSettings LensTop;
        public CMFreeLookModifierLensSettings LensBottom;

        public ScreenComposerSettings CompositionTop;
        public ScreenComposerSettings CompositionBottom;

        public float3 PositionDampingTop;
        public float3 PositionDampingBottom;

        public float DistanceTop;
        public float DistanceBottom;

        public CMFreeLookModifierNoiseSettings NoiseTop;
        public CMFreeLookModifierNoiseSettings NoiseBottom;
    }

    public struct CMFreeLookModifierNoiseSettings
    {
        public float Amplitude;
        public float Frequency;
    }

    public struct CMFreeLookModifierLensSettings
    {
        public float FieldOfView;
        public float OrthographicSize;
        public float NearClipPlane;
        public float FarClipPlane;
        public float Dutch;
        public LensSettings.OverrideModes ModeOverride;
        public PhysicalSettings PhysicalProperties;

        public struct PhysicalSettings
        {
            public Camera.GateFitMode GateFit;
            public float2 SensorSize;
            public float2 LensShift;
            public float FocusDistance;
            public int Iso;
            public float ShutterSpeed;
            public float Aperture;
            public int BladeCount;
            public float2 Curvature;
            public float BarrelClipping;
            public float Anamorphism;
        }
    }
}
#endif