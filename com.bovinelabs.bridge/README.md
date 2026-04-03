# BovineLabs Bridge

Bridge is the hybrid layer for Entities worlds: it keeps DOTS data in sync with GameObjects

## What it covers
- **Audio**: Bakers for `AudioSource` plus all built-in filters map settings onto ECS components; a pooled `AudioSource` set and priority system keep only the closest sources around the main camera playing, with pool size and listen distance configurable via `BridgeSettings`.
- **Cinemachine**: Bakers cover `CinemachineBrain`, `CinemachineCamera`, and the main rigs/modules (follow, position/rotation composers, orbital/third-person follow, spline dolly, POV/noise, offsets, group framing, volume settings, etc.). Runtime sync keeps companion cameras aligned to entity data and adds tracking markers for follow/look-at targets.
- **Lights**: Entity components drive `Light` properties plus URP and HDRP additional data, with a sync system handling enable state, intensity, shadows, cookies, and pipeline-specific settings.
- **Terrain and water hooks**: Optional bakers convert Unity terrain colliders into physics collider blobs for ECS, and register HDRP `WaterSurface` components when present.
