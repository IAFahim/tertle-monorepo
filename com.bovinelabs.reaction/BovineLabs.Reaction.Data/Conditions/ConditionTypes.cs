// <copyright file="ConditionTypes.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using System.Collections.Generic;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;

    /// <summary>
    /// Settings class defining the available condition types (event, stat, intrinsic) for the reaction system.
    /// </summary>
    [SettingsGroup("Reaction")]
    public class ConditionTypes : KSettings<ConditionTypes, byte>
    {
        public const string EventType = "event";
        public const string StatType = "stat";
        public const string IntrinsicType = "intrinsic";

        /// <inheritdoc />
        protected override IEnumerable<NameValue<byte>> SetReset()
        {
            return new[] { new NameValue<byte>(EventType, 0), new NameValue<byte>(StatType, 1), new NameValue<byte>(IntrinsicType, 2) };
        }
    }
}