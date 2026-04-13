---
name: bl-core-system-groups
description: "Use when placing, creating, or debugging com.bovinelabs.core system groups and update ordering, including BeforeScene/AfterScene, BeginSimulation, BeforeTransform/AfterTransform, Relevancy, and pause-aware BLSimulationSystemGroup placement."
---

# Core System Groups Usage

Use this skill for choosing `com.bovinelabs.core` system-group placement and debugging update ordering.

## Workflow

1. Read `references/system-groups.md`.
2. Choose the group from data flow and world phase first.
3. Add `UpdateAfter`/`UpdateBefore` only after the correct group is chosen.
4. Re-check pause behavior if the system or group lives under simulation roots.

## Routing

- `references/system-groups.md`: placement rules for `BeforeSceneSystemGroup`, `AfterSceneSystemGroup`, `BeginSimulationSystemGroup`, transform-adjacent groups, `RelevancySystemGroup`, and `BLSimulationSystemGroup`.
