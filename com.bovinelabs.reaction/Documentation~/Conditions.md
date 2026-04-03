# Conditions

## Summary

Conditions are the gatekeepers for reactions. Each reaction evaluates a set of condition bits and only activates when the configured logic resolves true. Conditions can be event-driven (single frame by default) or state-driven (persistent), and can record values for downstream actions. Each reaction supports up to 32 condition entries.

## Implementing

This section is for designers and content authors. Use it to build skills, buffs, debuffs, and AI reactions without writing code.

### Quick setup checklist

1. Add `ReactionAuthoring` to the GameObject or authoring prefab that represents the skill/reaction.
2. Add your actions (what should happen).
3. In **Conditions**, add one or more entries.
4. Pick the condition asset (event, stat, intrinsic, or custom).
5. Choose the target (who to check).
6. Pick an operation and enter a value (if needed).
7. Optional: configure **Chance To Trigger**, **Do Not Reset**, **Cancel Active**, or the **Expression** field.

### How conditions behave

- A reaction only activates when its condition logic resolves true.
- By default, all conditions must be true at the same time (AND logic).
- The list order matters because the **Expression** field uses list indices (0, 1, 2, ...).
- Event conditions are usually true for a single frame, unless **Do Not Reset** is enabled.
- State conditions remain true until their underlying value changes.
- Values can be recorded for actions that need scaling (for example, damage based on hit strength).

### Condition entry cheat sheet

**Condition**
- The thing to check (Event, Stat, Intrinsic, or custom condition asset).
- Events come from **BovineLabs > Settings > Reaction**.
- Stats/Intrinsics come from the Essence schemas.

**Target**
- Which entity is evaluated (Target/Owner/Source/Self/Custom0/Custom1).
- Pick the entity that actually owns the data or raises the event. For example, use Target for the thing you hit,
  Source for the instigator, Owner for the ability owner, and Self for the reaction entity itself.
- Global conditions hide this field because the target is resolved automatically.

**Operation**
- `Any` ignores values and only checks that the condition exists.
- `Equal`, `NotEqual`, `GreaterThan`, `LessThan`, `Between`, etc. use the value(s) below.
- Use `Any` for boolean-style events (OnHit, OnDeath) and numeric operations when the value matters.

**Comparison Mode**
- `Constant`: compare against a fixed number you type in the inspector.
- `Custom`: compare against a value supplied at runtime (for example, Health > Mana).
- If no custom types appear in the dropdown, no installed packages are providing dynamic comparisons.

**Value / Value Min / Value Max**
- Used by the selected `Operation`.
- `Between` uses Min/Max. Others use Value.
- When using `Custom`, the numeric fields are replaced by the custom comparison settings.

**Features**
- `Condition`: the condition contributes to activation logic (it can make the reaction active or inactive).
- `Value`: store the event/state value so actions or systems can read it later.
- `Accumulate`: add each event value into `ConditionValues` instead of replacing it. Use for build-up thresholds and combo
  style triggers. Only valid for event conditions.
- If you only need the value for later actions, use `Value` without `Condition` so it does not gate activation.
- Accumulate clears its stored value when the condition passes, letting you build toward a threshold repeatedly.

**Destroy If Target Destroyed**
- When true, the reaction entity is destroyed if its target goes away.
- Use false only if you will manually reassign targets later.

**Cancel Active**
- If checked and the reaction has a duration, it will stop when this condition becomes false.
- Requires **Duration** in Active and **Cancellable** enabled (conditions will auto-enable Cancellable for you).

### Condition set settings

**Chance To Trigger**
- Probability (0-1) applied only after all conditions are true.
- Use for proc chance or random effects without changing the condition logic itself.

**Do Not Reset**
- Stops automatic reset of event-based conditions.
- Use for one-shot gates (quests, unlocks) where a condition should stick after being met.

### Condition logic expression (optional)

Use the **Expression** field in the **Condition Logic** group when you need more than AND logic:
- Operators: `&` (AND), `|` (OR), `^` (XOR), `!` (NOT)
- Parentheses for grouping
- Indices refer to the condition list order (0-31)
- The list supports up to 32 conditions. Reordering changes the meaning of the indices, so keep the list stable once referenced.

Examples:
- `0 & 1` requires both conditions
- `(0 | 1) & !2` requires 0 or 1, but not 2
- `0 ^ 1 ^ 2` requires an odd number of true conditions

Example mapping:
- Condition 0: OnHit (event)
- Condition 1: Target Is Frozen (state)
- Condition 2: Target Is Burning (state)

Expression: `0 & (1 | 2)` means "OnHit and (Frozen or Burning)".

### Condition types

**Event conditions**
- Use for things that just happened (OnHit, OnDeath, OnCast).
- They usually reset automatically after one frame.
- If you need a new event, add it in **BovineLabs > Settings > Reaction** (or ask a programmer).

**State conditions**
- Use for persistent values like Health, Mana, Stunned, or Stacks.
- They stay true as long as the underlying value matches the comparison.

**Global conditions**
- Use when a condition exists in exactly one place (time-of-day, global alert state).
- The target is chosen automatically; you will not see a Target field.

### Common patterns

- **Hit reaction with threshold**: Event OnHit + Target Health < 50.
- **Proc chance**: Event OnHit + Chance To Trigger 0.2.
- **Build-up trigger**: Event OnHit with `Accumulate`, compare the stored `Value` against a range.
- **Scaling actions**: Enable `Value` so actions can scale with the incoming event amount.

## Reference (advanced)

### Core condition components

