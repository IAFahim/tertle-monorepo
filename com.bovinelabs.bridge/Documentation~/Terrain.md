# Terrain and Water

## Summary
Bridge provides authoring for Unity Terrain colliders to Unity Physics, and HDRP WaterSurface registration.

## Requirements
- Unity Physics and Unity Terrain for terrain collider baking.
- HDRP for WaterSurface.

## Terrain Collider Authoring
1. Add `TerrainAuthoring` to a GameObject with `UnityEngine.TerrainCollider` in a SubScene.
2. Configure `CollisionMethod` and `SmallestOffset`.
3. Bake the SubScene.

Bridge creates a Unity Physics `TerrainCollider` blob and adds `PhysicsCollider`, `PhysicsWorldIndex`, and a `PhysicsColliderKeyEntityPair` buffer.

## Water Surface Authoring
- Add `WaterSurface` in a SubScene. Bridge registers it as a managed component so it can be referenced by ECS and other systems.

## Notes
- `SmallestOffset` avoids flat terrain collision issues by adding a tiny alternating height offset. Adjust it if your terrain resolution differs.
