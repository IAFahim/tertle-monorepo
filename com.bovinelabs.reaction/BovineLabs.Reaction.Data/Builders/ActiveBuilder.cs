// <copyright file="ActiveBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Builders
{
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Reaction.Data.Active;
    using Unity.Entities;

    /// <summary>
    /// Builder for configuring active states with cooldown, duration, and trigger settings for reaction entities.
    /// </summary>
    public struct ActiveBuilder
    {
        private float activeCooldown;
        private float activeDuration;
        private bool activeTrigger;
        private bool withActivateCooldownAfterDuration;
        private bool activeCancellable;

        /// <summary>
        /// Sets the cooldown duration after the reaction activates.
        /// </summary>
        /// <param name="value">The cooldown duration in seconds.</param>
        public void WithActiveCooldown(float value)
        {
            this.activeCooldown = value;
        }

        /// <summary>
        /// Sets how long the reaction remains active once triggered.
        /// </summary>
        /// <param name="value">The active duration in seconds.</param>
        public void WithActiveDuration(float value)
        {
            this.activeDuration = value;
        }

        /// <summary>
        /// Configures whether the reaction requires an additional manual trigger to activate.
        /// </summary>
        /// <param name="value">True to require manual triggering; false for automatic activation.</param>
        public void WithActiveTrigger(bool value)
        {
            this.activeTrigger = value;
        }

        /// <summary>
        /// Configures whether cooldown should start after duration expires instead of immediately on activation.
        /// </summary>
        /// <param name="value">True to start cooldown after duration ends; false for immediate cooldown.</param>
        public void WithActivateCooldownAfterDuration(bool value)
        {
            this.withActivateCooldownAfterDuration = value;
        }

        /// <summary>
        /// Configures whether the active reaction can be cancelled before its duration expires.
        /// </summary>
        /// <param name="value">True to allow cancellation; false otherwise.</param>
        public void WithCancellable(bool value)
        {
            this.activeCancellable = value;
        }

        /// <summary>
        /// Applies the configured active state settings to the specified entity builder.
        /// </summary>
        /// <typeparam name="T">The type of entity command builder.</typeparam>
        /// <param name="builder">The entity builder to apply active settings to.</param>
        public void ApplyTo<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            builder.AddComponent<Active>();
            builder.SetComponentEnabled<Active>(false);

            builder.AddComponent<ActivePrevious>();
            builder.SetComponentEnabled<ActivePrevious>(false);

            if (this.activeTrigger)
            {
                builder.AddComponent<ActiveTrigger>();
                builder.SetComponentEnabled<ActiveTrigger>(false);
            }

            if (this.activeCooldown > 0)
            {
                builder.AddComponent(new ComponentTypeSet(typeof(ActiveCooldown), typeof(ActiveOnCooldown), typeof(ActiveCooldownRemaining)));
                builder.SetComponent(new ActiveCooldown { Value = this.activeCooldown });
                builder.SetComponentEnabled<ActiveOnCooldown>(false);
            }

            if (this.activeDuration > 0)
            {
                builder.AddComponent(new ComponentTypeSet(typeof(ActiveDuration), typeof(ActiveOnDuration), typeof(ActiveDurationRemaining)));
                builder.SetComponent(new ActiveDuration { Value = this.activeDuration });
                builder.SetComponentEnabled<ActiveOnDuration>(false);

                if (this.activeCancellable)
                {
                    builder.AddComponent<ActiveCancel>();
                    builder.SetComponentEnabled<ActiveCancel>(false);
                }
            }

            if (this.withActivateCooldownAfterDuration && this.activeDuration > 0 && this.activeCooldown > 0)
            {
                builder.AddComponent<ActiveCooldownAfterDuration>();
            }
        }
    }
}
