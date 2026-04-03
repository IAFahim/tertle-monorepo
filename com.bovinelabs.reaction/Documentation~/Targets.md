# Targets

## Summary

Targets define which entity a condition or action reads from or writes to. Each reaction entity stores a target context (Owner, Source, Target), plus optional custom slots, and the Target enum chooses which one to resolve.

## Implementing

This section is for designers and content authors. Use it to wire reactions to the correct entities.

### Quick setup checklist

1. Add `ReactionAuthoring` to the GameObject or authoring prefab (it adds `TargetsAuthoring`).
2. In `TargetsAuthoring`, set **Owner**, **Source**, and **Target** GameObjects.
3. Leave **Owner** or **Source** empty to default them to the prefab root.
4. Enable **Custom** targets if you need `Custom0` or `Custom1`.
5. Optional: set **Initialize Target** to remap the Target on instantiation (useful for buffs or spawned entities).

### How targets behave

- Each reaction entity stores its own `Targets` data (Owner/Source/Target).
- **Self** resolves to the reaction entity itself.
- **Custom0/Custom1** require `TargetsCustom`; otherwise they resolve to `Entity.Null` and log an error.
- `TargetsCustom` should be treated as write-once during initialization; changing it later is unsupported.
- Actions like `ActionCreate` copy target context to created entities, with the created entity's **Source** set to the reaction entity.

### Target entry cheat sheet

**None**
- Resolves to `Entity.Null`.
- Use only when you intentionally want no target.

**Target**
- The primary target entity for the reaction.
- Usually the thing you hit or the thing being affected.

**Source**
- The instigator of the reaction.
- Often the attacker, caster, or system that raised the event.

**Owner**
- The entity that owns the reaction.
- Defaults to the prefab root if unset.

**Self**
- The reaction entity itself.

**Custom0 / Custom1**
- Extra target slots for special relationships.
- Require **Custom** targets to be enabled in `TargetsAuthoring`.

### Example mapping

**Turret projectile**
- **Target**: the enemy the projectile was shot at.
- **Self**: the projectile entity.
- **Source**: the turret that fired.
- **Owner**: the character that placed the turret.
- **Custom0/Custom1**: nearby allies or environmental objects.

### Common patterns

- **Self buffs**: actions targeting Self or Owner.
- **On-hit effects**: conditions on Target, actions on Target.
- **Source recoil**: actions on Source while conditions read Target.
- **Linked entities**: use Custom0/Custom1 for secondary targets (pet owner, anchor entity).

## Reference (advanced)

### Target components and systems

- `Targets`: stores Owner/Source/Target per reaction entity.
- `TargetsCustom`: optional Custom0/Custom1 references.
- `InitializeTarget`: mapping data to remap Target at initialization.
- `InitializeTargetsSystem`: resolves `InitializeTarget` and updates `Targets.Target` for newly created entities.

### Target enum

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
