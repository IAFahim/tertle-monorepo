// <copyright file="ServiceStates.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.States
{
    using System.Collections.Generic;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;

    /// <summary> The client game states. </summary>
    /// <remarks> Note these values represent the index of the flag. </remarks>
    [SettingsGroup("Core")]
    public class ServiceStates : KSettings<ServiceStates, byte>
    {
        public const string Init = "init";
        public const string Game = "game";
        public const string HostGame = "host-game";
        public const string JoinGame = "join-game";
        public const string Dedicated = "dedicated-server";

        protected override IEnumerable<NameValue<byte>> SetReset()
        {
            return new NameValue<byte>[] { new(Init, 0), new(Game, 1), new(HostGame, 2), new(JoinGame, 3), new(Dedicated, 4) };
        }
    }
}
