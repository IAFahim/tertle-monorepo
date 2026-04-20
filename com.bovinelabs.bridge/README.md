# BovineLabs Bridge

Bridge is the hybrid layer for Entities worlds. It keeps DOTS data in sync with GameObjects and provides pooled managed objects for systems that still require Unity components.

## Overview
Bridge includes feature-specific authoring and runtime sync for Audio, Camera, Cinemachine, Lighting, Volumes, Splines, Terrain/Water, and Input. Each feature has its own documentation page with setup and usage.

## Requirements
- Unity 6.3 or newer and Entities.

## Optional Dependencies
These packages are only required for the related features. Bridge compiles and runs without them.
- Cinemachine: Cinemachine bridging.
- Unity Splines: spline baking and Cinemachine Spline Dolly.
- URP: Volume support.
- HDRP: WaterSurface and HDRP light data.
- Unity Input System: Input integration.
- Unity Physics + Unity Terrain: terrain collider baking.

## System Groups
- `BridgeReadSystemGroup`: reads GameObject state into ECS early in simulation.
- `BridgeSimulationSystemGroup`: Bridge simulation after transforms.
- `BridgeSyncSystemGroup`: writes ECS state back to managed objects in Presentation.
- `BridgeTransformSyncSystemGroup`: runs after transforms to keep managed transforms aligned.

## Settings
- `BridgeSettings` (BovineLabs -> Settings -> Bridge): audio pool sizes and music configuration.
- `InputCommonSettings` (BovineLabs -> Settings -> Bridge -> Input Common): InputActionAsset and action map defaults.

## Getting Started
1. Add the authoring component for the feature you need to a GameObject in a SubScene.
2. Bake the SubScene so ECS components are created.
3. Drive ECS components at runtime; Bridge syncs the managed components.

## Documentation
- [Audio](Documentation~/Audio.md): AudioSource pooling, filters, one-shots, and music.
- [Camera](Documentation~/Camera.md): Main camera bridge, frustum data, and projection offsets.
- [Cinemachine](Documentation~/Cinemachine.md): Cinemachine camera and module sync.
- [Lighting](Documentation~/Lighting.md): Light sync with URP and HDRP data.
- [Volume](Documentation~/Volume.md): URP Volume profile baking and runtime control.
- [Spline](Documentation~/Spline.md): Unity Splines baking and runtime bridging.
- [Terrain and Water](Documentation~/Terrain.md): Terrain collider baking and HDRP water surfaces.
- [Input](Documentation~/Input.md): Input System source generation and action map control.

## Notes
- Audio uses pooled sources; only the closest and highest-priority sources are active.
- Cinemachine targets are bridged via pooled hidden GameObjects that mirror entity transforms.
- Volume effects are baked only if present in the profile at bake time.
