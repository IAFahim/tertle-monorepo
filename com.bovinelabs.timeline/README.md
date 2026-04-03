# BovineLabs Timeline

BovineLabs Timeline is a DOTS and Burst friendly runtime for Unity Timeline authoring. It bakes Timeline assets into ECS entities so you can keep using the Timeline Editor while running fully in DOTS.

Loosely based on the original official package from https://docs.unity3d.com/Packages/com.unity.timeline.dots@latest but with modernizations, architectural improvements, and simulation logic changes.

## Documentation
- [Overview](Documentation~/Overview.md)
- [Architecture and system order](Documentation~/Architecture.md)
- [Authoring tracks and clips](Documentation~/Authoring.md)
- [Runtime playback and timers](Documentation~/Runtime.md)
- [Nested timelines](Documentation~/Nesting.md)
- [Inspector override pairing](Documentation~/OverridePairing.md)
- [Limitations and roadmap](Documentation~/Limitations.md)

## Requirements
- Unity 6000.3 or newer
- com.unity.timeline 1.8.10
- com.bovinelabs.core 1.5.1
- Unity Entities package (required for DOTS)

## Installation
Add the package via UPM or git. Dependencies are declared in package.json and will be pulled by the Package Manager.

## Samples
Import the Sample via Package Manager: BovineLabs Timeline -> Samples -> Sample (imports Sample~).

## Support
For issues, feature requests, or questions, please use the issue tracker at the source repository.

## License
Copyright (c) BovineLabs. All rights reserved.
