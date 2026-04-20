# Cinemachine

## Summary
Bridge bakes CinemachineCamera and its modules into ECS components and keeps them in sync at runtime. You can drive Cinemachine entirely from ECS data.

## Requirements
- com.unity.cinemachine package.
- Unity Splines for Spline Dolly and Spline Dolly LookAt targets.
- Unity Physics for CinemachineThirdPersonFollowDots obstacle avoidance.

## Authoring
1. Add `CinemachineBrain` to the main camera. `CameraMainAuthoring` will pick it up automatically.
2. Add a `CinemachineCamera` GameObject in a SubScene.
3. Add any modules you need. Bridge supports:
- `CinemachineFollow`
- `CinemachinePositionComposer`
- `CinemachineRotationComposer`
- `CinemachineThirdPersonFollowDots`
- `CinemachineOrbitalFollow`
- `CinemachineSplineDolly`
- `CinemachineFreeLookModifier`
- `CinemachineRotateWithFollowTarget`
- `CinemachineHardLockToTarget`
- `CinemachineHardLookAt`
- `CinemachineSplineDollyLookAtTargets`
- `CinemachinePanTilt`
- `CinemachineBasicMultiChannelPerlin`
- `CinemachineGroupFraming`
- `CinemachineFollowZoom`
- `CinemachineCameraOffset`
- `CinemachineRecomposer`
- `CinemachineVolumeSettings`
4. If you use Spline Dolly, ensure the target `SplineContainer` also has `SplineContainerBridgeAuthoring`.

## ECS Components
- `CMCamera` stores lens and target data.
- `CMCameraEnabled` toggles the `CinemachineCamera` component.
- Module-specific components such as `CMFollow`, `CMPositionComposer`, and `CMRotationComposer`.
- `CMBrain` mirrors `CinemachineBrain` settings on the main camera.

## Target Bridging
`CMCamera.TrackingTarget` and `CMCamera.LookAtTarget` store entity references. Bridge creates pooled hidden GameObjects and copies entity transforms into them so Cinemachine can follow and look at ECS entities.

## Runtime Control
Enable a camera and change priority and follow offset:

```csharp
SystemAPI.SetComponentEnabled<CMCameraEnabled>(cameraEntity, true);

var cam = SystemAPI.GetComponentRW<CMCamera>(cameraEntity);
cam.ValueRW.Priority = 20;
cam.ValueRW.FieldOfView = 60f;

var follow = SystemAPI.GetComponentRW<CMFollow>(cameraEntity);
follow.ValueRW.FollowOffset = new float3(0f, 2f, -5f);
```

Update spline dolly position:

```csharp
var dolly = SystemAPI.GetComponentRW<CMSplineDolly>(cameraEntity);
dolly.ValueRW.Position = 5f;
```

## Notes
- Spline Dolly targets must be entities with `LocalToWorld` so Bridge can sync their transforms.
- `CinemachineVolumeSettings` focus targets use the same bridging mechanism.
