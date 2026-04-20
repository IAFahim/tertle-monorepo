// <copyright file="BridgeWorlds.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data
{
    using BovineLabs.Core;
    using Unity.Entities;

    public static class BridgeWorlds
    {
        public const WorldSystemFilterFlags All = WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor | Worlds.Menu;
        public const WorldSystemFilterFlags NoEditor = WorldSystemFilterFlags.Presentation | Worlds.Menu;
    }
}
