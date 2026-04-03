// <copyright file="ActiveDisableOnDestroySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Actives
{
    using BovineLabs.Core;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Reaction.Data.Active;
    using Unity.Burst;
    using Unity.Entities;

    /// <summary>
    /// Disables active reactions on entities marked for destruction to trigger proper cleanup of their effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This system runs in the <see cref="DestroySystemGroup"/> on server and local worlds only, processing
    /// entities that have both the <see cref="DestroyEntity"/> and <see cref="Active"/> components.
    /// </para>
    /// <para>
    /// The system performs a critical cleanup step by disabling the <see cref="Active"/> component on entities
    /// about to be destroyed. This triggers the normal reaction deactivation process, ensuring that:
    /// - Created entities are properly destroyed
    /// - Added tag components are removed
    /// - Enabled components are disabled
    /// - All other reaction effects are reversed
    /// </para>
    /// <para>
    /// This system works in conjunction with <see cref="ActiveDestroyedCleanupSystem"/> which manually
    /// updates the disabled system group to process these changes before entity destruction completes.
    /// Without this system, destroyed entities with active reactions would leave orphaned effects in the world.
    /// </para>
    /// </remarks>
    [WorldSystemFilter(Worlds.ServerLocal)]
    [UpdateInGroup(typeof(DestroySystemGroup))]
    public partial struct ActiveDisableOnDestroySystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ActiveDisableOnDestroyJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(DestroyEntity))]
        private partial struct ActiveDisableOnDestroyJob : IJobEntity
        {
            private static void Execute(EnabledRefRW<Active> active)
            {
                active.ValueRW = false;
            }
        }
    }
}
