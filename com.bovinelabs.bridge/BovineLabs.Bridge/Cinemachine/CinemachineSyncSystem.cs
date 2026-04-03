// <copyright file="CinemachineSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Cinemachine
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Bridge.Data.Cinemachine;
    using BovineLabs.Core.Blobs;
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Physics;
    using Unity.Rendering;
    using UnityEngine;
    using UnityEngine.Splines;

    [UpdateInGroup(typeof(BridgeSystemGroup))]
    public partial class CinemachineSyncSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            this.BrainChanged();
            this.VirtualCameraChanged();
            this.FollowChanged();
            this.PositionComposerChanged();
            this.ComposerChanged();
            this.ThirdPersonCameraChanged();
            this.SplineDollyChanged();
            this.OrbitCameraChanged();
            this.FreeLookModifierChanged();
            this.RotateWithFollowTargetChanged();
            this.HardLockToTargetChanged();
            this.HardLookAtChanged();
            this.SplineDollyLookAtTargetsChanged();
            this.POVCameraChanged();
            this.BasicMultiChannelPerlinChanged();
            this.GroupFramingChanged();
            this.FollowZoomChanged();
            this.CameraOffsetChanged();
            this.RecomposerChanged();
            this.VolumeSettingsChanged();
        }

        private void BrainChanged()
        {
            foreach (var (component, brain) in SystemAPI
                .Query<CMBrain, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineBrain>>()
                .WithChangeFilter<CMBrain>())
            {
                brain.Value.IgnoreTimeScale = component.IgnoreTimeScale;
                brain.Value.UpdateMethod = component.UpdateMethod;
                brain.Value.BlendUpdateMethod = component.BlendUpdateMethod;
                brain.Value.DefaultBlend = component.DefaultBlend;
            }
        }

        private void VirtualCameraChanged()
        {
            foreach (var (component, cam) in SystemAPI
                .Query<CMCamera, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineCamera>>()
                .WithChangeFilter<CMCamera>())
            {
                cam.Value.enabled = component.Enabled;

                cam.Value.Target.TrackingTarget = this.EntityManager.HasComponent<CinemachineTrackingAuthoring>(component.TrackingTarget)
                    ? this.EntityManager.GetComponentObject<CinemachineTrackingAuthoring>(component.TrackingTarget).transform
                    : null;

                cam.Value.Target.LookAtTarget = this.EntityManager.HasComponent<CinemachineTrackingAuthoring>(component.LookAtTarget)
                    ? this.EntityManager.GetComponentObject<CinemachineTrackingAuthoring>(component.LookAtTarget).transform
                    : null;

                cam.Value.Target.CustomLookAtTarget = component.CustomLookAtTarget;
                cam.Value.Priority = component.Priority;
                cam.Value.OutputChannel = component.OutputChannel;
                cam.Value.BlendHint = component.BlendHint;

                var lens = cam.Value.Lens;
                lens.FieldOfView = math.clamp(component.FieldOfView, 1, 179);
                lens.NearClipPlane = component.NearClipPlane;
                lens.FarClipPlane = component.FarClipPlane;
                lens.Dutch = component.Dutch;
                lens.ModeOverride = component.ModeOverride;
                lens.OrthographicSize = component.OrthographicSize;
                cam.Value.Lens = lens;

                cam.Value.StandbyUpdate = component.StandbyUpdate;
            }
        }

        private void FollowChanged()
        {
            foreach (var (component, follow) in SystemAPI
                .Query<CMFollow, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineFollow>>()
                .WithChangeFilter<CMFollow>())
            {
                follow.Value.FollowOffset = component.FollowOffset;
                follow.Value.TrackerSettings = component.TrackerSettings;
            }
        }

        private void PositionComposerChanged()
        {
            foreach (var (component, composer) in SystemAPI
                .Query<CMPositionComposer, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachinePositionComposer>>()
                .WithChangeFilter<CMPositionComposer>())
            {
                composer.Value.CameraDistance = component.CameraDistance;
                composer.Value.DeadZoneDepth = component.DeadZoneDepth;
                composer.Value.Composition = component.Composition;
                composer.Value.TargetOffset = component.TargetOffset;
                composer.Value.Damping = component.Damping;
                composer.Value.Lookahead = component.Lookahead;
                composer.Value.CenterOnActivate = component.CenterOnActivate;
            }
        }

        private void ComposerChanged()
        {
            foreach (var (component, composer) in SystemAPI
                .Query<CMRotationComposer, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineRotationComposer>>()
                .WithChangeFilter<CMRotationComposer>())
            {
                composer.Value.TargetOffset = component.TargetOffset;
                composer.Value.Lookahead = component.Lookahead;
                composer.Value.Damping = component.Damping;
                composer.Value.Composition = component.Composition;
                composer.Value.CenterOnActivate = component.CenterOnActivate;
            }
        }

        private void ThirdPersonCameraChanged()
        {
            SystemAPI.TryGetSingletonEntity<PhysicsWorldSingleton>(out var physicsWorldEntity);

            foreach (var (component, cam) in SystemAPI
                .Query<CMThirdPersonFollow, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineThirdPersonFollowDots>>()
                .WithChangeFilter<CMThirdPersonFollow>())
            {
                cam.Value.Damping = component.Damping;
                cam.Value.ShoulderOffset = component.ShoulderOffset;
                cam.Value.VerticalArmLength = component.VerticalArmLength;
                cam.Value.CameraSide = math.clamp(component.CameraSide, 0, 1);
                cam.Value.CameraDistance = component.CameraDistance;
                cam.Value.AvoidObstacles = component.AvoidObstacles;
                cam.Value.World = physicsWorldEntity != Entity.Null ? this.World : null;
                cam.Value.PhysicsWorldEntity = physicsWorldEntity;
            }
        }

        private void SplineDollyChanged()
        {
            foreach (var (component, dolly) in SystemAPI
                .Query<CMSplineDolly, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineSplineDolly>>()
                .WithChangeFilter<CMSplineDolly>())
            {
                if (dolly.Value.Spline == null && this.EntityManager.HasComponent<Splines>(component.Spline))
                {
                    dolly.Value.Spline = dolly.Value.gameObject.AddComponent<SplineContainer>();

                    var splines = this.EntityManager.GetComponentData<Splines>(component.Spline);

                    if (!splines.Value.IsCreated)
                    {
                        dolly.Value.Spline.Splines = Array.Empty<Spline>();
                    }
                    else
                    {
                        ref var blobSplines = ref splines.Value.Value;
                        var managedSplines = new List<Spline>(blobSplines.Length);

                        for (var i = 0; i < blobSplines.Length; ++i)
                        {
                            managedSplines.Add(blobSplines[i].ToSpline());
                        }

                        dolly.Value.Spline.Splines = managedSplines;
                    }
                }

                ref var settings = ref dolly.Value.SplineSettings;
                settings.Position = component.Position;
                settings.Units = component.PositionUnits;
                settings.InvalidateCache();

                dolly.Value.SplineOffset = component.SplineOffset;
                dolly.Value.CameraRotation = component.CameraRotation;

                var damping = dolly.Value.Damping;
                damping.Enabled = component.Damping.Enabled;
                damping.Position = component.Damping.Position;
                damping.Angular = component.Damping.Angular;
                dolly.Value.Damping = damping;

                var autoDolly = dolly.Value.AutomaticDolly;
                autoDolly.Enabled = component.AutoDolly.Enabled;
                autoDolly.Method = CreateAutoDolly(component.AutoDolly);
                dolly.Value.AutomaticDolly = autoDolly;
            }
        }

        private void OrbitCameraChanged()
        {
            foreach (var (component, cam) in SystemAPI
                .Query<CMOrbitFollow, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineOrbitalFollow>>()
                .WithChangeFilter<CMOrbitFollow>())
            {
                cam.Value.TargetOffset = component.TargetOffset;
                cam.Value.TrackerSettings = component.TrackerSettings;
                cam.Value.OrbitStyle = component.OrbitStyle;
                cam.Value.Radius = component.Radius;
                cam.Value.Orbits = component.Orbits;
                cam.Value.HorizontalAxis = component.HorizontalAxis;
                cam.Value.VerticalAxis = component.VerticalAxis;
                cam.Value.RadialAxis = component.RadialAxis;
                cam.Value.RecenteringTarget = component.RecenteringTarget;
            }
        }

        private void FreeLookModifierChanged()
        {
            foreach (var (component, modifier) in SystemAPI
                .Query<CMFreeLookModifier, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineFreeLookModifier>>()
                .WithChangeFilter<CMFreeLookModifier>())
            {
                modifier.Value.Easing = component.Easing;
            }

            foreach (var (buffer, modifier) in SystemAPI
                .Query<DynamicBuffer<CMFreeLookModifierEntry>, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineFreeLookModifier>>()
                .WithChangeFilter<CMFreeLookModifierEntry>())
            {
                var managedModifiers = modifier.Value.Modifiers;
                if (managedModifiers == null)
                {
                    managedModifiers = new List<CinemachineFreeLookModifier.Modifier>(buffer.Length);
                }
                else
                {
                    managedModifiers.Clear();
                    if (managedModifiers.Capacity < buffer.Length)
                    {
                        managedModifiers.Capacity = buffer.Length;
                    }
                }

                var owner = modifier.Value.ComponentOwner;
                foreach (var entry in buffer)
                {
                    var newModifier = CreateModifier(entry);
                    if (newModifier == null)
                    {
                        continue;
                    }

                    managedModifiers.Add(newModifier);
                    if (owner != null)
                    {
                        newModifier.RefreshCache(owner);
                    }
                }

                modifier.Value.Modifiers = managedModifiers;
            }
        }

        private void RotateWithFollowTargetChanged()
        {
            foreach (var (component, rotate) in SystemAPI
                .Query<CMRotateWithFollowTarget, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineRotateWithFollowTarget>>()
                .WithChangeFilter<CMRotateWithFollowTarget>())
            {
                rotate.Value.Damping = component.Damping;
            }
        }

        private void HardLockToTargetChanged()
        {
            foreach (var (component, hardLock) in SystemAPI
                .Query<CMHardLockToTarget, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineHardLockToTarget>>()
                .WithChangeFilter<CMHardLockToTarget>())
            {
                hardLock.Value.Damping = component.Damping;
            }
        }

        private void HardLookAtChanged()
        {
            foreach (var (component, hardLook) in SystemAPI
                .Query<CMHardLookAt, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineHardLookAt>>()
                .WithChangeFilter<CMHardLookAt>())
            {
                hardLook.Value.LookAtOffset = new Vector3(component.LookAtOffset.x, component.LookAtOffset.y, component.LookAtOffset.z);
            }
        }

        private void SplineDollyLookAtTargetsChanged()
        {
            foreach (var (component, buffer, lookAt) in SystemAPI
                .Query<CMSplineDollyLookAtTargets, DynamicBuffer<CMSplineDollyLookAtTarget>,
                    SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineSplineDollyLookAtTargets>>()
                .WithChangeFilter<CMSplineDollyLookAtTargets>())
            {
                this.ApplySplineDollyLookAtTargets(component, buffer, lookAt.Value);
            }

            foreach (var (component, buffer, lookAt) in SystemAPI
                .Query<CMSplineDollyLookAtTargets, DynamicBuffer<CMSplineDollyLookAtTarget>,
                    SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineSplineDollyLookAtTargets>>()
                .WithChangeFilter<CMSplineDollyLookAtTarget>())
            {
                this.ApplySplineDollyLookAtTargets(component, buffer, lookAt.Value);
            }
        }

        private void ApplySplineDollyLookAtTargets(
            in CMSplineDollyLookAtTargets component, DynamicBuffer<CMSplineDollyLookAtTarget> buffer, CinemachineSplineDollyLookAtTargets lookAtTargets)
        {
            var targets = lookAtTargets.Targets ?? new SplineData<CinemachineSplineDollyLookAtTargets.Item>
            {
                DefaultValue = new CinemachineSplineDollyLookAtTargets.Item { Easing = 1f },
            };

            targets.PathIndexUnit = component.PathIndexUnit;
            targets.DefaultValue = new CinemachineSplineDollyLookAtTargets.Item { Easing = 1f };
            targets.Clear();

            foreach (var entry in buffer)
            {
                Transform lookAtTransform = null;

                if (entry.LookAt != Entity.Null && this.EntityManager.HasComponent<CinemachineTrackingAuthoring>(entry.LookAt))
                {
                    lookAtTransform = this.EntityManager.GetComponentObject<CinemachineTrackingAuthoring>(entry.LookAt).transform;
                }

                var item = new CinemachineSplineDollyLookAtTargets.Item
                {
                    LookAt = lookAtTransform,
                    Offset = entry.Offset,
                    Easing = entry.Easing,
                };

                targets.Add(new DataPoint<CinemachineSplineDollyLookAtTargets.Item>(entry.Position, item));
            }

            lookAtTargets.Targets = targets;
        }

        private void POVCameraChanged()
        {
            foreach (var (component, cam) in SystemAPI
                .Query<CMPanTilt, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachinePanTilt>>()
                .WithChangeFilter<CMPanTilt>())
            {
                cam.Value.ReferenceFrame = component.ReferenceFrame;
                cam.Value.RecenterTarget = component.RecenterTarget;
                cam.Value.PanAxis = component.PanAxis;
                cam.Value.TiltAxis = component.TiltAxis;
            }
        }

        private void BasicMultiChannelPerlinChanged()
        {
            foreach (var (component, noise) in SystemAPI
                .Query<CMBasicMultiChannelPerlin, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineBasicMultiChannelPerlin>>()
                .WithChangeFilter<CMBasicMultiChannelPerlin>())
            {
                noise.Value.PivotOffset = component.PivotOffset;
                noise.Value.AmplitudeGain = component.AmplitudeGain;
                noise.Value.FrequencyGain = component.FrequencyGain;
                noise.Value.NoiseProfile = component.NoiseProfile.Value;
            }
        }

        private void GroupFramingChanged()
        {
            foreach (var (component, extension) in SystemAPI
                .Query<CMGroupFraming, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineGroupFraming>>()
                .WithChangeFilter<CMGroupFraming>())
            {
                extension.Value.FramingMode = component.FramingMode;
                extension.Value.FramingSize = component.FramingSize;
                extension.Value.CenterOffset = new Vector2(component.CenterOffset.x, component.CenterOffset.y);
                extension.Value.Damping = component.Damping;
                extension.Value.SizeAdjustment = component.SizeAdjustment;
                extension.Value.LateralAdjustment = component.LateralAdjustment;
                extension.Value.FovRange = new Vector2(component.FovRange.x, component.FovRange.y);
                extension.Value.DollyRange = new Vector2(component.DollyRange.x, component.DollyRange.y);
                extension.Value.OrthoSizeRange = new Vector2(component.OrthoSizeRange.x, component.OrthoSizeRange.y);
            }
        }

        private void FollowZoomChanged()
        {
            foreach (var (component, extension) in SystemAPI
                .Query<CMFollowZoom, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineFollowZoom>>()
                .WithChangeFilter<CMFollowZoom>())
            {
                extension.Value.Width = component.Width;
                extension.Value.Damping = component.Damping;
                extension.Value.FovRange = new Vector2(component.FovRange.x, component.FovRange.y);
            }
        }

        private void CameraOffsetChanged()
        {
            foreach (var (component, extension) in SystemAPI
                .Query<CMCameraOffset, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineCameraOffset>>()
                .WithChangeFilter<CMCameraOffset>())
            {
                extension.Value.Offset = new Vector3(component.Offset.x, component.Offset.y, component.Offset.z);
                extension.Value.ApplyAfter = component.ApplyAfter;
                extension.Value.PreserveComposition = component.PreserveComposition;
            }
        }

        private void RecomposerChanged()
        {
            foreach (var (component, extension) in SystemAPI
                .Query<CMRecomposer, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineRecomposer>>()
                .WithChangeFilter<CMRecomposer>())
            {
                extension.Value.ApplyAfter = component.ApplyAfter;
                extension.Value.Tilt = component.Tilt;
                extension.Value.Pan = component.Pan;
                extension.Value.Dutch = component.Dutch;
                extension.Value.ZoomScale = component.ZoomScale;
                extension.Value.FollowAttachment = component.FollowAttachment;
                extension.Value.LookAtAttachment = component.LookAtAttachment;
            }
        }

        private void VolumeSettingsChanged()
        {
            foreach (var (component, volumeSettings) in SystemAPI
                .Query<CMVolumeSettings, SystemAPI.ManagedAPI.UnityEngineComponent<CinemachineVolumeSettings>>()
                .WithChangeFilter<CMVolumeSettings>())
            {
                volumeSettings.Value.Profile = component.Profile.Value;
                volumeSettings.Value.Weight = component.Weight;
                volumeSettings.Value.FocusTracking = component.FocusTracking;
                volumeSettings.Value.FocusOffset = component.FocusOffset;

                volumeSettings.Value.FocusTarget = this.EntityManager.HasComponent<CinemachineTrackingAuthoring>(component.FocusTarget)
                    ? this.EntityManager.GetComponentObject<CinemachineTrackingAuthoring>(component.FocusTarget).transform
                    : null;
            }
        }

        private static SplineAutoDolly.ISplineAutoDolly CreateAutoDolly(in CMSplineAutoDolly autoDolly)
        {
            switch (autoDolly.Type)
            {
                case CMSplineAutoDollyType.FixedSpeed:
                    return new SplineAutoDolly.FixedSpeed
                    {
                        Speed = autoDolly.FixedSpeed,
                    };
                case CMSplineAutoDollyType.NearestPointToTarget:
                    return new SplineAutoDolly.NearestPointToTarget
                    {
                        PositionOffset = autoDolly.PositionOffset,
                        SearchResolution = math.max(1, autoDolly.SearchResolution),
                        SearchIteration = math.max(1, autoDolly.SearchIteration),
                    };
                default:
                    return null;
            }
        }

        private static CinemachineFreeLookModifier.Modifier CreateModifier(in CMFreeLookModifierEntry entry)
        {
            switch (entry.Type)
            {
                case CMFreeLookModifierType.Tilt:
                    return new CinemachineFreeLookModifier.TiltModifier
                    {
                        Tilt = new CinemachineFreeLookModifier.TopBottomRigs<float>
                        {
                            Top = entry.TiltTop,
                            Bottom = entry.TiltBottom,
                        },
                    };
                case CMFreeLookModifierType.Lens:
                    return new CinemachineFreeLookModifier.LensModifier
                    {
                        Top = ToLensSettings(entry.LensTop),
                        Bottom = ToLensSettings(entry.LensBottom),
                    };
                case CMFreeLookModifierType.PositionDamping:
                    return new CinemachineFreeLookModifier.PositionDampingModifier
                    {
                        Damping = new CinemachineFreeLookModifier.TopBottomRigs<Vector3>
                        {
                            Top = new Vector3(entry.PositionDampingTop.x, entry.PositionDampingTop.y, entry.PositionDampingTop.z),
                            Bottom = new Vector3(entry.PositionDampingBottom.x, entry.PositionDampingBottom.y, entry.PositionDampingBottom.z),
                        },
                    };
                case CMFreeLookModifierType.Composition:
                    return new CinemachineFreeLookModifier.CompositionModifier
                    {
                        Composition = new CinemachineFreeLookModifier.TopBottomRigs<ScreenComposerSettings>
                        {
                            Top = entry.CompositionTop,
                            Bottom = entry.CompositionBottom,
                        },
                    };
                case CMFreeLookModifierType.Distance:
                    return new CinemachineFreeLookModifier.DistanceModifier
                    {
                        Distance = new CinemachineFreeLookModifier.TopBottomRigs<float>
                        {
                            Top = entry.DistanceTop,
                            Bottom = entry.DistanceBottom,
                        },
                    };
                case CMFreeLookModifierType.Noise:
                    return new CinemachineFreeLookModifier.NoiseModifier
                    {
                        Noise = new CinemachineFreeLookModifier.TopBottomRigs<CinemachineFreeLookModifier.NoiseModifier.NoiseSettings>
                        {
                            Top = new CinemachineFreeLookModifier.NoiseModifier.NoiseSettings
                            {
                                Amplitude = entry.NoiseTop.Amplitude,
                                Frequency = entry.NoiseTop.Frequency,
                            },
                            Bottom = new CinemachineFreeLookModifier.NoiseModifier.NoiseSettings
                            {
                                Amplitude = entry.NoiseBottom.Amplitude,
                                Frequency = entry.NoiseBottom.Frequency,
                            },
                        },
                    };
                default:
                    return null;
            }
        }

        private static LensSettings ToLensSettings(in CMFreeLookModifierLensSettings source)
        {
            var lens = LensSettings.Default;
            lens.FieldOfView = source.FieldOfView;
            lens.OrthographicSize = source.OrthographicSize;
            lens.NearClipPlane = source.NearClipPlane;
            lens.FarClipPlane = source.FarClipPlane;
            lens.Dutch = source.Dutch;
            lens.ModeOverride = source.ModeOverride;
            lens.PhysicalProperties = new LensSettings.PhysicalSettings
            {
                GateFit = source.PhysicalProperties.GateFit,
                SensorSize = new Vector2(source.PhysicalProperties.SensorSize.x, source.PhysicalProperties.SensorSize.y),
                LensShift = new Vector2(source.PhysicalProperties.LensShift.x, source.PhysicalProperties.LensShift.y),
                FocusDistance = source.PhysicalProperties.FocusDistance,
                Iso = source.PhysicalProperties.Iso,
                ShutterSpeed = source.PhysicalProperties.ShutterSpeed,
                Aperture = source.PhysicalProperties.Aperture,
                BladeCount = source.PhysicalProperties.BladeCount,
                Curvature = new Vector2(source.PhysicalProperties.Curvature.x, source.PhysicalProperties.Curvature.y),
                BarrelClipping = source.PhysicalProperties.BarrelClipping,
                Anamorphism = source.PhysicalProperties.Anamorphism,
            };

            return lens;
        }
    }
}
#endif