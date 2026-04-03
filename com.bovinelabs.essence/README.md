# BovineLabs Essence

Essence is a comprehensive stat system designed for Unity DOTS applications, providing seamless integration with the [Reaction](https://gitlab.com/tertle/com.bovinelabs.reaction) framework. It introduces a robust architecture for managing both **Stats** and **Intrinsics**, enabling data-driven gameplay mechanics with state-based condition support.

For support and discussions, join [Discord](https://discord.gg/RTsw6Cxvw3).

## Key Features

- **Dual Stat System**: Separate Stats (modifiable attributes) and Intrinsics (direct values)
- **Reaction Integration**: State-based conditions that trigger reactions based on stat/intrinsic values
- **Dynamic Modifiers**: Additive, multiplicative, and base modifiers for complex stat calculations
- **Action Support**: ActionStat and ActionIntrinsic for reaction-driven modifications

## Dependencies

* Unity 6
* Entities Latest (1.3.5+)
* [Reaction](https://gitlab.com/tertle/com.bovinelabs.reaction)
* [Core](https://gitlab.com/tertle/com.bovinelabs.core)

Custom inspectors for Stats and Intrinsics require the [BovineLabs Entities fork](https://github.com/tertle/com.unity.entities) (optional but recommended).

## Core Concepts

### Stats vs Intrinsics

Essence distinguishes between two fundamental types of entity attributes:

**Stats**
- **Modifiable attributes** with base values and modifier support
- Examples: Strength, Movement Speed, Max Health, Damage
- Calculated as: `(Base + Sum(Added)) * (1 + Sum(Additive)) * Product(1 + Multiplicative)`
- **Invertible**: Modifiers can be added and removed without data loss
- Support complex modifier stacking from equipment, buffs, and temporary effects
- Use `StatKey` for identification and `StatValue` for storage

**Intrinsics**
- **Direct values** that only increase or decrease
- Examples: Current Health, Mana, Experience Points, Currency
- No modifier calculations - values change directly
- **Permanent changes**: Once modified, the previous value is lost
- Often have min/max ranges defined in schema
- Use `IntrinsicKey` for identification and simple `int` values

### Integration with Reaction

Essence provides **state-based conditions** for the Reaction framework, enabling reactions to trigger based on:
- Stat value thresholds ("When Strength >= 10")
- Intrinsic value changes ("When Health < 25%")
- Stat comparisons ("When Damage > Target's Armor")
- Complex stat-based logic

State conditions automatically update when underlying stat/intrinsic values change, providing reactive gameplay systems.

### Schema System

Both Stats and Intrinsics use a schema-based approach:
- **StatSchemaObject**: Defines stat types, default values, and metadata
- **IntrinsicSchemaObject**: Defines intrinsic types, ranges, and default values
- **Centralized Management**: All definitions in BovineLabs > Settings window
- **Type Safety**: Compile-time checking with StatKey and IntrinsicKey structs

## Dynamic Intrinsic Limits

Essence supports **stat-based intrinsic limits**, where Stats can dynamically control the min/max ranges of Intrinsics. This enables powerful gameplay patterns where calculated stat values determine intrinsic boundaries in real-time.

### Core Concept

Instead of fixed min/max values, intrinsics can reference stats to determine their valid ranges:
- **Min Stat Reference**: Intrinsic minimum is controlled by a stat's value
- **Max Stat Reference**: Intrinsic maximum is controlled by a stat's value  
- **Dynamic Updates**: Limits automatically update when referenced stats change
- **Automatic Clamping**: IntrinsicValidationSystem enforces limits after stat calculations

### Common Use Cases

**RPG Health System:**
```csharp
// Schema Configuration:
StatSchemaObject: "MaxHealth" (calculated stat with equipment/level modifiers)
IntrinsicSchemaObject: "Health" 
  - Min: 0 (fixed)
  - Max: Reference to "MaxHealth" stat
```

## Architecture & Components

### Data Components

**Core Data Structures:**
```csharp
// Stats buffer - dynamic hashmap of StatKey -> StatValue
Stat : IDynamicHashMap<StatKey, StatValue>

// Intrinsics buffer - dynamic hashmap of IntrinsicKey -> int
Intrinsic : IDynamicHashMap<IntrinsicKey, int>

// Stat modifier with type and value
struct StatModifier
{
    StatKey Type;               // Which stat to modify
    StatModifyType ModifyType;  // Added/Additive/Multiplicative
    short Value;                // Modifier value
}
```

**Supporting Components:**
- `StatModifiers`: Buffer of modifiers applied to calculate final stat values
- `StatDefaults`: Base stat values before modifiers
- `IntrinsicConditionDirty`: Marks when intrinsic conditions need updates
- `StatsConditionDirty`: Marks when stat conditions need updates

### Authoring Components

**StatAuthoring**: Main authoring component for entity stat setup
- Configure default stats and intrinsics
- Set up stat groups for reusable configurations
- Control whether stats can be modified at runtime
- Integration with EventWriterAuthoring for condition triggering

**Configuration Options:**
```csharp
public class StatAuthoring : MonoBehaviour
{
    public bool AddStats = true;
    public StatDefault[] StatDefaults;     // Individual stat values
    public StatGroup[] StatDefaultGroups;  // Reusable stat sets
    public bool StatsCanBeModified = true; // Allow runtime modifications
    
    public bool AddIntrinsics = true;
    public IntrinsicDefault[] IntrinsicDefaults;
    public IntrinsicGroup[] IntrinsicDefaultGroups;
}
```

## Quick Start

### 1. Create Stat and Intrinsic Schemas

1. Open **BovineLabs > Settings** window
2. Navigate to **Reaction/Stats** tab
3. Create StatSchemaObject and IntrinsicSchemaObject definitions:
   - **Stat Examples**: Strength, Speed, MaxHealth, Damage
   - **Intrinsic Examples**: Health, Mana, Experience

### 2. Basic Entity Setup

1. Add `StatAuthoring` component to your GameObject
2. Configure default stats and intrinsics in the inspector
3. Optional: Add `EventWriterAuthoring` to enable condition triggering

### 3. Modifying Stats and Intrinsics

**⚠️ IMPORTANT: Proper Modification Methods**

To ensure reactions trigger correctly, always use the appropriate modification methods:

**For Intrinsics (Preferred: Use Actions, Manual: Use IntrinsicWriter):**
```csharp
// ✅ PREFERRED: Use ActionIntrinsic through reactions (automatic)
// Reactions handle intrinsic modifications and trigger properly

// ✅ MANUAL: Use IntrinsicWriter for direct modifications
public partial struct DamageSystem : ISystem
{
    private IntrinsicWriter.Lookup intrinsicWriterLookup;
    
    public void OnCreate(ref SystemState state)
    {
        this.intrinsicWriterLookup.Create(ref state);
    }
    
    public void OnUpdate(ref SystemState state)
    {
        this.intrinsicWriterLookup.Update(ref state);
        
        // Apply damage manually - this triggers conditions and events
        // Note: Prefer ActionIntrinsic through reactions when possible
        var intrinsicWriter = this.intrinsicWriterLookup[targetEntity];
        intrinsicWriter.Subtract(healthKey, damageAmount);
    }
}

// ❌ WRONG: Direct modification won't trigger reactions
var intrinsicsBuffer = EntityManager.GetBuffer<Intrinsic>(entity);
var intrinsicsMap = intrinsicsBuffer.AsMap();
intrinsicsMap[healthKey] = newValue; // Reactions won't trigger!
```

**For Stats (Preferred: Use Actions, Manual: Use StatModifiers):**
```csharp
// ✅ PREFERRED: Use ActionStat through reactions (automatic)
// Reactions handle stat modifications and trigger properly

// ✅ MANUAL: Add modifiers to StatModifiers buffer if needed and mark stats changed
var modifiersBuffer = EntityManager.GetBuffer<StatModifiers>(entity);
modifiersBuffer.Add(new StatModifiers 
{ 
    SourceEntity = sourceEntity,
    Value = new StatModifier 
    { 
        Type = strengthKey, 
        ModifyType = StatModifyType.Added, 
        Value = 10 
    }
});
EntityManager.SetComponentEnabled<StatChanged>(entity, true);

// ❌ WRONG: Never modify Stat buffer directly
var statsBuffer = EntityManager.GetBuffer<Stat>(entity);
statsBuffer.AsMap()[strengthKey] = newValue; // This gets overwritten by calculation!
```

**For Reading Values:**
```csharp
// Access calculated stat values (read-only)
var statsBuffer = EntityManager.GetBuffer<Stat>(entity);
var statsMap = statsBuffer.AsMap();
var strength = statsMap.Get(strengthStatKey).Value;

// Access intrinsic values (read-only for queries)
var intrinsicsBuffer = EntityManager.GetBuffer<Intrinsic>(entity);
var intrinsicsMap = intrinsicsBuffer.AsMap();
var currentHealth = intrinsicsMap.GetOrDefault(healthKey);
```

## State-Based Conditions

Essence enables state-based conditions in the Reaction framework, allowing reactions to trigger based on stat and intrinsic values.

### Stat Conditions

React to calculated stat values (including all modifiers):

**Setup in ReactionAuthoring:**
1. **Condition Type**: Choose "stat"
2. **Key**: Select the specific stat (from your StatSchemaObject definitions)
3. **Target**: Which entity to check the stat on
4. **Operation**: Comparison type (Equal, Greater, Less, GreaterEqual, LessEqual)
5. **Value**: The threshold value to compare against

**Use Cases:**
- "When Strength >= 15, enable special attacks"
- "When Movement Speed < 5, apply slow effect"
- "When Damage > 100, trigger critical hit reaction"

### Intrinsic Conditions

React to direct intrinsic values:

**Setup in ReactionAuthoring:**
1. **Condition Type**: Choose "intrinsic"
2. **Key**: Select the specific intrinsic (from your IntrinsicSchemaObject definitions)
3. **Target**: Which entity to check the intrinsic on
4. **Operation**: Comparison type
5. **Value**: The threshold value

**Use Cases:**
- "When Health <= 25, trigger low health effects"
- "When Mana >= 100, enable powerful spells"
- "When Experience >= 1000, trigger level up"

### Global vs Local Conditions

- **Local Conditions**: Check specific entity's stats/intrinsics
- **Global Conditions**: Check world-wide or cross-entity stat relationships
- Configure through EventSubscriber setup on target entities

## Action Types

Essence provides two primary action types for modifying stats and intrinsics through the Reaction system.

### ActionStat

Modifies stat values on target entities with support for different value calculation types.

**Configuration:**
- **Type**: Which StatKey to modify
- **ModifyType**: How to apply the modification (Added/Additive/Multiplicative)
- **ValueType**: How to calculate the value (Fixed/Linear/Range)
- **Target**: Which entity to affect

**Value Types:**

1. **Fixed**: Static value set at authoring time
   ```csharp
   ValueType = Fixed
   Value = 10  // Always adds exactly 10 to the stat
   ```

2. **Linear**: Remaps a condition value linearly
   ```csharp
   ValueType = Linear
   FromMin = 0, FromMax = 100    // Input range from condition
   ToMin = 5, ToMax = 50         // Output range for stat modification
   ```

3. **Range**: Random value within a range, set on effect creation
   ```csharp
   ValueType = Range
   Min = 5, Max = 15  // Randomly picks value between 5-15 when created
   ```

**Modify Types:**
- **Added**: Flat value additions that sum together before multiplication (`Sum(Added)`)
- **Additive**: Percentage modifiers that sum together (increased/reduced merged, `1 + Sum(Additive)`)
- **Multiplicative**: Multipliers that chain together (more/less merged, `Product(1 + Multiplicative)`)

**Example Use Cases:**
```csharp
// Equipment that adds flat damage (+25 damage)
ActionStat: Damage, Added, Fixed(25)

// Buff that increases speed by 50% (50% increased speed)
ActionStat: Speed, Additive, Fixed(0.5)

// Critical hit multiplier (100% more damage)
ActionStat: Damage, Multiplicative, Fixed(1.0)
```

### ActionIntrinsic

Modifies intrinsic values directly on target entities.

**Configuration:**
- **Type**: Which IntrinsicKey to modify
- **Target**: Which entity to affect
- **ValueType**: How to calculate the modification (Fixed/Linear/Range)
- **Operation**: Add or Set the value

**Operations:**
- **Add**: Adds to current value (`current + modification`)
- **Set**: Sets absolute value (`value = modification`)

**Example Use Cases:**
```csharp
// Healing potion
ActionIntrinsic: Health, Add, Fixed(50)

// Damage over time
ActionIntrinsic: Health, Add, Fixed(-10)

// Full mana restore
ActionIntrinsic: Mana, Set, Fixed(100)

// Experience gain based on enemy level
ActionIntrinsic: Experience, Add, Linear(enemyLevel -> expGain)
```

### Action Integration Examples

**Weapon Enhancement System:**
```csharp
// Base weapon stats
StatDefaults: [Damage(25), CritChance(5)]

// Enchantment reactions
Reaction "Fire Enchant":
  Condition: OnEquip
  Actions: 
    ActionStat(Damage, Added, Fixed(10))
    ActionStat(CritChance, Additive, Fixed(0.2))
```

**Health/Damage System:**
```csharp
Reaction "Take Damage":
  Condition: OnHit event
  Actions:
    ActionIntrinsic(Health, Add, Linear(damageAmount -> -damageAmount))
    
Reaction "Low Health Warning":
  Condition: Health <= 25%
  Actions:
    ActionStat(Speed, Multiplicative, Fixed(0.5))  // Slow when injured
```

## System Groups & Execution Order

Essence systems are organized across multiple system groups in the frame execution order:

```csharp
InitializeSystemGroup (Begin Simulation, OrderFirst)
├── ActionInitializeStatRangeSystem  // Initializes stat ranges for new entities
└── InitializeStatsSystem            // Initializes stats/intrinsics for new entities

ReactionSystemGroup (After Transform)
├── ConditionsSystemGroup
│   └── ConditionWriteEventsGroup
│       ├── ConditionSubscribedSystem      // Manages stat/intrinsic subscriptions
│       ├── ConditionStatWriteSystem           // Updates stat conditions
│       └── ConditionIntrinsicWriteSystem      // Updates intrinsic conditions
├── ActiveSystemGroup                 // Reaction system internals
├── ActiveDisabledSystemGroup
│   └── ActionStatDeactivatedSystem   // Removes stat modifiers
├── ActiveEnabledSystemGroup
│   ├── ActionStatSystem              // Adds stat modifiers
│   └── ActionIntrinsicSystem         // Applies intrinsic changes
└── StatChangedSystemGroup
    ├── StatCalculationSystem         // Calculates final stat values (First)
    ├── IntrinsicValidationSystem     // Enforces stat-based limits (Last)
    └── StatChangedResetSystem        // Resets change flags (Last)
```

**Key System Functions:**

- **InitializeStatsSystem**: Initializes stats and intrinsics for newly created entities with default values
- **ActionInitializeStatRangeSystem**: Sets up stat range configurations for new entities requiring range-based stats
- **ConditionSubscribedSystem**: Manages subscriptions for both stat and intrinsic conditions and tracks which entities need monitoring
- **ConditionStatWriteSystem**: Evaluates stat-based conditions and updates condition states when stat values change
- **ConditionIntrinsicWriteSystem**: Evaluates intrinsic-based conditions and updates condition states when intrinsic values change
- **ActionStatSystem**: Processes ActionStat reactions by adding/removing stat modifiers on target entities
- **ActionStatDeactivatedSystem**: Removes stat modifiers when ActionStat reactions are deactivated or disabled
- **ActionIntrinsicSystem**: Processes ActionIntrinsic reactions by directly modifying intrinsic values on target entities
- **StatCalculationSystem**: Applies all stat modifiers using the formula `(Base + Sum(Added)) * (1 + Sum(Additive)) * Product(1 + Multiplicative)`
- **IntrinsicValidationSystem**: Enforces stat-based intrinsic limits and clamps intrinsic values to their valid ranges
- **StatChangedResetSystem**: Resets dirty flags and change tracking after all stat/intrinsic processing is complete

## Advanced

### Stat-Based Timer Modification

Override reaction cooldown and duration timers using WriteGroups to modify tick rates based on stat values.

**Built-in Implementation:**
- Single stat affects all timers globally
- Positive values accelerate timers (0.1 = 1.1x speed)
- Negative values decelerate timers (-0.1 = 0.9x speed)
- Custom implementations can target specific timer types

**Setup Example:**
```csharp
[WriteGroup(typeof(ActiveCooldownRemaining))]
[WriteGroup(typeof(ActiveDurationRemaining))]
public struct UseStatForTimers : IComponentData { }

[UpdateInGroup(typeof(TimerSystemGroup))]
[UpdateBefore(typeof(ActiveTimerTriggerFilterSystem))]
public partial struct StatActiveDurationSystem : ISystem
{
    private StatTimerEnableable<ActiveOnDuration, ActiveDurationRemaining, Active, ActiveDuration> impl;
    
    // A similar setup can be used for cooldowns
    // private StatTimerEnableable<ActiveOnCooldown, ActiveCooldownRemaining, Active, ActiveCooldown> impl;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var timerSpeedStat = new StatKey { Value = 123 }; // Configure your stat
        this.impl.OnCreate(ref state, timerSpeedStat, ComponentType.ReadOnly<UseStatForTimers>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        this.impl.OnUpdate(ref state);
    }
}
```

## API Reference

### Modification Systems

**IntrinsicWriter (for manual intrinsic modifications):**
```csharp
// Use when you need direct intrinsic modification outside of reactions
// ActionIntrinsic through reactions is preferred when possible
// IntrinsicWriter automatically handles:
// - Min/max clamping based on schema
// - Condition dirty flagging for state-based reactions  
// - Event triggering for event-based reactions
// - Delta calculations for proper change detection

struct IntrinsicWriter
{
    int Add(IntrinsicKey key, int delta);        // Add/subtract with clamping
    int Subtract(IntrinsicKey key, int delta);   // Convenience method for Add(-delta)
    int Set(IntrinsicKey key, int value);        // Set absolute value with clamping
    
    // Access underlying systems if needed
    ref DynamicHashMap<IntrinsicKey, int> Intrinsics;
    ref ConditionEventWriter EventWriter;
}

// Setup in systems
struct IntrinsicWriter.Lookup
{
    void Create(ref SystemState state);
    void Update(ref SystemState state);
    IntrinsicWriter this[Entity entity];
    bool TryGet(Entity entity, out IntrinsicWriter writer);
}
```

**StatModifiers (for manual stat modifications):**
```csharp
// For direct stat modifier manipulation when not using actions
struct StatModifiers : IBufferElementData
{
    Entity SourceEntity;    // What entity added this modifier
    StatModifier Value;     // The actual modifier data
}

// StatModifier automatically gets processed by StatCalculationSystem
struct StatModifier
{
    StatKey Type;           // Which stat to modify
    StatModifyType ModifyType;  // Added/Additive/Multiplicative
    short Value;            // Modifier value (or half for percentages)
}
```