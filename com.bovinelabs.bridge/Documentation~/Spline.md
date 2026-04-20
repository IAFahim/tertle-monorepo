# Spline

## Summary
Bridge bakes Unity Splines into a BlobAsset for ECS and can optionally create a companion `SplineContainer` for GameObject users like Cinemachine.

## Requirements
- Unity Splines package.

## Authoring
- Add `SplineContainer` in a SubScene to bake splines into ECS data.
- Add `SplineContainerBridgeAuthoring` on the same GameObject if you need a runtime `SplineContainer` bridged back to GameObject systems.

Bridge adds `Splines`.

## Usage
Evaluate a spline from ECS data:

```csharp
var splines = SystemAPI.GetComponentRO<Splines>(entity);
ref var blobSplines = ref splines.ValueRO.Value.Value;
if (blobSplines.Length > 0)
{
    var position = blobSplines[0].EvaluatePosition(0.5f);
}
```

## Cinemachine Integration
Cinemachine Spline Dolly uses the bridged `SplineContainer`. Ensure the spline entity has `AddSplineBridge` by adding `SplineContainerBridgeAuthoring`.
