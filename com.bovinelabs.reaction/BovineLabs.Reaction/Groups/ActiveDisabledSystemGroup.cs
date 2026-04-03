// <copyright file="ActiveDisabledSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Groups
{
    using BovineLabs.Core;
    using BovineLabs.Reaction.Actions;
    using BovineLabs.Reaction.Actives;
    using Unity.Entities;

    /// <summary>
    /// System group that processes reactions becoming inactive, reversing their effects on the game world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs in the <see cref="ReactionSystemGroup"/> after <see cref="ActiveSystemGroup"/>
    /// on server and local worlds only. It processes the cleanup and reversal of reaction effects
    /// when reactions transition from active to inactive state.
    /// </para>
    /// <para>
    /// The group contains action systems that reverse reaction effects:
    /// - <see cref="ActionCreateDeactivatedSystem"/> - Destroys entities created by reactions
    /// - <see cref="ActionTagDeactivatedSystem"/> - Removes tag components from target entities
    /// - <see cref="ActionEnableableDeactivatedSystem"/> - Disables components on target entities
    /// </para>
    /// <para>
    /// These systems target entities that were previously active but are now inactive
    /// (Active=false, ActivePrevious=true), ensuring that cleanup only occurs during
    /// active-to-inactive transitions. The systems use reference counting to prevent
    /// premature removal when multiple reactions affect the same targets.
    /// </para>
    /// <para>
    /// This group can also be manually triggered by <see cref="ActiveDestroyedCleanupSystem"/>
    /// during entity destruction to ensure proper cleanup of reaction effects before
    /// entities are removed from the world.
    /// </para>
    /// <para>
    /// <b>Critical: Why Disabled and Enabled Must Be Separate System Groups</b>
    /// </para>
    /// <para>
    /// This system group MUST remain separate from <see cref="ActiveEnabledSystemGroup"/> for two technical reasons:
    /// </para>
    /// <para>
    /// 1. <b>Recursive State Mutation</b>: <see cref="ActionCreateDeactivatedSystem"/> modifies the <see cref="Active"/>
    /// component on child entities during deactivation (line 111: SetComponentEnabled(entity, false)). This happens recursively
    /// through entity hierarchies. If deactivation and activation systems ran in the same system group with sequential job chains,
    /// the activation queries would be built before the deactivation jobs finish modifying child entity states, causing those
    /// newly-deactivated children to be missed by the activation query, resulting in inconsistent state for nested reaction hierarchies.
    /// </para>
    /// <para>
    /// 2. <b>Selective Execution</b>: <see cref="ActiveDestroyedCleanupSystem"/> needs to manually trigger ONLY
    /// the disabled system group during entity destruction cleanup. This selective triggering is impossible if deactivation
    /// and activation are combined into a single system - the cleanup flow requires running deactivation in isolation.
    /// </para>
    /// <para>
    /// These groups use separate update phases to ensure deactivation effects fully complete and propagate through
    /// entity hierarchies before any activation effects begin, preventing race conditions in nested reaction systems.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
    [UpdateInGroup(typeof(ReactionSystemGroup))]
    [UpdateAfter(typeof(ActiveSystemGroup))]
    public partial class ActiveDisabledSystemGroup : ComponentSystemGroup
    {
    }
}
