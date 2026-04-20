# Audio

## Summary
Bridge Audio bakes AudioSource data into ECS components and plays them through pooled AudioSource objects. Only the closest, highest-priority sources around the main camera stay active. Music playback uses two pooled AudioSources for crossfades.

## Requirements
- Unity Audio.
- A `CameraMain` entity for listener position. Bridge creates one from `Camera.main` if you do not add `CameraMainAuthoring`.

## Authoring
1. Add an `AudioSource` to a GameObject in a SubScene.
2. Set `loop` for ambience or leave it off for one-shot playback.
3. Optional: add filter components such as `AudioLowPassFilter`, `AudioHighPassFilter`, `AudioDistortionFilter`, `AudioEchoFilter`, `AudioReverbFilter`, and `AudioChorusFilter`.
4. Bake the SubScene.

Bridge adds ECS components including `AudioSourceData`, `AudioSourceDataExtended`, `AudioSourceAudibleRange`, `AudioSourceIndex`, and `AudioSourceEnabled`. Non-looped sources also get `AudioSourceOneShot`. Filter components add their matching data components.

## Pooling
Bridge keeps a fixed pool of managed AudioSources and assigns them based on audibility, priority, and distance.
- Pool sizes are configured in `BridgeSettings` under `BovineLabs -> Settings -> Bridge`.
- `LoopedAudioPoolSize` controls looped sources.
- `OneShotAudioPoolSize` controls one-shot sources.
- Audibility is computed from rolloff and volume and stored in `AudioSourceAudibleRange`.
- Selection uses `AudioSourceDataExtended.Priority` and distance from the listener.

## Runtime Control
Enable a looped source and adjust volume and pitch:

```csharp
var data = SystemAPI.GetComponentRW<AudioSourceData>(entity);
data.ValueRW.Volume = 0.5f;
data.ValueRW.Pitch = 1.0f;

SystemAPI.SetComponentEnabled<AudioSourceEnabled>(entity, true);
```

Play a one-shot clip:

```csharp
var extended = SystemAPI.GetComponentRW<AudioSourceDataExtended>(entity);
extended.ValueRW.Clip = clip;

SystemAPI.SetComponentEnabled<AudioSourceEnabled>(entity, true);
// Disable again after the request to avoid re-triggering.
SystemAPI.SetComponentEnabled<AudioSourceEnabled>(entity, false);
```

## Music
Bridge provides two dedicated music slots for crossfading.
- Create `MusicTrackDefinition` assets and assign them in `BridgeSettings`.
- `DefaultBlendSeconds` controls crossfade time and each track can override it.
- `MusicSelection.TrackId` selects which track to play. `0` is silence.

```csharp
SystemAPI.GetSingletonRW<MusicSelection>().ValueRW.TrackId = trackId;
```

Optional: if you want to manage the music AudioSources yourself, add a GameObject with `MusicSource` and at least two `AudioSource` components. Bridge uses the first two sources it finds.

## Notes
- Audio Reverb Zones and AudioMixerSnapshot data are baked but currently not synced by runtime systems.
