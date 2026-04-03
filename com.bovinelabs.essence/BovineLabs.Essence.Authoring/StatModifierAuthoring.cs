// <copyright file="StatDefault.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using System;
    using BovineLabs.Essence.Data;

    /// <summary>
    /// Configuration class for defining a default stat value and its modification type.
    /// Used to specify how a particular stat should be initialized or modified, including the stat type, modification method, and value.
    /// </summary>
    [Serializable]
    public class StatModifierAuthoring
    {
        public StatSchemaObject? Stat;
        public StatAuthoringType ModifyType = StatAuthoringType.Added;

        public float Value;

        public StatModifier ToStatModifier()
        {
            if (!this.Stat)
            {
                throw new NullReferenceException("Stat is null");
            }

            return new StatModifier
            {
                Type = this.Stat,
                ModifyType = StatAuthoringUtil.GetModifier(this.ModifyType),
                ValueRaw = StatAuthoringUtil.GetValueRaw(this.ModifyType, this.Value),
            };
        }
    }
}