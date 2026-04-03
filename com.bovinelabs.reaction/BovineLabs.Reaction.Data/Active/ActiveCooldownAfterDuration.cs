// <copyright file="ActiveCooldownAfterDuration.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Active
{
    using Unity.Entities;

    /// <summary>
    /// Tag component that modifies cooldown timing behavior to start cooldown after duration expires instead of immediately on activation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this component is present on an entity that has both <see cref="ActiveDuration"/> and <see cref="ActiveCooldown"/>:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Normal behavior: Cooldown starts immediately when the reaction activates</description></item>
    /// <item><description>With this component: Cooldown starts only after the duration timer expires</description></item>
    /// </list>
    /// <para>
    /// This enables gameplay patterns where:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Buffs/debuffs only go on cooldown after they wear off</description></item>
    /// <item><description>Channeled abilities have cast time followed by cooldown</description></item>
    /// <item><description>Temporary effects can be retriggered during their active period</description></item>
    /// </list>
    /// <para>
    /// Requirements:
    /// - Entity must have both <see cref="ActiveDuration"/> &gt; 0 and <see cref="ActiveCooldown"/> &gt; 0
    /// - The <see cref="ActiveCooldownSystem"/> uses this component to determine which query path to use
    /// </para>
    /// <para>
    /// Implementation: The <see cref="ActiveCooldownSystem"/> processes entities with this component using
    /// a separate query that triggers cooldown when <see cref="ActiveOnDuration"/> becomes disabled.
    /// </para>
    /// </remarks>
    public struct ActiveCooldownAfterDuration : IComponentData
    {
    }
}
