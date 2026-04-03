// <copyright file="ClientStates.cs" company="BovineLabs">
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
    public class ClientStates : KSettings<ClientStates, byte>
    {
        public const string Init = "init";
        public const string Quit = "quit";

        protected override IEnumerable<NameValue<byte>> SetReset()
        {
            return new NameValue<byte>[] { new(Init, 0), new(Quit, 1) };
        }
    }
}
