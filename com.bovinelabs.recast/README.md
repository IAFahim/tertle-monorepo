# BovineLabs Recast

HPC# port of [Recast](https://github.com/recastnavigation/recastnavigation) for Unity using Burst, Collections, and Mathematics.

Note that 'DetourCrowd' and 'DetourTileCache' are not currently included in this package.

This is just the port and you will need to supply your own integration. For Unity Entities integration, see [Traverse](https://github.com/tertle/com.bovinelabs.traverse).

## Intentional Port Improvements

This package intentionally diverges from upstream Recast/Detour in a few places where the Unity/HPC# version benefits from a different default or a more ergonomic API.

- `DtPolyRef` and `DtTileRef` default to 64-bit refs instead of upstream's default 32-bit mode.
- Several raw C tuple/pointer surfaces are exposed as stronger typed helpers such as `DtPolyRef`, `DtTileRef`, `DtNavMeshData`, `ushort3`, and `byte4`.
- Major Recast/Detour objects carry explicit Unity allocator ownership, and several build APIs collapse allocator-failure return channels where allocation failure is treated as fatal in this port.
- The query surface adds Unity-oriented conveniences such as explicit `DtNavMeshQuery` allocator lifecycle, `ReplaceNavMeshTarget`, `ref Unity.Mathematics.Random` random queries, generic polygon query callbacks, public `GetPortalPoints`, and `UnsafeList` overloads.
- `FindStraightPath` intentionally uses a small portal-scaled epsilon in the funnel corner tests to reduce near-collinear corner churn in live movement instead of matching upstream's exact zero-threshold comparisons.
- `DtNavMesh` intentionally stitches off-mesh links to the tile resolved from `DtOffMeshConnection.EndPos`, so long-range portals can connect across non-adjacent tiles once both endpoint tiles are loaded instead of being limited to same-tile or immediate-neighbor stitching.
- Recast carrier structs such as `RcHeightfield`, `RcCompactHeightfield`, `RcContourSet`, and `RcPolyMeshDetail` are runtime ownership-aware C# representations rather than byte-for-byte native mirrors.

For support and discussions, join the [Discord](https://discord.gg/RTsw6Cxvw3) community.

If you want to support my work or get access to a few private libraries, [Buy Me a Coffee](https://buymeacoffee.com/bovinelabs).
