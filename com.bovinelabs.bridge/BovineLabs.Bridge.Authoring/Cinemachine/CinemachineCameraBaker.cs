// <copyright file="CinemachineCameraBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Authoring.Cinemachine
{
    using BovineLabs.Bridge.Authoring.Spline;
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Collections;
    using BovineLabs.Core.Authoring;
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;
    using UnityEngine.Splines;

    [ForceBakingOnDisabledComponents]
    public class CinemachineCameraBaker : Baker<CinemachineCamera>
    {
        /// <inheritdoc />
        public override void Bake(CinemachineCamera authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.Dynamic);

            this.BakeCamera(entity, authoring);
            this.BakeFollow(entity, authoring);
            this.BakeSplineDolly(entity, authoring);
            this.BakePositionComposer(entity, authoring);
            this.BakeRotationComposer(entity, authoring);
            this.BakeThirdPersonFollow(entity, authoring);
            this.BakeOrbitalFollow(entity, authoring);
            this.BakeFreeLookModifier(entity, authoring);
            this.BakeRotateWithFollowTarget(entity, authoring);
            this.BakeHardLockToTarget(entity, authoring);
            this.BakeHardLookAt(entity, authoring);
            this.BakeSplineDollyLookAtTargets(entity, authoring);
            this.BakePanTilt(entity, authoring);
            this.BakeBasicMultiChannelPerlin(entity, authoring);
            this.BakeGroupFraming(entity, authoring);
            this.BakeFollowZoom(entity, authoring);
            this.BakeCameraOffset(entity, authoring);
            this.BakeRecomposer(entity, authoring);
            this.BakeVolumeSettings(entity, authoring);
        }

        private void BakeCamera(Entity entity, CinemachineCamera authoring)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var nameBlob = ref builder.ConstructRoot<BlobString>();
            builder.AllocateString(ref nameBlob, authoring.gameObject.name);
            var nameBlobRef = builder.CreateBlobAssetReference<BlobString>(Allocator.Persistent);
            this.AddBlobAsset(ref nameBlobRef, out _);
            var trackingTargetBridge = this.CreateAdditionalEntity(TransformUsageFlags.WorldSpace, entityName: $"{authoring.gameObject.name} TrackingTargetBridge");
            var lookAtTargetBridge = this.CreateAdditionalEntity(TransformUsageFlags.WorldSpace, entityName: $"{authoring.gameObject.name} LookAtTargetBridge");

            this.AddComponent(trackingTargetBridge, new CMCameraTargetBridgeObject());
            this.AddComponent(lookAtTargetBridge, new CMCameraTargetBridgeObject());

            this.AddEnabledComponent<CMCameraEnabled>(entity, authoring.enabled);
            this.AddComponent(entity, new CMCamera
            {
                Name = nameBlobRef,
                TrackingTarget = this.GetEntity(authoring.Target.TrackingTarget, TransformUsageFlags.None),
                LookAtTarget = this.GetEntity(authoring.Target.LookAtTarget, TransformUsageFlags.None),
                CustomLookAtTarget = authoring.Target.CustomLookAtTarget,
                Priority = authoring.Priority,
                OutputChannel = authoring.OutputChannel,
                BlendHint = authoring.BlendHint,
                FieldOfView = authoring.Lens.FieldOfView,
                OrthographicSize = authoring.Lens.OrthographicSize,
                NearClipPlane = authoring.Lens.NearClipPlane,
                FarClipPlane = authoring.Lens.FarClipPlane,
                Dutch = authoring.Lens.Dutch,
                ModeOverride = authoring.Lens.ModeOverride,
                StandbyUpdate = authoring.StandbyUpdate,
            });
            this.AddComponent(entity, new CMCameraTargetBridgeObjects
            {
                TrackingTargetBridge = trackingTargetBridge,
                LookAtTargetBridge = lookAtTargetBridge,
            });
        }

        private void BakeFollow(Entity entity, CinemachineCamera authoring)
        {
            var follow = authoring.GetComponent<CinemachineFollow>();
            if (follow == null)
            {
                return;
            }

            this.AddComponent(entity, new CMFollow
            {
                FollowOffset = follow.FollowOffset,
                TrackerSettings = follow.TrackerSettings,
            });
        }

        private void BakePositionComposer(Entity entity, CinemachineCamera authoring)
        {
            var positionComposer = authoring.GetComponent<CinemachinePositionComposer>();
            if (positionComposer == null)
            {
                return;
            }

            this.AddComponent(entity, new CMPositionComposer
            {
                CameraDistance = positionComposer.CameraDistance,
                DeadZoneDepth = positionComposer.DeadZoneDepth,
                Composition = positionComposer.Composition,
                TargetOffset = positionComposer.TargetOffset,
                Damping = positionComposer.Damping,
                Lookahead = positionComposer.Lookahead,
                CenterOnActivate = positionComposer.CenterOnActivate,
            });
        }

        private void BakeSplineDolly(Entity entity, CinemachineCamera authoring)
        {
            var splineDolly = authoring.GetComponent<CinemachineSplineDolly>();
            if (splineDolly == null)
            {
                return;
            }

            this.AddComponent(entity, new CMSplineDolly
            {
                Position = splineDolly.CameraPosition,
                PositionUnits = splineDolly.PositionUnits,
                SplineOffset = math.float3(splineDolly.SplineOffset),
                CameraRotation = splineDolly.CameraRotation,
                Damping = new CMSplineDollyDamping
                {
                    Enabled = splineDolly.Damping.Enabled,
                    Position = math.float3(splineDolly.Damping.Position),
                    Angular = splineDolly.Damping.Angular,
                },
                AutoDolly = ConvertAutoDolly(splineDolly.AutomaticDolly, authoring),
            });

            this.AddComponent(entity, new CMSplineDollyTarget { Spline = this.GetEntity(splineDolly.Spline, TransformUsageFlags.None) });

            if (splineDolly.Spline != null && splineDolly.Spline.GetComponent<SplineContainerBridgeAuthoring>() == null)
            {
                Debug.LogError($"CineMachineSplineDolly spline target {splineDolly.Spline} is required to have {nameof(SplineContainerBridgeAuthoring)}");
            }
        }

        private void BakeRotationComposer(Entity entity, CinemachineCamera authoring)
        {
            var rotationComposer = authoring.GetComponent<CinemachineRotationComposer>();
            if (rotationComposer == null)
            {
                return;
            }

            this.AddComponent(entity, new CMRotationComposer
            {
                TargetOffset = rotationComposer.TargetOffset,
                Lookahead = rotationComposer.Lookahead,
                Damping = rotationComposer.Damping,
                Composition = rotationComposer.Composition,
                CenterOnActivate = rotationComposer.CenterOnActivate,
            });
        }

        private void BakeThirdPersonFollow(Entity entity, CinemachineCamera authoring)
        {
            var thirdPerson = authoring.GetComponent<CinemachineThirdPersonFollowDots>();
            if (thirdPerson == null)
            {
                return;
            }

            this.AddComponent(entity, new CMThirdPersonFollow
            {
                Damping = thirdPerson.Damping,
                ShoulderOffset = thirdPerson.ShoulderOffset,
                VerticalArmLength = thirdPerson.VerticalArmLength,
                CameraSide = thirdPerson.CameraSide,
                CameraDistance = thirdPerson.CameraDistance,
                AvoidObstacles = thirdPerson.AvoidObstacles,
            });
        }

        private void BakeOrbitalFollow(Entity entity, CinemachineCamera authoring)
        {
            var orbit = authoring.GetComponent<CinemachineOrbitalFollow>();
            if (orbit == null)
            {
                return;
            }

            this.AddComponent(entity, new CMOrbitFollow
            {
                TargetOffset = orbit.TargetOffset,
                TrackerSettings = orbit.TrackerSettings,
                OrbitStyle = orbit.OrbitStyle,
                Radius = orbit.Radius,
                Orbits = orbit.Orbits,
                HorizontalAxis = orbit.HorizontalAxis,
                VerticalAxis = orbit.VerticalAxis,
                RadialAxis = orbit.RadialAxis,
                RecenteringTarget = orbit.RecenteringTarget,
            });
        }

        private void BakeFreeLookModifier(Entity entity, CinemachineCamera authoring)
        {
            var freeLookModifier = authoring.GetComponent<CinemachineFreeLookModifier>();
            if (freeLookModifier == null)
            {
                return;
            }

            this.AddComponent(entity, new CMFreeLookModifier
            {
                Easing = freeLookModifier.Easing,
            });

            var modifierBuffer = this.AddBuffer<CMFreeLookModifierEntry>(entity);
            var modifiers = freeLookModifier.Modifiers;
            if (modifiers == null)
            {
                return;
            }

            foreach (var modifier in modifiers)
            {
                if (modifier == null)
                {
                    continue;
                }

                switch (modifier)
                {
                    case CinemachineFreeLookModifier.TiltModifier tilt:
                        modifierBuffer.Add(new CMFreeLookModifierEntry
                        {
                            Type = CMFreeLookModifierType.Tilt,
                            TiltTop = tilt.Tilt.Top,
                            TiltBottom = tilt.Tilt.Bottom,
                        });

                        break;
                    case CinemachineFreeLookModifier.LensModifier lens:
                        modifierBuffer.Add(new CMFreeLookModifierEntry
                        {
                            Type = CMFreeLookModifierType.Lens,
                            LensTop = ConvertLensSettings(lens.Top),
                            LensBottom = ConvertLensSettings(lens.Bottom),
                        });

                        break;
                    case CinemachineFreeLookModifier.PositionDampingModifier damping:
                        modifierBuffer.Add(new CMFreeLookModifierEntry
                        {
                            Type = CMFreeLookModifierType.PositionDamping,
                            PositionDampingTop = math.float3(damping.Damping.Top),
                            PositionDampingBottom = math.float3(damping.Damping.Bottom),
                        });

                        break;
                    case CinemachineFreeLookModifier.CompositionModifier composition:
                        modifierBuffer.Add(new CMFreeLookModifierEntry
                        {
                            Type = CMFreeLookModifierType.Composition,
                            CompositionTop = composition.Composition.Top,
                            CompositionBottom = composition.Composition.Bottom,
                        });

                        break;
                    case CinemachineFreeLookModifier.DistanceModifier distance:
                        modifierBuffer.Add(new CMFreeLookModifierEntry
                        {
                            Type = CMFreeLookModifierType.Distance,
                            DistanceTop = distance.Distance.Top,
                            DistanceBottom = distance.Distance.Bottom,
                        });

                        break;
                    case CinemachineFreeLookModifier.NoiseModifier noiseModifier:
                        modifierBuffer.Add(new CMFreeLookModifierEntry
                        {
                            Type = CMFreeLookModifierType.Noise,
                            NoiseTop = new CMFreeLookModifierNoiseSettings
                            {
                                Amplitude = noiseModifier.Noise.Top.Amplitude,
                                Frequency = noiseModifier.Noise.Top.Frequency,
                            },
                            NoiseBottom = new CMFreeLookModifierNoiseSettings
                            {
                                Amplitude = noiseModifier.Noise.Bottom.Amplitude,
                                Frequency = noiseModifier.Noise.Bottom.Frequency,
                            },
                        });

                        break;
                    default:
                        Debug.LogWarning($"Unsupported CinemachineFreeLookModifier type '{modifier.GetType().Name}' during baking.", authoring);
                        break;
                }
            }
        }

        private void BakeRotateWithFollowTarget(Entity entity, CinemachineCamera authoring)
        {
            var rotateWithFollowTarget = authoring.GetComponent<CinemachineRotateWithFollowTarget>();
            if (rotateWithFollowTarget == null)
            {
                return;
            }

            this.AddComponent(entity, new CMRotateWithFollowTarget
            {
                Damping = rotateWithFollowTarget.Damping,
            });
        }

        private void BakeHardLockToTarget(Entity entity, CinemachineCamera authoring)
        {
            var hardLockToTarget = authoring.GetComponent<CinemachineHardLockToTarget>();
            if (hardLockToTarget == null)
            {
                return;
            }

            this.AddComponent(entity, new CMHardLockToTarget
            {
                Damping = hardLockToTarget.Damping,
            });
        }

        private void BakeHardLookAt(Entity entity, CinemachineCamera authoring)
        {
            var hardLookAt = authoring.GetComponent<CinemachineHardLookAt>();
            if (hardLookAt == null)
            {
                return;
            }

            this.AddComponent(entity, new CMHardLookAt
            {
                LookAtOffset = math.float3(hardLookAt.LookAtOffset),
            });
        }

        private void BakeSplineDollyLookAtTargets(Entity entity, CinemachineCamera authoring)
        {
            var lookAtTargets = authoring.GetComponent<CinemachineSplineDollyLookAtTargets>();
            if (lookAtTargets == null)
            {
                return;
            }

            var targetsComponent = new CMSplineDollyLookAtTargets
            {
                PathIndexUnit = lookAtTargets.Targets?.PathIndexUnit ?? PathIndexUnit.Knot,
            };

            this.AddComponent(entity, targetsComponent);
            var buffer = this.AddBuffer<CMSplineDollyLookAtTarget>(entity);
            var bridgeBuffer = this.AddBuffer<CMSplineDollyLookAtTargetBridge>(entity);

            if (lookAtTargets.Targets == null)
            {
                return;
            }

            var index = 0;
            foreach (var dataPoint in lookAtTargets.Targets)
            {
                var targetBridge = this.CreateAdditionalEntity(TransformUsageFlags.WorldSpace, entityName: $"SplineDollyLookAtTargetBridge {index}");
                this.AddComponent(targetBridge, new CMCameraTargetBridgeObject());

                var item = dataPoint.Value;
                buffer.Add(new CMSplineDollyLookAtTarget
                {
                    Position = dataPoint.Index,
                    LookAt = item.LookAt != null ? this.GetEntity(item.LookAt, TransformUsageFlags.Dynamic) : Entity.Null,
                    Offset = math.float3(item.Offset),
                    Easing = item.Easing,
                });
                bridgeBuffer.Add(new CMSplineDollyLookAtTargetBridge { Value = targetBridge });
                index++;
            }
        }

        private void BakePanTilt(Entity entity, CinemachineCamera authoring)
        {
            var pov = authoring.GetComponent<CinemachinePanTilt>();
            if (pov == null)
            {
                return;
            }

            this.AddComponent(entity, new CMPanTilt
            {
                ReferenceFrame = pov.ReferenceFrame,
                RecenterTarget = pov.RecenterTarget,
                PanAxis = pov.PanAxis,
                TiltAxis = pov.TiltAxis,
            });
        }

        private void BakeBasicMultiChannelPerlin(Entity entity, CinemachineCamera authoring)
        {
            var noise = authoring.GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise == null)
            {
                return;
            }

            this.AddComponent(entity, new CMBasicMultiChannelPerlin
            {
                PivotOffset = noise.PivotOffset,
                AmplitudeGain = noise.AmplitudeGain,
                FrequencyGain = noise.FrequencyGain,
                NoiseProfile = noise.NoiseProfile,
            });
        }

        private void BakeGroupFraming(Entity entity, CinemachineCamera authoring)
        {
            var groupFraming = authoring.GetComponent<CinemachineGroupFraming>();
            if (groupFraming == null)
            {
                return;
            }

            this.AddComponent(entity, new CMGroupFraming
            {
                FramingMode = groupFraming.FramingMode,
                FramingSize = groupFraming.FramingSize,
                CenterOffset = math.float2(groupFraming.CenterOffset),
                Damping = groupFraming.Damping,
                SizeAdjustment = groupFraming.SizeAdjustment,
                LateralAdjustment = groupFraming.LateralAdjustment,
                FovRange = math.float2(groupFraming.FovRange),
                DollyRange = math.float2(groupFraming.DollyRange),
                OrthoSizeRange = math.float2(groupFraming.OrthoSizeRange),
            });
        }

        private void BakeFollowZoom(Entity entity, CinemachineCamera authoring)
        {
            var followZoom = authoring.GetComponent<CinemachineFollowZoom>();
            if (followZoom == null)
            {
                return;
            }

            this.AddComponent(entity, new CMFollowZoom
            {
                Width = followZoom.Width,
                Damping = followZoom.Damping,
                FovRange = math.float2(followZoom.FovRange),
            });
        }

        private void BakeCameraOffset(Entity entity, CinemachineCamera authoring)
        {
            var cameraOffset = authoring.GetComponent<CinemachineCameraOffset>();
            if (cameraOffset == null)
            {
                return;
            }

            this.AddComponent(entity, new CMCameraOffset
            {
                Offset = math.float3(cameraOffset.Offset),
                ApplyAfter = cameraOffset.ApplyAfter,
                PreserveComposition = cameraOffset.PreserveComposition,
            });
        }

        private void BakeRecomposer(Entity entity, CinemachineCamera authoring)
        {
            var recomposer = authoring.GetComponent<CinemachineRecomposer>();
            if (recomposer == null)
            {
                return;
            }

            this.AddComponent(entity, new CMRecomposer
            {
                ApplyAfter = recomposer.ApplyAfter,
                Tilt = recomposer.Tilt,
                Pan = recomposer.Pan,
                Dutch = recomposer.Dutch,
                ZoomScale = recomposer.ZoomScale,
                FollowAttachment = recomposer.FollowAttachment,
                LookAtAttachment = recomposer.LookAtAttachment,
            });
        }

        private void BakeVolumeSettings(Entity entity, CinemachineCamera authoring)
        {
            var volumeSettings = authoring.GetComponent<CinemachineVolumeSettings>();
            if (volumeSettings == null)
            {
                return;
            }

            var focusTargetBridge = this.CreateAdditionalEntity(TransformUsageFlags.WorldSpace, entityName: $"{authoring.gameObject.name} VolumeSettingsFocusTargetBridge");
            this.AddComponent(focusTargetBridge, new CMCameraTargetBridgeObject());

            this.AddComponent(entity, new CMVolumeSettings
            {
                Weight = volumeSettings.Weight,
                FocusTracking = volumeSettings.FocusTracking,
                FocusTarget = this.GetEntity(volumeSettings.FocusTarget, TransformUsageFlags.None),
                FocusOffset = volumeSettings.FocusOffset,
                Profile = volumeSettings.Profile,
            });

            this.AddComponent(entity, new CMVolumeSettingsFocusBridge { Value = focusTargetBridge });
        }

        private static CMFreeLookModifierLensSettings ConvertLensSettings(LensSettings lens)
        {
            return new CMFreeLookModifierLensSettings
            {
                FieldOfView = lens.FieldOfView,
                OrthographicSize = lens.OrthographicSize,
                NearClipPlane = lens.NearClipPlane,
                FarClipPlane = lens.FarClipPlane,
                Dutch = lens.Dutch,
                ModeOverride = lens.ModeOverride,
                PhysicalProperties = new CMFreeLookModifierLensSettings.PhysicalSettings
                {
                    GateFit = lens.PhysicalProperties.GateFit,
                    SensorSize = math.float2(lens.PhysicalProperties.SensorSize),
                    LensShift = math.float2(lens.PhysicalProperties.LensShift),
                    FocusDistance = lens.PhysicalProperties.FocusDistance,
                    Iso = lens.PhysicalProperties.Iso,
                    ShutterSpeed = lens.PhysicalProperties.ShutterSpeed,
                    Aperture = lens.PhysicalProperties.Aperture,
                    BladeCount = lens.PhysicalProperties.BladeCount,
                    Curvature = math.float2(lens.PhysicalProperties.Curvature),
                    BarrelClipping = lens.PhysicalProperties.BarrelClipping,
                    Anamorphism = lens.PhysicalProperties.Anamorphism,
                },
            };
        }

        private static CMSplineAutoDolly ConvertAutoDolly(SplineAutoDolly autoDolly, CinemachineCamera authoring)
        {
            var result = new CMSplineAutoDolly
            {
                Enabled = autoDolly.Enabled,
                Type = CMSplineAutoDollyType.None,
            };

            if (autoDolly.Method == null)
            {
                return result;
            }

            switch (autoDolly.Method)
            {
                case SplineAutoDolly.FixedSpeed fixedSpeed:
                    result.Type = CMSplineAutoDollyType.FixedSpeed;
                    result.FixedSpeed = fixedSpeed.Speed;
                    break;
                case SplineAutoDolly.NearestPointToTarget nearestPoint:
                    result.Type = CMSplineAutoDollyType.NearestPointToTarget;
                    result.PositionOffset = nearestPoint.PositionOffset;
                    result.SearchResolution = nearestPoint.SearchResolution;
                    result.SearchIteration = nearestPoint.SearchIteration;
                    break;
                default:
                    Debug.LogWarning($"Unsupported SplineAutoDolly type '{autoDolly.Method.GetType().Name}' during baking.", authoring);
                    break;
            }

            return result;
        }
    }
}
#endif
