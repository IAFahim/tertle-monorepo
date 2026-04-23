# Changelog

## [1.0.6] - 2026-04-23

### Changed
* `DtNavMesh` now stitches off-mesh links to the exact destination tile resolved from `DtOffMeshConnection.EndPos`, allowing long-range portals across non-adjacent tiles while preserving the existing traversal/off-mesh metadata model
* Documented the intentional difference from upstream's same-tile or immediate-neighbor off-mesh stitching behavior in the README

### Fixed
* Fixed stale or missing long-range off-mesh stitching during tile add, late destination-tile load, and destination-tile remove/re-add flows
* Reserved detour tile link capacity for both start-side and remote landing-side off-mesh attachments so long-range links can be connected safely

## [1.0.5] - 2026-03-31

### Changed
* Synced recent upstream recast library fixes for invalid `AddSpan` inputs and detail-mesh refinement with empty triangulations

### Fixed
* Restored upstream-compatible planar finite checks so `IsFinite2D` and `GetPolyHeight` only validate X/Z
* Restored upstream `MoveAlongSurface` search-radius math
* Restored upstream nearest-poly-in-tile traversal and scoring used by off-mesh link anchoring
* Restored upstream external off-mesh link wiring, endpoint snapping, and bidirectional-link gating
* Restored upstream portal-segment range checks in `FindPolysAroundShape`
* Restored upstream closed-list validation in `GetPathFromDijkstraSearch`
* Restored upstream shared-edge reachability checks in `FindDistanceToWall`
* Restored upstream tile-state serialization layout, tile-ref validation, and size reporting
* Restored `RcSpan` field order to match the native `rcSpan` layout
* Restored `BuildDistanceField` cleanup of an existing distance buffer before rebuilding
* Restored upstream `DtNodePool` acceptance of exact upper-bound `maxNodes` values
* Fixed the dormant `DT_POLYREF32` path in `DtNavMesh` so it uses valid bit helpers and throws on invalid 32-bit ref configurations instead of relying on stale status-return code

### Changed
* Documented the remaining intentional port-owned divergences and movement-oriented `FindStraightPath` epsilon behavior in the README

## [1.0.4] - 2026-03-26

### Fixed
* Fixed inaccurate detail mesh height calculations when interior detail samples are added

### Changed
* Cleaned up some documentation

## [1.0.3] - 2026-02-18

### Added
* FindStraightPath pointer overload

### Fixed
* Fixed inaccurate detail mesh height calculations when interior detail samples are added

### Changed
* DisableAutoCreation added to test assembly

## [1.0.2] - 2025-12-23

### Added
* ReplaceNavMeshTarget to DtNavMeshQuery to allow re-use for different NavMesh

## [1.0.1] - 2025-12-05

### Added
* Recast test suite

### Changed
* Allocator.Persistant is no longer used internally when building the NavMesh for temp structures
* Internalized Math and Common functions

### Fixed
* Obscure bug with degenerate triangles due to a difference in math.clamp and rcClamp when min/max are inverted

## [1.0.0] - 2025-12-03

### Added
* Initial release
