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
* Entities Latest 1.4.0-pre.3
* [Core](https://gitlab.com/tertle/com.bovinelabs.core) and the following extensions
  * Entropy
  * Life Cycle
  * Object Definitions
  * Subscene Loading
* UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS define added to Project Settings

## Core Concepts

### Conditions
Conditions are evaluation criteria that determine when actions should trigger. The system supports up to 32 conditions per entity. Conditions can be:
- **Event-based**: React to specific game events
- **State-based**: Evaluate current entity or world state
- **Global**: Apply across multiple entities
- **Targeted**: Evaluate state relative to different target entities

### Complex Boolean Logic (ConditionComposite)
By default, reactions use simple AND logic where all conditions must be true. 
For more complex scenarios, `ConditionComposite` enables advanced boolean expressions using string-based syntax. 
The Authoring includes a built-in parser that converts these expressions into blob assets for runtime evaluation.

Supported operators include AND (&), OR (|), XOR (^), NOT (!), and parentheses for grouping. 
Condition indices (0-31) reference the bit positions in the `ConditionActive` component. 

Examples: `"0 & 1"` requires both conditions, `"(0 | 1) & !2"` requires either condition 0 or 1 but not condition 2, 
and `"0 ^ 1 ^ 2"` requires exactly one of the three conditions to be true.

### Actions
Actions are operations that execute when their conditions are met. Common action types include:
- **ActionCreate**: Instantiate new entities with specific configurations
- **ActionEnableable**: Enable/disable components on target entities
- **ActionTag**: Add/remove tag components

### Active States
The Active system manages the lifecycle of reactions through a sophisticated state evaluation system. The `ActiveSystem` processes 16 different combinations of input components to determine when reactions should be active.

#### Core Active Components
- **Active**: Core component indicating a reaction is currently running
- **ActiveDuration**: Time-limited reactions with automatic cleanup
- **ActiveCooldown**: Prevent reactions from retriggering too frequently
- **ActiveCooldownAfterDuration**: Tag component that modifies cooldown timing - cooldown starts after duration expires instead of immediately on activation
- **ActiveTrigger**: Single-frame activation signals that reset automatically
- **ActiveCancel**: Mechanism to interrupt running reactions

#### ActiveSystem Behavior Matrix

The `ActiveSystem` evaluates entities based on four key components: Condition, Duration, Cooldown, and Trigger. Each combination (2⁴ = 16 cases) has specific behavior logic:

| Case | Components                      | Logic                                                   | Behavior Description                                             |
|------|---------------------------------|---------------------------------------------------------|------------------------------------------------------------------|
| 1    | None                            | Always True                                             | Always active (user-controlled)                                  |
| 2    | Duration                        | Duration OR (NOT Active)                                | Active during duration, resets when expired                      |
| 3    | Cooldown                        | NOT Cooldown                                            | Active when not on cooldown                                      |
| 4    | Duration + Cooldown             | Duration OR (NOT Cooldown)                              | Duration overrides cooldown                                      |
| 5    | Condition                       | Conditions                                              | Active when conditions satisfied                                 |
| 6    | Condition + Duration            | Duration OR Conditions                                  | Either can activate                                              |
| 7    | Condition + Cooldown            | Conditions AND (NOT Cooldown)                           | Conditions required, cooldown blocks                             |
| 8    | Condition + Duration + Cooldown | Duration OR (Conditions AND (NOT Cooldown))             | Duration overrides, conditions blocked by cooldown               |
| 9    | Trigger                         | Trigger                                                 | Active only when triggered (single-frame)                        |
| 10   | Trigger + Duration              | Duration OR Trigger                                     | Trigger can start duration                                       |
| 11   | Trigger + Cooldown              | Trigger AND (NOT Cooldown)                              | Trigger blocked by cooldown                                      |
| 12   | Trigger + Duration + Cooldown   | Duration OR (Trigger AND (NOT Cooldown))                | Duration overrides, trigger blocked by cooldown                  |
| 13   | Trigger + Condition             | Trigger AND Conditions                                  | Both required simultaneously                                     |
| 14   | Trigger + Condition + Duration  | Duration OR (Trigger AND Conditions)                    | Duration overrides, trigger needs conditions                     |
| 15   | Trigger + Condition + Cooldown  | Trigger AND Conditions AND (NOT Cooldown)               | All three conditions required                                    |
| 16   | All Components                  | Duration OR (Trigger AND Conditions AND (NOT Cooldown)) | Duration overrides all, trigger needs conditions and no cooldown |

#### Key Behavioral Patterns

**Duration as Override**: Duration acts as an OR operation - when active, it overrides other conditions and keeps the entity active regardless of cooldown, conditions, or trigger states.

**Cooldown as Blocker**: Cooldown acts as an AND NOT operation - it prevents activation when active, but doesn't affect entities already active through other means (like duration).

**Trigger Reset**: Triggers are automatically reset by `ActiveTriggerSystem` after processing, ensuring single-frame activation. They're intended for momentary activation signals.

**Condition Evaluation**: Conditions are evaluated continuously and can activate/deactivate entities reactively as game state changes.

**Special Duration-Only Behavior**: Entities with only ActiveOnDuration components get special handling by `ActiveTriggerSystem` to ensure proper reactivation cycles when duration expires.

#### Cooldown Timing: ActiveCooldownAfterDuration

The `ActiveCooldownAfterDuration` component provides advanced cooldown timing control for reactions that have both duration and cooldown components.

**Normal Cooldown Behavior**: 
- Cooldown starts immediately when the reaction activates
- While the reaction is running (during duration), it's already on cooldown
- Use case: "Fireball spell has 3-second cooldown starting when cast"

**After-Duration Cooldown Behavior** (with `ActiveCooldownAfterDuration`):
- Cooldown starts only after the duration expires
- The reaction can be retriggered during its duration if conditions allow
- Use case: "Shield buff lasts 10 seconds, then has 30-second cooldown"

**Implementation Details**:
- Requires both `ActiveDuration` > 0 and `ActiveCooldown` > 0
- `ActiveCooldownSystem` uses dual-query architecture:
  - Entities without `ActiveCooldownAfterDuration`: Start cooldown on activation  
  - Entities with `ActiveCooldownAfterDuration`: Start cooldown when `ActiveOnDuration` becomes disabled
- The core 16-case logic in `ActiveSystem` remains unchanged - this feature works within the existing architecture

**Common Use Cases**:
- **Buffs/Debuffs**: Status effects that only go on cooldown after they wear off
- **Channeled Abilities**: Spells with cast time followed by cooldown
- **Burst Windows**: Temporary damage windows followed by recovery periods
- **Resource Systems**: Temporary resource boosts with cooldown after expiration

### Targets
The target system is fundamental to the reaction framework and defines which entities actions affect. Understanding targets is critical for proper reaction implementation.

```csharp
public enum Target : byte
{
    None = 0,      // No target
    Target = 1,    // Primary target entity
    Owner = 2,     // Entity that owns this reaction
    Source = 3,    // Entity that triggered this reaction
    Self = 4,      // The reaction entity itself
    Custom0 = 6,   // Custom target slot 0
    Custom1 = 7,   // Custom target slot 1
}
```

## Quick Start

### 1. Basic Reaction Setup

Create a GameObject and add the ReactionAuthoring component in the inspector. This will automatically add the required LifeCycleAuthoring and TargetsAuthoring dependencies.

### 2. Configure Conditions

In the ReactionAuthoring component:
- Set up conditions that will trigger your reaction
- Configure condition types, targets, and values
- Add event subscriptions if needed

### 3. Define Actions

Configure the actions that will execute:
- Choose action types (Create, Enableable, Tag, etc.)
- Set target entities for each action
- Configure action-specific parameters

### 4. Optional: Add Timing

For time-based reactions:
- Set duration in ActiveAuthoring for temporary effects
- Configure cooldown to prevent rapid retriggering
- Use cancel conditions for early termination

## Understanding Targets

The target system allows reactions to operate on different entities depending on the context. Consider a projectile scenario to understand the relationships:

**Example: Turret Projectile System**
- **Target**: The enemy that the projectile was shot at
- **Self**: The projectile entity itself
- **Source**: The turret that shot the projectile
- **Owner**: The character that placed the turret
- **Custom0/Custom1**: Additional entities (nearby allies, environmental objects, etc.)

### Target Types

**Target**
- The designated target entity (typically from conditions)
- Primary target of the action or effect

**Source**
- The entity that triggered this reaction
- The immediate cause or initiator of the reaction

**Owner**
- The entity that "owns" this reaction entity
- Represents ownership relationships in entity hierarchies

**Self**
- The entity that has the reaction component
- Used for self-affecting actions

**Custom0 & Custom1**
- Custom target slots for complex multi-entity interactions
- Store additional entities for specialized use cases

## Triggering Events

The reaction system primarily responds to events that you trigger from your game systems. Understanding how to trigger events is essential for using reactions effectively.

### Event-Based Conditions

Event-based conditions respond to discrete events that happen in your game (e.g., "OnHit", "OnDeath", "ItemPickup").

**Setup:**
1. Add `EventWriterAuthoring` component to entities that will send events
2. Create condition event objects via **BovineLabs > Settings** window, under **Reaction/Reaction** tab in the **condition events** array (use the + button)
3. Configure reactions to listen for these events

**Triggering Events from Code:**
```csharp
public partial struct MyDamageSystem : ISystem
{
    private ConditionEventWriter.Lookup eventWriterLookup;
    
    public void OnCreate(ref SystemState state)
    {
        this.eventWriterLookup.Create(ref state);
    }
    
    public void OnUpdate(ref SystemState state)
    {
        this.eventWriterLookup.Update(ref state);
        
        // This is just an example of API, pass ConditionEventWriter.Lookup to a job or call state.Dependency.Complete();
        // Trigger an event on an entity
        if (this.eventWriterLookup.TryGet(targetEntity, out var eventWriter))
        {
            eventWriter.Trigger(onHitEventKey, damageAmount);
        }
    }
}
```

**Key Points:**
- Events are identified by `ConditionKey` (configured in project settings)
- Events automatically reset after being processed (typically within one frame)
- Use for reactive behaviors: damage triggers, collision responses, state changes
- Each event should only be triggered once per frame per entity

### Event Configuration

**In ReactionAuthoring:**
1. **Condition Type**: Choose "event"
2. **Key**: Select the specific event (from your created condition event objects)
3. **Target**: Which entity to check the event on
4. **Operation**: Comparison type (Equal, Greater, Less, etc.)
5. **Value**: The value to compare against (useful for damage amounts, etc.)

**Global vs Local Events:**
- **Local**: Events checked on specific entities
- **Global**: Events that can affect multiple entities (configured in global condition systems)

## Action Types

The reaction system provides several built-in action types for different use cases:

### ActionCreate
Creates new entities when conditions are met.

**Use Cases:**
- Spawning projectiles, effects, or pickups
- Creating temporary entities like explosions or particles
- Instantiating complex entity hierarchies

**Configuration:**
- **Object ID**: Reference to the entity definition to create
- **Target**: Where to create the entity (relative to target entity)
- **Destroy On Disabled**: Whether to destroy created entities when the reaction is disabled

### ActionEnableable
Enables or disables components on target entities.

**Use Cases:**
- Toggling component states (enable/disable renderers, colliders, etc.)
- Activating/deactivating gameplay systems
- Managing component-based state changes

**Configuration:**
- **Component Type**: The component type to enable/disable
- **Target**: Which entity to affect
- **Enable**: Whether to enable (true) or disable (false) the component

**Reference Counting:**
ActionEnableable uses reference counting, making it safe for multiple reactions to enable/disable the same component. The component is only actually disabled when all enabling reactions have been deactivated.

**Setup Required:**
Before using ActionEnableable, you must register component types:
1. Open **BovineLabs > Settings** window
2. Navigate to **Reaction/Reaction** tab  
3. In the **reaction enableables** array, click the + button
4. Add your component types that will be enabled/disabled by reactions

### ActionTag
Adds or removes tag components on target entities.

**Use Cases:**
- Adding status effect tags (Burning, Poisoned, Stunned)
- Marking entities with temporary states
- Creating component-based flags for other systems

**Configuration:**
- **Tag Type**: The tag component type to add/remove
- **Target**: Which entity to affect
- **Add**: Whether to add (true) or remove (false) the tag

**Reference Counting:**
ActionTag uses reference counting, making it safe for multiple reactions to add/remove the same tag. The tag is only actually removed when all adding reactions have been deactivated.

## System Groups & Execution Order

Reaction systems are organized across multiple system groups in the frame execution order:

```csharp
DestroySystemGroup (Initialization)
├── ActiveDisableOnDestroySystem            // Disables active reactions on entities marked for destruction
├── ActiveDestroyedCleanupSystem            // Ensures proper cleanup by updating ActiveDisabledSystemGroup
└── ConditionDestroySystem                  // Cleans up condition subscriptions and global registrations

InitializeSystemGroup (Begin Simulation, OrderFirst)
├── ConditionInitializeSystem               // Initializes condition subscriptions and global registry
├── InitializeTargetsSystem                 // Initializes target relationships for entity hierarchies
└── InitializeTransformSystem               // Initializes transform-based targeting data

ReactionSystemGroup (After Transform)
├── ConditionsSystemGroup
│   ├── GlobalConditionsSystemGroup         // Processes global condition subscriptions
│   ├── ConditionAllActiveSystem            // Evaluates condition logic (simple AND and complex boolean)
│   ├── ConditionEventWriteSystem           // Processes incoming events and updates condition states
│   └── ConditionEventResetSystem           // Resets event-based conditions after processing
├── ActiveSystemGroup
│   ├── ActivePreviousSystem                // Tracks previous frame's active state (OrderFirst)
│   ├── ActiveSystem                        // Determines final active state
│   ├── ActiveTriggerSystem                 // Resets triggers
│   └── TimerSystemGroup                    // Groups timer-related systems for better organization
│       ├── ActiveDurationSystem            // Manages time-limited reactions
│       ├── ActiveCancelSystem              // Interrupts running reactions
│       ├── ActiveCancelSystemGroup         // Groups systems that request reaction cancellation
│       │   └── ConditionCancelActiveSystem // Cancels reactions when conditions become false
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

**Critical Architectural Requirement:**

The `ActiveDisabledSystemGroup` and `ActiveEnabledSystemGroup` **must remain separate system groups** - they cannot be combined into a single system with sequential job chains. This separation is required for two technical reasons:

1. **Recursive State Mutation**: `ActionCreateDeactivatedSystem` modifies the `Active` component on child entities during deactivation, recursively propagating through entity hierarchies. If combined with activation in sequential jobs, the activation queries would be built before deactivation completes its child modifications, causing newly-deactivated children to be missed and creating inconsistent state in nested reaction hierarchies.

2. **Selective Execution**: `ActiveDestroyedCleanupSystem` must manually trigger only the disabled group during entity destruction. This selective triggering is impossible if deactivation and activation are combined - the cleanup flow requires running deactivation in isolation.

**Key System Functions:**

- **ActionCreateDeactivatedSystem**: Destroys entities created by reactions when they deactivate, with recursive cleanup for nested hierarchies
- **ActionCreateSystem**: Instantiates new entities using ObjectDefinitionRegistry when reactions activate, with optional cleanup linking
- **ActionEnableableDeactivatedSystem**: Handles component disabling when reactions deactivate, using reference counting for safe multi-source management
- **ActionEnableableSystem**: Manages component enabling/disabling with reference counting to safely handle multiple activation sources
- **ActionTagDeactivatedSystem**: Removes tag components when reactions deactivate, using reference counting to ensure safe cleanup
- **ActionTagSystem**: Adds/removes tag components on target entities with reference counting for safe multi-source tag management
- **ActiveCancelSystem**: Provides mechanism to interrupt running reactions by zeroing duration timers when cancel conditions are met
- **ActiveCooldownSystem**: Prevents spam triggering by tracking cooldown timers and blocking reactivation until cooldown expires
- **ActiveDestroyedCleanupSystem**: Ensures proper cleanup during entity destruction by manually updating the disabled system group
- **ActiveDisableOnDestroySystem**: Disables active reactions on entities marked for destruction to trigger proper cleanup of their effects
- **ActiveDurationSystem**: Manages time-limited reactions by decrementing remaining time and updating duration state components
- **ActivePreviousSystem**: Tracks previous frame's active state to enable change detection for reaction activation and deactivation
- **ActiveSystem**: Combines all input states (conditions, duration, cooldown, triggers) using bitwise operations to determine final active state through 16 different combination cases
- **ActiveTriggerSystem**: Resets trigger components after processing to ensure single-frame activation
- **ConditionAllActiveSystem**: Evaluates condition logic using simple AND operations or complex boolean expressions with AND/OR/XOR/NOT and nested grouping
- **ConditionCancelActiveSystem**: Automatically cancels active duration-based reactions when their required conditions are no longer met, running in ActiveCancelSystemGroup to enable cancellation requests
- **ConditionDestroySystem**: Cleans up condition subscriptions and global condition registrations when entities are destroyed
- **ConditionEventResetSystem**: Resets event-based conditions after processing to prevent accumulation across frames
- **ConditionEventWriteSystem**: Processes events from condition buffers, matches against subscribers, and atomically updates condition bitmasks
- **ConditionInitializeSystem**: Builds global condition registry, sets up event subscriptions, and establishes target relationships during entity initialization
- **InitializeTargetsSystem**: Initializes target relationships and hierarchy connections for entities during initialization
- **InitializeTransformSystem**: Sets up transform-based targeting data for position and rotation-dependent reactions

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
