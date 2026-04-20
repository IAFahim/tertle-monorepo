# Lighting

## Summary
Bridge syncs ECS light components to managed Unity `Light` objects, including URP and HDRP additional data when those pipelines are installed.

## Requirements
- Unity Lights.
- URP for `LightUniversalData`.
- HDRP for `LightHdData`.

## Authoring
1. Add a `Light` component to a GameObject in a SubScene.
2. Optional: add `UniversalAdditionalLightData` (URP) or `HDAdditionalLightData` (HDRP).
3. Bake the SubScene.

Bridge adds `LightData`, `LightDataExtended`, and `LightEnabled` plus pipeline-specific data components.

Bridge uses pooled companion GameObjects for lights; the authoring Light is used for baking only.

## Runtime Control

```csharp
var data = SystemAPI.GetComponentRW<LightData>(entity);
data.ValueRW.Color = Color.red;
data.ValueRW.Intensity = 2f;

SystemAPI.SetComponentEnabled<LightEnabled>(entity, true);
```

## Pipeline-Specific Data
- URP: `LightUniversalData` maps to `UniversalAdditionalLightData`.
- HDRP: `LightHdData` maps to `HDAdditionalLightData`.

## Notes
- Bridge uses pooled GameObjects. Hide flags are controlled by the config var `bridge.hide-flags` in `BridgeObjectConfig`.
