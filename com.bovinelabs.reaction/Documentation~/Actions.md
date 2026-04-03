# Actions

## Summary

Actions are the "do something" part of a reaction. When a reaction becomes active, actions run on their targets; when it deactivates, actions are undone or cleaned up.

## Implementing

This section is for designers and content authors. Use it to build skills, buffs, debuffs, and AI reactions without writing code.

### Quick setup checklist

1. Add `ReactionAuthoring` to the GameObject or authoring prefab that represents the skill/reaction.
2. Add one or more action authoring components: `ActionCreateAuthoring`, `ActionEnableableAuthoring`, `ActionTagAuthoring` (and `ActionTimelineAuthoring` if installed).
3. In each action component, add entries to its list.
4. Choose the target for each entry (who the action affects).
5. Fill the action-specific fields (Definition, Enableable, Tag, or Timeline).
6. Optional: configure duration/cooldown in the **Active** section if the action should be temporary.

### How actions behave

- Actions run while the reaction is active.
- When the reaction deactivates, actions are cleaned up (created entities can be destroyed if flagged, tags are removed, enableables are disabled).
- You can stack multiple actions by adding multiple entries to the action lists.
- Targets decide which entity receives the effect (Self, Owner, Source, Target, Custom0, Custom1).

### Action entry cheat sheet

**Action Create**
- Creates new entities when the reaction becomes active.
- **Definition**: object definition to instantiate.
- **Target**: which entity the created entity should align to.
- **Destroy On Disabled**: if true, destroy the created entity when the reaction deactivates; if false, you own cleanup.
- Use for projectiles, VFX, pickups, and temporary entities.

**Action Enableable**
- Enables an enableable component while the reaction is active, disables it on deactivation.
- **Enableable**: component type from **BovineLabs > Settings > Reaction** in the **Enableables** list.
- **Setup required**: add component types to **BovineLabs > Settings > Reaction** so they appear in the list.
- **Target**: which entity to affect.
- Reference counted so multiple reactions can enable the same component safely.

**Action Tag**
- Adds a zero-size tag component while active, removes it on deactivation.
- **Tag**: zero-size component type from your project.
- **Target**: which entity to affect.
- Tags must be zero-size components (no fields).
- Reference counted so multiple reactions can add/remove the same tag safely.

**Action Timeline** (optional)
- Plays a DOTS Timeline sequence while the reaction is active.
- **Director**: the `PlayableDirector` to drive.
- **Initial Time**: time to start playback from.
- **Disable Timeline On Deactivate**: stop the timeline when the reaction ends.
- **Reset When Active**: if already active, retrigger restarts the timeline; otherwise it ignores new triggers.
- **Bindings**: map DOTS tracks to reaction targets.
- Requires the Timeline extension (`com.bovinelabs.timeline`) and the Reaction.Timeline assemblies.

### Common patterns

- **Hit VFX**: Action Create on Target with Destroy On Disabled enabled.
- **Temporary buff**: Action Enableable on Target with a duration.
- **Status marker**: Action Tag on Target while a condition stays true.
- **Self cast**: Action Tag or Enableable on Self for a channel or stance.

## Reference (advanced)

### Extending actions

Actions are intentionally small and composable. Extend them by adding a new action type and a system that applies it.

#### Add a new action type

1. Create a buffer element data struct that holds your action settings.
2. Add an authoring component to bake entries into that buffer (see existing action authoring components for patterns).
3. Implement a system in `ActiveEnabledSystemGroup` to apply the action while active.
4. If the action needs cleanup, add a companion system in `ActiveDisabledSystemGroup`.
