# BovineLabs Timeline

A high-performance, DOTS and Burst-compatible implementation of Unity Timeline for ECS projects. This package enables you to use Unity's Timeline Editor to author complex sequences and animations that run entirely in DOTS with full Burst compilation support.

Loosely based on the original official package from https://docs.unity3d.com/Packages/com.unity.timeline.dots@latest but with significant modernizations, architectural improvements, and simulation logic enhancements.

## Features

- **Full DOTS/Burst compatibility**: All runtime systems are Burst-compiled for maximum performance
- **Unity Timeline Editor integration**: Author timelines using Unity's familiar Timeline Editor
- **Flexible scheduling system**: Supports multiple clock types (game time, unscaled time, constant rate)
- **Timer system**: Advanced timer management with pause/resume, time scaling, and composite timers
- **Clip extrapolation**: Support for loop, ping-pong, and hold behaviors beyond clip boundaries
- **Track blending**: Weighted blending between multiple clips on the same track
- **Nested timelines**: Create complex sequences with sub-directors and composite timers
- **Extensible architecture**: Easy-to-extend base classes for custom tracks and clips

## Installation

### Requirements
- BovineLabs Core: 1.3.6 or newer
  - Available at https://gitlab.com/tertle/com.bovinelabs.core or via OpenUPM: https://openupm.com/packages/com.bovinelabs.core/

### Sample
A comprehensive sample is included via the Unity Package Manager showing basic timeline usage and custom track implementation.

## Architecture

### System Execution Order

The timeline systems execute in the `BeforeTransformSystemGroup` with the following structure:

```
TimelineSystemGroup (BeforeTransformSystemGroup)
├── ScheduleSystemGroup
│   ├── ClockUpdateSystem - Updates clock data from Unity time
│   └── TimerUpdateSystem - Updates all timers based on clocks
├── TimelineUpdateSystemGroup
│   ├── ClipLocalTimeSystem - Calculates clip local time
│   └── ClipWeightSystem - Evaluates clip blending weights
├── TimelineComponentAnimationGroup
│   └── [Your custom track systems go here]
├── ClipActivePreviousSystem - Tracks previous active state
└── TimelineActivePreviousSystem - Tracks timeline active state
```

### Core Concepts

**Timeline (Director)**: The root entity that represents a PlayableDirector. Contains timing information and controls playback.

**Track**: A sequence container that holds clips. Each track targets a specific bound object.

**Clip**: Individual timeline segments that contain the data to animate or trigger behaviors.

**Timer**: Controls time progression with support for pause, speed scaling, and time ranges.

**Clock**: Provides delta time to timers. Multiple clock types are supported:
- `ClockTypeGameTime`: Uses `Time.timeScale` and `Time.deltaTime`
- `ClockTypeUnscaledGameTime`: Uses `Time.unscaledDeltaTime` (ignores timeScale)
- `ClockTypeConstant`: Custom constant delta time and scale

## How to Use

### Basic Timeline Setup

1. **Create a Timeline Asset**:
   - Right-click in Project window → Create → Timeline
   - Open Timeline Editor window (Window → Sequencing → Timeline)

2. **Add a PlayableDirector**:
   - Add a GameObject with a `PlayableDirector` component
   - Assign your Timeline asset to it
   - The director will be automatically baked to ECS entities

3. **Activate the Timeline**:
   ```csharp
   // Enable the TimelineActive component to start playback
   EntityManager.SetComponentEnabled<TimelineActive>(directorEntity, true);
   ```

### Creating Custom Tracks

To create a custom track that animates ECS components:

1. **Define your clip data** (inherits from `DOTSClip`):

```csharp
using BovineLabs.Timeline.Authoring;
using Unity.Entities;

[Serializable]
public class MyCustomClip : DOTSClip
{
    public float someValue;
    public Vector3 someVector;

    public override void Bake(Entity clipEntity, BakingContext context)
    {
        // Add components to the clip entity
        context.Baker.AddComponent(clipEntity, new MyClipData
        {
            Value = someValue,
            Vector = someVector
        });
    }
}
```

2. **Define your track** (inherits from `DOTSTrack`):

```csharp
[TrackClipType(typeof(MyCustomClip))]
[TrackBindingType(typeof(Transform))]
public class MyCustomTrack : DOTSTrack
{
    protected override void Bake(BakingContext context)
    {
        // Add track-level components if needed
        if (context.Binding is GameObject go)
        {
            var entity = context.Baker.GetEntity(go, TransformUsageFlags.Dynamic);
            context.Baker.AddComponent(entity, new MyAnimatedComponent());
        }
    }
}
```

3. **Create a system to process the track**:

```csharp
[UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
public partial struct MyCustomTrackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Use TrackBlendImpl helper to blend multiple clips
        foreach (var (transform, clips) in
            SystemAPI.Query<RefRW<LocalTransform>>()
                .WithAll<MyAnimatedComponent>())
        {
            // Your blending logic here
            // See sample tracks for complete examples
        }
    }
}
```

### Using Mixers for Blending

The package provides built-in mixer helpers for common types:

```csharp
using BovineLabs.Timeline.Mixers;

// In your track system:
var mixer = new Float3Mixer();
foreach (var clipData in clips)
{
    mixer.Add(clipData.Position, clipWeight.Value);
}
var blendedValue = mixer.GetValue();
```

Available mixers: `FloatMixer`, `Float2Mixer`, `Float3Mixer`, `Float4Mixer`, `QuaternionMixer`

### Timer Control

```csharp
// Pause a timeline
EntityManager.SetComponentEnabled<TimerPaused>(timerEntity, true);

// Change time scale
var clockData = EntityManager.GetComponentData<ClockData>(timerEntity);
clockData.Scale = 2.0; // 2x speed
EntityManager.SetComponentData(timerEntity, clockData);

// Set a timer range (auto-stop at end time)
EntityManager.SetComponentData(timerEntity, new TimerRange
{
    Start = DiscreteTime.Zero,
    End = new DiscreteTime(10.0), // Stop after 10 seconds
    Mode = TimerRangeMode.Once
});
```

### Nested Timelines

Create sub-timelines using `SubDirectorTrack` and `SubDirectorClip` to compose complex sequences:

1. Add a `SubDirectorTrack` to your main timeline
2. Create clips with `SubDirectorClip` and assign timeline assets
3. The sub-timelines will be baked as composite timers with proper time transforms

## Advanced Features

### Clip Extrapolation

Control what happens when time is outside clip bounds:

- **Loop**: Repeats the clip indefinitely (pre/post)
- **PingPong**: Bounces back and forth (pre/post)
- **Hold**: Holds the first/last frame value (pre/post)

These are configured per-clip in the Timeline Editor under clip extrapolation settings.

### Track Reset on Deactivate

Tracks can optionally reset their target components when deactivated:

```csharp
public class MyTrack : DOTSTrack
{
    // Enable reset behavior
    // This is controlled via the inspector checkbox "Reset On Deactivate"
}
```

Implement `IAnimatedComponent<T>` on your components to provide default values:

```csharp
public struct MyAnimatedComponent : IComponentData, IAnimatedComponent<float3>
{
    public float3 Value { get; set; }
}
```

## Known Limitations

- Timeline playback is controlled via `TimelineActive` component (no play/pause controls in editor during play mode)
- Nested prefab timelines require additional setup
- Some Unity Timeline features (like markers) are not yet supported

## Future Work

- Additional built-in tracks and clips
- Enhanced prefab lifecycle support
- Signal and marker system

## Support

For issues, feature requests, or questions, please use the issue tracker at the source repository.

## License

Copyright (c) BovineLabs. All rights reserved.
