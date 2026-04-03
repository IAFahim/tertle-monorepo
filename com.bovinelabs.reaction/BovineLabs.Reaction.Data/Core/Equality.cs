// <copyright file="Equality.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Core
{
    using BovineLabs.Reaction.Data.Conditions;

    /// <summary> Enum that defines how Events should be tested for equality. </summary>
    public enum Equality : byte
    {
        /// <summary>
        /// Will pass as long as the event exists.
        /// </summary>
        Any = 0,

        /// <summary>
        /// Will pass as long as the event exists and <see cref="ConditionEvent"/> Value matches the <see cref="EventSubscriber.Value"/>.
        /// </summary>
        Equal = 1,

        /// <summary>
        /// Will pass as long as the event exists and <see cref="ConditionEvent"/> Value doesn't matches the <see cref="EventSubscriber.Value"/>.
        /// </summary>
        NotEqual = 2,

        /// <summary>
        /// Will pass as long as the event exists and <see cref="ConditionEvent"/> Value is greater than the <see cref="EventSubscriber.Value"/>.
        /// </summary>
        GreaterThan = 3,

        /// <summary>
        /// Will pass as long as the event exists and <see cref="ConditionEvent"/> Value is greater than or equal the <see cref="EventSubscriber.Value"/>.
        /// </summary>
        GreaterThanEqual = 4,

        /// <summary>
        /// Will pass as long as the event exists and <see cref="ConditionEvent"/> Value is less than the <see cref="EventSubscriber.Value"/>.
        /// </summary>
        LessThan = 5,

        /// <summary>
        /// Will pass as long as the event exists and <see cref="ConditionEvent"/> Value is less than or equal the <see cref="EventSubscriber.Value"/>.
        /// </summary>
        LessThanEqual = 6,

        /// <summary>
        /// Will pass as long as the event exists and <see cref="ConditionEvent"/> Value is between an inclusive range <see cref="EventSubscriber.Value"/>.
        /// </summary>
        Between = 7,
    }
}
