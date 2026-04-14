---
name: bl-core-entity-commands
description: "Use when creating, refactoring, or debugging com.bovinelabs.core IEntityCommands workflows, including shared builder methods across BakerCommands, CommandBufferCommands, CommandBufferParallelCommands, and EntityManagerCommands."
---

# Core Entity Commands Usage

Use this skill for reusable entity builder code that must work across baking, runtime command buffers, jobs, and tests/editor setup.

## Workflow

1. Read `references/entity-commands.md`.
2. Decide whether the logic should be generic over `IEntityCommands` or intentionally tied to one concrete command type.
3. Choose the implementation from execution context first, then write the shared helper.
4. If the builder also owns blob creation, coordinate with the blobs skill after the command-selection rules are clear.

## Routing

- `references/entity-commands.md`: `IEntityCommands`, concrete command implementations, local-entity rules, baker limitations, blob-store handling, and common builder/test patterns.
