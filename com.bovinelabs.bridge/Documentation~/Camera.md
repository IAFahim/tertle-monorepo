# Camera

## Summary
Bridge exposes the main camera to ECS, provides frustum data for culling, and allows projection center offsets for jitter or view shifts.

## Authoring
- Add `CameraMainAuthoring` to your camera in a SubScene to create a `CameraMain` entity and configure `projectionCenterOffset`.
- If you do not add it, Bridge creates a `CameraMain` entity at runtime using `Camera.main`.

## Components
- `CameraMain` marks the main camera entity.
- `CameraBridge` stores a `UnityObjectRef<Camera>`.
- `CameraFrustumPlanes` and `CameraFrustumCorners` are updated each frame.
- `CameraViewSpaceOffset` controls projection center shift.

## Usage
Frustum culling with the built-in helpers:

```csharp
foreach (var (planes, bounds) in SystemAPI.Query<CameraFrustumPlanes, RenderBounds>())
{
    var aabb = new AABB
    {
        Center = bounds.Value.Center,
        Extents = bounds.Value.Extents,
    };

    if (!planes.AnyIntersect(aabb))
    {
        continue;
    }
}
```

Apply a projection center offset for jitter:

```csharp
SystemAPI.GetSingletonRW<CameraViewSpaceOffset>().ValueRW.ProjectionCenterOffset = new float2(jitterX, jitterY);
```

## Cinemachine
When Cinemachine is installed, `CameraMainAuthoring` also adds `CMBrain` and `CinemachineBrainBridge`, and the main camera brain is synced by `CameraMainSystem`.
