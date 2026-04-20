// <copyright file="BridgeWorldsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Core;
    using NUnit.Framework;
    using Unity.Entities;

    public class BridgeWorldsTests
    {
        [Test]
        public void All_ContainsPresentationEditorAndMenuFlags()
        {
            var all = BridgeWorlds.All;
            Assert.AreEqual(WorldSystemFilterFlags.Presentation, all & WorldSystemFilterFlags.Presentation);
            Assert.AreEqual(WorldSystemFilterFlags.Editor, all & WorldSystemFilterFlags.Editor);
            Assert.AreEqual(Worlds.Menu, all & Worlds.Menu);
        }

        [Test]
        public void NoEditor_ExcludesEditorFlag()
        {
            var noEditor = BridgeWorlds.NoEditor;
            Assert.AreEqual(WorldSystemFilterFlags.Presentation, noEditor & WorldSystemFilterFlags.Presentation);
            Assert.AreEqual(Worlds.Menu, noEditor & Worlds.Menu);
            Assert.AreEqual(0, (int)(noEditor & WorldSystemFilterFlags.Editor));
        }
    }
}
