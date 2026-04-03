# BovineLabs Reaction

Reaction is a powerful framework for developing Unity DOTS applications based on the concept of **Conditions and Actions**. It provides a data-driven approach to creating dynamic gameplay effects, AI behaviors, and complex interactive systems through a modular condition-action paradigm.

Originally inspired by the GDC talk from Aurelie Le Chevalier on [Data-Driven Dynamic Gameplay Effects on For Honor](https://www.youtube.com/watch?v=JgSvuSaXs3E).

For support and discussions, join [Discord](https://discord.gg/RTsw6Cxvw3).

## Key Features

- **Condition-Action Framework**: Create complex behaviors by combining conditions that trigger actions
- **Event-Driven Architecture**: Reactive system that responds to game state changes
- **Multi-Target Support**: Actions can target Self, Owner, Source, Target, or custom entities
- **Cooldown & Duration Management**: Built-in systems for timing-based behaviors
- **Global & Local Conditions**: Support for both entity-specific and world-wide conditions

## Dependencies

* Unity 6
* Entities Latest 2.0.0
* [Core](https://gitlab.com/tertle/com.bovinelabs.core) and the following extensions
  * Entropy
  * Life Cycle
  * Object Definitions
  * Subscene Loading
* UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS define added to Project Settings

## Core Concepts

### Conditions
Conditions decide when actions run. They can be event-based, state-based, or global, and each condition can target a specific entity.
For a full authoring guide (including condition logic expressions and event setup), see [Conditions](Documentation~/Conditions.md).

### Actions
Actions are operations that run while reactions are active and target specific entities. For authoring details and action types, see [Actions](Documentation~/Actions.md).

### Active
The Active system manages the lifecycle of reactions through a state evaluation system that combines conditions, triggers, duration, and cooldown. For authoring details and behavior explanations, see [Active](Documentation~/Active.md).

### Targets
Targets define which entity conditions and actions operate on. For authoring guidance, examples, and the target enum, see [Targets](Documentation~/Targets.md).

### Architecture
For system group ordering, lifecycle, and cleanup rules, see [Architecture](Documentation~/Architecture.md).

## Quick Start

### 1. Basic Reaction Setup

Create a GameObject and add the ReactionAuthoring component in the inspector. This will automatically add the required LifeCycleAuthoring and TargetsAuthoring dependencies.

### 2. Configure Conditions

In the ReactionAuthoring component:
- Set up conditions that will trigger your reaction
- Configure condition types, targets, and values
- Add event subscriptions if needed

### 3. Configure Actions

Add the action authoring components you need and set their targets. See [Actions](Documentation~/Actions.md) for action types and setup.

### 4. Optional: Add Timing

For time-based reactions:
- Set duration in ActiveAuthoring for temporary effects
- Configure cooldown to prevent rapid retriggering
- Use cancel conditions for early termination
- See [Active](Documentation~/Active.md) for detailed behavior

## API Reference

### Key Data Components
- `ConditionData`: Core condition configuration
- `ActionCreate`: Entity creation action
- `Active`: Active reaction marker
- `Target`: Target entity enumeration
- `ConditionValues`: Variable condition values buffer

### Authoring Components
- `ReactionAuthoring`: Main reaction configuration
- `ConditionAuthoring`: Condition setup interface
- `ActiveAuthoring`: Active state configuration
- `TargetsAuthoring`: Target entity management

## Optional Extensions

### Timeline Integration

The reaction package includes optional Timeline integration through a separate custom Timeline package. This is **not required** for basic reaction functionality.

**Dependencies for Timeline Extension:**
- [BovineLabs Timeline](https://gitlab.com/tertle/com.bovinelabs.timeline) (entities version of Unity's Timeline)

**Timeline Features:**
- **Position Tracks**: Animate entity positions over time with PositionClip and PositionStartClip
- **Rotation Tracks**: Control entity rotations with RotationLookAtTargetClip and RotationLookAtStartClip  
- **ActionTimeline**: Execute Unity Timeline sequences as reaction actions
- **Timeline Bindings**: Configure Timeline bindings for position/rotation tracks

The Timeline extension allows for complex animated sequences to be triggered by reactions, providing a bridge between the data-driven reaction system and Unity's visual Timeline authoring tools.

### Timer Customization

Timer systems (`ActiveCooldownSystem` and `ActiveDurationSystem`) support **WriteGroups** for custom tick rate modifications, allowing you to override how cooldown and duration timers count down.

**Use Cases:**
- Haste effects that accelerate cooldown recovery
- Slow effects that extend buff/debuff durations
- Stat-based timer modifications (e.g., cooldown reduction stats)

**Implementation:**
Use WriteGroups on `ActiveCooldownRemaining` and `ActiveDurationRemaining` components to replace default timer behavior with custom systems that modify tick rates based on stats, conditions, or other game state.



