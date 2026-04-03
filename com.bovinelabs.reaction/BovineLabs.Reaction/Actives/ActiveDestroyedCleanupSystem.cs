// <copyright file="ActiveDestroyedCleanupSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actives
{
    using BovineLabs.Core;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Groups;
    using Unity.Entities;

    /// <summary>
    /// Ensures proper cleanup of active reactions during entity destruction by triggering disabled system processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="DestroySystemGroup"/> on server and local worlds only, updating after
    /// <see cref="ActiveDisableOnDestroySystem"/> to ensure proper reaction cleanup sequencing during entity destruction.
    /// </para>
    /// <para>
    /// The system performs the following cleanup sequence:
    /// 1. <see cref="ActiveDisableOnDestroySystem"/> first disables all <see cref="Active"/> components on entities marked for destruction
    /// 2. This system then manually updates the <see cref="ActiveDisabledSystemGroup"/> to process the disabled reactions
    /// 3. All action systems (create, tag, enableable) reverse their effects before the entities are destroyed
    /// </para>
    /// <para>
    /// This ensures that entities created by reactions, tags added by reactions, and components enabled by reactions
    /// are all properly cleaned up before the parent entity is destroyed, preventing orphaned entities and
    /// inconsistent game state.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal)]
    [UpdateAfter(typeof(ActiveDisableOnDestroySystem))]
    [UpdateInGroup(typeof(DestroySystemGroup))]
    public sealed partial class ActiveDestroyedCleanupSystem : SystemBase
    {
        private ActiveDisabledSystemGroup activeDisabledGroup;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.activeDisabledGroup = this.World.GetExistingSystemManaged<ActiveDisabledSystemGroup>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            this.activeDisabledGroup.Update();
        }
    }
}