- `ConditionActive`: 32-bit mask of condition states.
- `ConditionAllActive`: overall result for the current condition logic.
- `ConditionValues`: stored values for conditions that record data.
- `ConditionComparisonValue`: runtime values for custom comparisons.
- `ConditionComposite`: compiled logic for **Expression** when used.
- `ConditionChance` and `ConditionReset`: optional modifiers for chance and event reset behavior.

### Extending conditions

Conditions are intentionally small and composable. You extend them by adding new schemas, registering a new condition type, and writing a runtime system that feeds values into the condition pipeline.

#### Add a new condition schema and type

1. Inherit from `ConditionSchemaObject`.
2. Return a unique `Key` and a `ConditionType` string.
3. Register the `ConditionType` string in the `ConditionTypes` settings asset (for example add `status = 3`).
4. If the condition is event-driven, set `IsEvent = true`. Otherwise treat it as a state.

Example schema object (state-driven "status" type):
```csharp
[CreateAssetMenu(menuName = "MyGame/Conditions/Status")]
public sealed class StatusSchemaObject : ConditionSchemaObject
{
    [SerializeField]
    private ushort key;

    public override ushort Key => this.key;

    public override string ConditionType => "status";
}
```

Notes:
- Use the `IUID` + `AutoRef` pattern (see Essence's `StatSchemaObject` and `IntrinsicSchemaObject`) if you want keys auto-managed.
- The `ConditionType` string must exist in `ConditionTypes` or baking will fail.

### Event condition flow (how to trigger)

For event-based conditions, you normally use the built-in `ConditionEventObject`:
1. Create a `ConditionEventObject` asset in **BovineLabs > Settings > Reaction**.
2. Add `EventWriterAuthoring` to the entity that will raise events.
3. Write events from code using `ConditionEventWriter`.

Schedule event writes in a job (avoid main-thread triggers). Assuming you already have the publisher entity (the entity with `EventWriterAuthoring`):
```csharp
[BurstCompile]
private struct RaiseEventJob : IJob
{
    public ConditionEventWriter.Lookup EventWriters;
    public Entity Publisher;
    public ConditionKey Key;
    public int Value;

    public void Execute()
    {
        if (this.EventWriters.TryGet(this.Publisher, out var writer))
        {
            writer.Trigger(this.Key, this.Value);
        }
    }
}
```

Event pipeline recap:
1. `EventWriterAuthoring` adds `EventSubscriber` + `ConditionEvent` buffers.
2. `ConditionEventWriter.Trigger` writes to the `ConditionEvent` map and sets `EventsDirty`.
3. `ConditionEventWriteSystem` processes events:
   - Matches subscribers by type/key.
   - Runs `ReactionUtil.EqualityCheck`.
   - Updates `ConditionActive` bits (atomic).
   - Writes/accumulates `ConditionValues`.
   - Clears the event map.
4. `ConditionEventResetSystem` applies `ConditionReset` masks after evaluation.

### State condition flow (how to write values)

State-based conditions should call `ReactionUtil.WriteState` to update condition bits and values.
This helper disallows accumulation on states and performs equality comparisons consistently.

Example pattern (similar to Essence's `ConditionStatWriteSystem` and `ConditionIntrinsicWriteSystem`):
Assumes a `Status` component with an integer value and a `StatusConditionDirty` enableable component.
```csharp
[BurstCompile]
private struct WriteStatusJob : IJobChunk
{
    [NativeDisableParallelForRestriction]
    public ComponentLookup<ConditionActive> ConditionActives;

    [NativeDisableParallelForRestriction]
    public BufferLookup<ConditionValues> ConditionValues;

    [ReadOnly]
    public BufferLookup<ConditionComparisonValue> ConditionComparisonValues;

    public byte ConditionType;

    [ReadOnly]
    public BufferTypeHandle<EventSubscriber> EventSubscriberHandle;

    [ReadOnly]
    public ComponentTypeHandle<Status> StatusHandle;

    public ComponentTypeHandle<StatusConditionDirty> StatusConditionDirtyHandle;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var subscribersAccessor = chunk.GetBufferAccessor(ref this.EventSubscriberHandle);
        var statuses = chunk.GetNativeArray(ref this.StatusHandle);

        var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
        while (enumerator.NextEntityIndex(out var entityIndexInChunk))
        {
            chunk.SetComponentEnabled(ref this.StatusConditionDirtyHandle, entityIndexInChunk, false);

            var subscribers = subscribersAccessor[entityIndexInChunk].AsNativeArrayRO();
            var value = statuses[entityIndexInChunk].Value;

            foreach (var subscriber in subscribers)
            {
                if (subscriber.ConditionType != this.ConditionType)
                {
                    continue;
                }

                ReactionUtil.WriteState(subscriber, value, this.ConditionComparisonValues,
                    this.ConditionActives, this.ConditionValues);
            }
        }
    }
}
```

### Custom comparisons (Essence example)

To allow inspector-driven custom comparisons:
1. Implement `ICustomComparison` and write any supporting data in `Bake`.
2. The authoring UI exposes all `ICustomComparison` types via a dropdown.
3. Your runtime systems must update `ConditionComparisonValue` (or other data used by `ReactionUtil.EqualityCheck`).

Essence ships two comparison types out of the box:
- `StatComparisonMode` (compare against another stat)
- `IntrinsicComparisonMode` (compare against another intrinsic)

Example usage (Health > Mana):
1. Condition: `StatSchemaObject` for Health.
2. Operation: `GreaterThan`.
3. Comparison Mode: `Custom` -> `StatComparisonMode`, select Mana.

At runtime, `EssenceComparisonWriteSystem` updates the `ConditionComparisonValue` buffer so the comparison uses the current stat/intrinsic values. If you implement your own comparison, follow the same pattern: bake a buffer of comparison descriptors and write the live values into `ConditionComparisonValue`. For `Between`, update both the min and max indices.
