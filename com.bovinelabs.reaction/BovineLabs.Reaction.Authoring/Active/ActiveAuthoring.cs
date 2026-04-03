// <copyright file="ActiveAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Active
{
    using System;
    using BovineLabs.Core.Authoring.EntityCommands;
    using BovineLabs.Reaction.Data.Builders;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring configuration for active state behavior including triggers, cooldowns, and durations.
    /// Controls when and how long a reaction remains active once triggered.
    /// </summary>
    [Serializable]
    public class ActiveAuthoring
    {
        [Tooltip("Add the ActiveTrigger component to require external manual triggering. This trigger will auto reset.")]
        [SerializeField]
        private bool trigger;

        [Tooltip("Cooldown starts as soon as active is triggered, even if it has a duration.")]
        [Min(0)]
        [SerializeField]
        private float cooldown;

        [SerializeField]
        private bool cooldownAfterDuration;

        [Min(0)]
        [SerializeField]
        private float duration;

        [Tooltip("Can the condition")]
        [SerializeField]
        private bool cancellable;

        public bool Trigger
        {
            get => this.trigger;
            set => this.trigger = value;
        }

        public float Cooldown
        {
            get => this.cooldown;
            set => this.cooldown = value;
        }

        public bool ActivateCooldownAfterDuration
        {
            get => this.cooldownAfterDuration;
            set => this.cooldownAfterDuration = value;
        }

        public float Duration
        {
            get => this.duration;
            set => this.duration = value;
        }

        public bool Cancellable
        {
            get => this.cancellable;
            set => this.cancellable = value;
        }

        public void Bake(IBaker baker, Entity entity)
        {
            var builder = default(ActiveBuilder);
            builder.WithActiveCooldown(this.Cooldown);
            builder.WithActiveDuration(this.Duration);
            builder.WithActiveTrigger(this.Trigger);
            builder.WithActivateCooldownAfterDuration(this.ActivateCooldownAfterDuration);
            builder.WithCancellable(this.Cancellable);

            var commands = new BakerCommands(baker, entity);
            builder.ApplyTo(ref commands);
        }
    }
}
