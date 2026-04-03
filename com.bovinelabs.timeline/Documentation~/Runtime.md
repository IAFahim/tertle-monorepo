# Runtime Playback and Timers

This document focuses on runtime control, clocks, and timer ranges.

## Activation and Pause
Timeline playback is controlled by enableable components:
- TimelineActive controls whether the timeline evaluates.
- TimerPaused pauses the timer while leaving the timeline active.

```csharp
using BovineLabs.Timeline.Data;
using BovineLabs.Timeline.Data.Schedular;

EntityManager.SetComponentEnabled<TimelineActive>(directorEntity, true);
EntityManager.SetComponentEnabled<TimerPaused>(directorEntity, true);
EntityManager.SetComponentEnabled<TimerPaused>(directorEntity, false);
```

## Clock Settings
ClockSettings controls how time advances. It is populated from PlayableDirector.timeUpdateMode during baking.
- GameTime -> ClockUpdateMode.GameTime
- UnscaledGameTime -> ClockUpdateMode.UnscaledGameTime
- Manual -> ClockUpdateMode.Constant
- DSPClock -> falls back to UnscaledGameTime with a warning

You can change the clock at runtime:

```csharp
using BovineLabs.Timeline.Data.Schedular;
using Unity.IntegerTime;

var clockSettings = EntityManager.GetComponentData<ClockSettings>(directorEntity);
clockSettings.UpdateMode = ClockUpdateMode.Constant;
clockSettings.DeltaTime = new DiscreteTime(1.0 / 60.0);
clockSettings.TimeScale = 1f;
clockSettings.Reverse = false;
EntityManager.SetComponentData(directorEntity, clockSettings);
```

## Timer Ranges
TimerRange constrains playback and defines the stop behavior.
- AutoStop: stops when the range ends
- AutoPause: holds when the range ends
- Loop: loops to the start of the range

```csharp
using BovineLabs.Timeline.Data.Schedular;
using Unity.IntegerTime;

EntityManager.SetComponentData(directorEntity, new TimerRange
{
    Behaviour = RangeBehaviour.AutoStop,
    Range = new DiscreteTimeInterval(DiscreteTime.Zero, new DiscreteTime(10.0)),
    SampleLastFrame = true,
});
```

## Reverse Playback
Set ClockSettings.Reverse to run the clock backwards. TimerUpdateSystem applies the reverse flag to timer updates and ranges.
