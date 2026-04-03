# Active

## Summary

Active controls when a reaction is running. It combines conditions, triggers, duration, and cooldown into one lifecycle. While a reaction is active, actions run on their targets; when it deactivates, actions are cleaned up.

## Implementing

This section is for designers and content authors. Use it to control how long effects last, how often they can trigger, and whether they can be interrupted.

### Quick setup checklist

1. Add `ReactionAuthoring` to the GameObject or authoring prefab that represents the skill/reaction.
2. In the **Active** section, set **Duration** if the effect should last for a fixed time.
3. Set **Cooldown** if the effect should not retrigger immediately.
4. Toggle **Cooldown After Duration** if the cooldown should start after the duration ends.
5. Enable **Trigger** if you want the reaction to require a manual trigger signal.
6. Enable **Cancellable** if you plan to use cancel conditions that stop the effect early.

### How active behaves

- Actions run while the reaction is active.
- **Duration** keeps a reaction active for a fixed time once it starts.
- **Cooldown** blocks reactivation for a set time after activation (or after duration, if configured).
- **Trigger** requires an external trigger signal; leave it off for automatic condition-driven activation.
- **Cancellable** lets cancel conditions stop an active duration early.

### Active settings cheat sheet

**Trigger**
- Requires an external trigger signal to activate.
- Automatically resets after processing.
- Use for abilities that should only activate on button press or scripted triggers.

**Duration**
- How long the reaction stays active once triggered.
- Use for timed buffs, channels, and temporary effects.

**Cooldown**
- Prevents the reaction from retriggering for a set time.
- Use to rate-limit effects or enforce skill cooldowns.

**Cooldown After Duration**
- Starts the cooldown only after the duration ends.
- Requires both **Duration** and **Cooldown** to be set.

**Cancellable**
- Allows cancel conditions to interrupt an active duration.
- Required for **Cancel Active** condition entries; it is auto-enabled when you use cancel conditions.

### Common patterns

- **Timed buff**: Duration > 0, no trigger, no cooldown.
- **Ability with cooldown**: Cooldown > 0, trigger optional.
- **Channeled ability**: Duration > 0, Cancellable enabled, Cancel Active conditions set.
- **After-duration cooldown**: Duration > 0, Cooldown > 0, Cooldown After Duration enabled.
- **Manual trigger**: Trigger enabled with conditions that must also be true.

## Reference (advanced)

### Core active components

- `Active`: core marker indicating a reaction is running.
- `ActiveDuration`, `ActiveOnDuration`, `ActiveDurationRemaining`: duration configuration and timer state.
- `ActiveCooldown`, `ActiveOnCooldown`, `ActiveCooldownRemaining`: cooldown configuration and timer state.
- `ActiveCooldownAfterDuration`: modifier for cooldown timing.
- `ActiveTrigger`: single-frame activation signal that resets automatically.
- `ActiveCancel`: cancellation request to end active durations.

### Active behavior matrix

The `ActiveSystem` evaluates entities based on four key components: Condition, Duration, Cooldown, and Trigger. Each combination (2^4 = 16 cases) has specific behavior logic:

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

### Key behavioral patterns

**Duration as override**: Duration acts as an OR operation. When active, it overrides other conditions and keeps the entity active regardless of cooldown, conditions, or trigger states.

**Cooldown as blocker**: Cooldown acts as an AND NOT operation. It prevents activation when active, but does not affect entities already active through other means (like duration).

**Trigger reset**: Triggers are automatically reset by `ActiveTriggerSystem` after processing, ensuring single-frame activation. They are intended for momentary activation signals.

**Condition evaluation**: Conditions are evaluated continuously and can activate/deactivate entities reactively as game state changes.

**Special duration-only behavior**: Entities with only `ActiveOnDuration` components get special handling by `ActiveTriggerSystem` to ensure proper reactivation cycles when duration expires.

### Triggering manually

`ActiveTrigger` is an enableable component. To fire it, set it enabled for one frame; it will be reset automatically.

```csharp
// Requires Trigger enabled in the Active settings.
entityManager.SetComponentEnabled<ActiveTrigger>(reactionEntity, true);
```

### Cooldown timing: ActiveCooldownAfterDuration

The `ActiveCooldownAfterDuration` component provides advanced cooldown timing control for reactions that have both duration and cooldown components.

**Normal cooldown behavior:**
- Cooldown starts immediately when the reaction activates.
- While the reaction is running (during duration), it is already on cooldown.
- Use case: "Fireball spell has 3-second cooldown starting when cast."

**After-duration cooldown behavior** (with `ActiveCooldownAfterDuration`):
- Cooldown starts only after the duration expires.
- The reaction can be retriggered during its duration if conditions allow.
- Use case: "Shield buff lasts 10 seconds, then has 30-second cooldown."

**Implementation details:**
- Requires both `ActiveDuration` > 0 and `ActiveCooldown` > 0.
- `ActiveCooldownSystem` uses dual-query architecture:
  - Entities without `ActiveCooldownAfterDuration`: start cooldown on activation.
  - Entities with `ActiveCooldownAfterDuration`: start cooldown when `ActiveOnDuration` becomes disabled.
- The core 16-case logic in `ActiveSystem` remains unchanged. This feature works within the existing architecture.

**Common use cases:**
- **Buffs/Debuffs**: Status effects that only go on cooldown after they wear off.
- **Channeled abilities**: Spells with cast time followed by cooldown.
- **Burst windows**: Temporary damage windows followed by recovery periods.
- **Resource systems**: Temporary resource boosts with cooldown after expiration.
