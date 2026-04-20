# Volume

## Summary
Bridge bakes URP Volume settings into ECS components and syncs changes back to a managed `Volume` at runtime.

## Requirements
- URP.

## Authoring
1. Add a `Volume` component in a SubScene.
2. Assign a Volume Profile (shared or instantiated).
3. Bake the SubScene.

Bridge adds `VolumeSettings` and components for any effects found in the profile:
- `VolumeBloom`
- `VolumeChannelMixer`
- `VolumeChromaticAberration`
- `VolumeColorAdjustments`
- `VolumeColorCurves`
- `VolumeColorLookup`
- `VolumeDepthOfField`
- `VolumeFilmGrain`
- `VolumeLensDistortion`
- `VolumeLiftGammaGain`
- `VolumeMotionBlur`
- `VolumePaniniProjection`
- `VolumeScreenSpaceLensFlare`
- `VolumeShadowsMidtonesHighlights`
- `VolumeSplitToning`
- `VolumeTonemapping`
- `VolumeVignette`
- `VolumeWhiteBalance`

## Runtime Control

```csharp
var settings = SystemAPI.GetComponentRW<VolumeSettings>(entity);
settings.ValueRW.Weight = 0.75f;

var bloom = SystemAPI.GetComponentRW<VolumeBloom>(entity);
bloom.ValueRW.Active = true;
bloom.ValueRW.Intensity = 1.5f;
bloom.ValueRW.IntensityOverride = true;
```

## Notes
- Only effects present in the Volume Profile at bake time get ECS components. If you need to drive an effect later, make sure it exists in the profile or add the component manually.
- Bridge writes to `volume.profile` at runtime, so ensure the Volume can instantiate a profile if you are using a shared profile.
