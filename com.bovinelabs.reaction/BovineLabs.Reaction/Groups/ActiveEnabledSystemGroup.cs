// <copyright file="ActiveEnabledSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Groups
{
    using BovineLabs.Core;
    using BovineLabs.Reaction.Actions;
    using Unity.Entities;

    /// <summary>
    /// System group that processes reactions becoming active, applying their effects to the game world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system group runs in the <see cref="ReactionSystemGroup"/> after <see cref="ActiveDisabledSystemGroup"/>
    /// on server and local worlds only. It ensures that deactivation effects are processed before
    /// activation effects, preventing conflicts when reactions change state rapidly.
    /// </para>
    /// <para>
    /// The group contains action systems that apply reaction effects:
    /// - <see cref="ActionCreateSystem"/> - Instantiates new entities based on reaction activation
    /// - <see cref="ActionTagSystem"/> - Adds tag components to target entities
    /// - <see cref="ActionEnableableSystem"/> - Enables components on target entities
    /// </para>
    /// <para>
    /// These systems target entities that have just become active (Active=true, ActivePrevious=false),
    /// ensuring that effects are only applied when reactions transition from inactive to active state.
    /// The systems use reference counting to handle multiple reactions affecting the same targets.
    /// </para>
    /// <para>
    /// The execution order after disabled systems ensures that:
    /// - Entities are destroyed before new ones are created
    /// - Tags are removed before new ones are added
    /// - Components are disabled before being re-enabled
    /// This prevents resource conflicts and maintains consistent game state.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal, Worlds.ServerLocal)]
    [UpdateInGroup(typeof(ReactionSystemGroup))]
    [UpdateAfter(typeof(ActiveDisabledSystemGroup))]
    public partial class ActiveEnabledSystemGroup : ComponentSystemGroup
    {
    }
}
