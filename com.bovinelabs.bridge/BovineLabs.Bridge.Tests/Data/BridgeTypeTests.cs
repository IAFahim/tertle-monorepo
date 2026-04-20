// <copyright file="BridgeTypeTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data;
    using NUnit.Framework;

    public class BridgeTypeTests
    {
#if UNITY_URP
        [Test]
        public void Equals_SameFlags_ReturnsTrue()
        {
            var a = new BridgeType { Types = UnityComponentType.Light | UnityComponentType.Volume };
            var b = new BridgeType { Types = UnityComponentType.Light | UnityComponentType.Volume };

            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Equals_DifferentFlags_ReturnsFalse()
        {
            var a = new BridgeType { Types = UnityComponentType.Light };
            var b = new BridgeType { Types = UnityComponentType.Volume };

            Assert.IsFalse(a.Equals(b));
        }
#endif
    }
}
