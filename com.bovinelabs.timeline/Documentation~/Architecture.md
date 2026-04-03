# Architecture

This document describes the baking pipeline, core entities/components, and system execution order.

## Baking Pipeline
1) PlayableDirectorBaker converts a PlayableDirector with a TimelineAsset into a timer entity.
2) DOTSTrack.BakeTrack creates a track entity and converts active DOTSClip assets into clip entities.
3) DOTSClip.Bake adds clip-specific components to each clip entity.

Only DOTSTrack and DOTSClip types are converted. Non-DOTS tracks and clip assets are ignored during baking.

## Entities and Components

Director (timer) entity:
- Timer, ClockSettings, ClockData
- TimerRange and TimerPaused
- TimelineActive and TimelineActivePrevious

Track entity:
- TrackBinding (target entity)
- TimerData
- TimelineActive and TimelineActivePrevious
- TrackResetOnDeactivate (optional)

Clip entity:
- Clip (parent track reference)
- ActiveRange, TimeTransform, LocalTime
- ClipActive and ClipActivePrevious
- ClipWeight and AnimatedClipWeight (when clip supports blending)
- Extrapolation components when pre/post extrapolation is enabled

## System Execution Order

```
TimelineSystemGroup (BeforeTransformSystemGroup)
├── ScheduleSystemGroup
│   ├── ClockUpdateSystem - Updates clock data from Unity time
│   └── TimerUpdateSystem - Updates timers and composite timers
├── TimelineUpdateSystemGroup
│   ├── ClipLocalTimeSystem - Calculates clip local time
│   └── ClipWeightSystem - Evaluates clip blending weights
├── TimelineComponentAnimationGroup
│   └── [Your custom track systems go here]
├── ClipActivePreviousSystem - Tracks previous clip active state
└── TimelineActivePreviousSystem - Tracks previous timeline active state
```

## Editor World Behavior
EditorTimelineSystem keeps editor preview in sync with the Timeline window. It:
- Forces TrackResetOnDeactivate on track entities in the editor world
- Enables TimelineActive for the selected Timeline window while time is inside the playable range
- Resets timers to time 0 when nothing is selected
