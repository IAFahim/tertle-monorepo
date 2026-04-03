# Overview

BovineLabs Timeline is a DOTS and Burst friendly runtime for Unity Timeline authoring. It bakes Timeline assets into ECS entities so you can keep using the Timeline Editor while running fully in DOTS.

## Features
- Full DOTS/Burst compatibility for runtime systems
- Unity Timeline Editor integration for authoring
- Flexible clock and timer system (pause, scale, reverse, ranges)
- Clip extrapolation (loop, ping-pong, hold)
- Weighted track blending
- Nested timelines via sub directors and composite timers
- Extensible authoring base classes for custom tracks and clips

## Requirements
- Unity 6000.3 or newer
- com.unity.timeline 1.8.10
- com.bovinelabs.core 1.5.1
- Unity Entities package (required for DOTS)

## Installation
Add the package via UPM or git. Dependencies are declared in package.json and will be pulled by the Package Manager.

## Getting Started
1) Create a Timeline asset (Project window -> Create -> Timeline).
2) Add a PlayableDirector component and assign the Timeline asset.
3) Enter play mode and enable TimelineActive on the baked director entity.

```csharp
using BovineLabs.Timeline.Data;

EntityManager.SetComponentEnabled<TimelineActive>(directorEntity, true);
```

## Samples
Import the Sample via Package Manager: BovineLabs Timeline -> Samples -> Sample (imports Sample~).

## Next Steps
- [Architecture and system order](Architecture.md)
- [Authoring tracks and clips](Authoring.md)
- [Runtime playback and timers](Runtime.md)
- [Nested timelines](Nesting.md)
- [Inspector override pairing](OverridePairing.md)
- [Limitations and roadmap](Limitations.md)
