# Architecture

## Summary

Reaction is a condition-driven pipeline that resolves targets, evaluates conditions, computes active state, then applies actions. Ordering is enforced through system groups and explicit update constraints to keep activation, cleanup, and destruction consistent.

## Overview

- Initialization builds condition subscriptions, target mappings, and spawn transforms.
- Runtime evaluation turns condition data into `ConditionAllActive`, then `Active`.
- Action systems apply effects on activation and undo them on deactivation.
- Destroy-time systems disable `Active` and force cleanup before entity removal.
- Most reaction systems run on server/local worlds only.

## System Groups & Execution Order

Execution order is enforced at the group level; intra-group ordering uses UpdateBefore/UpdateAfter attributes on individual systems.

High-level order:
1. DestroySystemGroup (Initialization)
2. InitializeSystemGroup (Begin Simulation)
3. ReactionSystemGroup (After Transform)
   - ConditionsSystemGroup
   - ActiveSystemGroup
   - ActiveDisabledSystemGroup
   - ActiveEnabledSystemGroup

### System group tree (overview)

```text
DestroySystemGroup (Initialization)
├── ConditionDestroySystem                  // Cleans up condition subscriptions and global registrations
├── ActiveDisableOnDestroySystem            // Disables active reactions on entities marked for destruction
└── ActiveDestroyedCleanupSystem            // Ensures cleanup by updating ActiveDisabledSystemGroup

InitializeSystemGroup (Begin Simulation, OrderFirst)
├── InitializeTargetsSystem                 // Initializes target relationships for entity hierarchies
├── ConditionInitializeSystem               // Initializes condition subscriptions and global registry
└── InitializeTransformSystem               // Initializes transform-based targeting data

ReactionSystemGroup (After Transform)
├── ConditionsSystemGroup
│   ├── GlobalConditionsSystemGroup         // Processes global condition subscriptions
│   ├── ConditionWriteEventsGroup
│   │   └── ConditionEventWriteSystem       // Processes incoming events and updates condition states
│   ├── ConditionAllActiveSystem            // Evaluates condition logic (simple AND and composite)
│   └── ConditionEventResetSystem           // Resets event-based conditions after processing
├── ActiveSystemGroup
│   ├── ActivePreviousSystem                // Tracks previous frame's active state (OrderFirst)
│   ├── ActiveSystem                        // Determines final active state
│   ├── ActiveTriggerSystem                 // Resets triggers
│   └── TimerSystemGroup
│       ├── ActiveDurationSystem            // Manages time-limited reactions
│       ├── ActiveCancelSystemGroup
│       │   └── ConditionCancelActiveSystem // Cancels reactions when conditions become false
│       ├── ActiveCancelSystem              // Interrupts running reactions
│       └── ActiveCooldownSystem            // Prevents frequent retriggering
├── ActiveDisabledSystemGroup
│   ├── ActionCreateDeactivatedSystem       // Destroys entities when reactions deactivate
│   ├── ActionEnableableDeactivatedSystem   // Handles component disabling with reference counting
│   └── ActionTagDeactivatedSystem          // Removes tag components with reference counting
└── ActiveEnabledSystemGroup
    ├── ActionCreateSystem                  // Instantiates new entities
    ├── ActionEnableableSystem              // Manages component enabling with reference counting
    └── ActionTagSystem                     // Adds/removes tag components
```

## Critical architectural requirement

ActiveDisabledSystemGroup and ActiveEnabledSystemGroup must remain separate.

1. ActionCreateDeactivatedSystem disables `Active` on child entities during deactivation. If deactivation and activation were combined, activation queries could be built before those child states are updated, leaving nested hierarchies in an inconsistent state.
2. ActiveDestroyedCleanupSystem must manually update only the disabled group during entity destruction. That selective execution is impossible if activation and deactivation are merged.

## Lifecycle and data flow

Initialization:
- ConditionInitializeSystem registers global conditions and creates event subscriptions on target entities.
- InitializeTargetsSystem remaps `Targets.Target` based on `InitializeTarget` data.
- InitializeTransformSystem sets `LocalTransform` based on `InitializeTransform` data.

Runtime:
- Event writers populate `ConditionEvent`; ConditionEventWriteSystem updates `ConditionActive` and `ConditionValues`.
- ConditionAllActiveSystem evaluates condition logic into `ConditionAllActive`.
- ActiveSystem combines conditions, duration, cooldown, and trigger to set `Active`.
- Action systems use `Active` and `ActivePrevious` to apply or undo effects on transitions.

Destruction:
- ConditionDestroySystem removes subscriptions and global registrations.
- ActiveDisableOnDestroySystem disables `Active` so deactivation flows run.
- ActiveDestroyedCleanupSystem forces ActiveDisabledSystemGroup to clean up before destroy.

## Extension points

- New condition types: define a schema, register the condition type, and write a system that feeds values into the condition pipeline.
- New action types: add a buffer element, bake it in authoring, and apply it in ActiveEnabledSystemGroup with cleanup in ActiveDisabledSystemGroup.

## Related docs

- Conditions: `Packages/com.bovinelabs.reaction/Documentation~/Conditions.md`
- Actions: `Packages/com.bovinelabs.reaction/Documentation~/Actions.md`
- Active: `Packages/com.bovinelabs.reaction/Documentation~/Active.md`
- Targets: `Packages/com.bovinelabs.reaction/Documentation~/Targets.md`
