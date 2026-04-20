// <copyright file="CMSplineDollyLookAtTargetBridgeTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && UNITY_SPLINES
namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using NUnit.Framework;
    using Unity.Entities;

    public class CMSplineDollyLookAtTargetBridgeTests
    {
        [Test]
        public void Field_RoundTripsAssignedBridgeEntity()
        {
            var value = new Entity { Index = 5, Version = 7 };
            var bridge = new CMSplineDollyLookAtTargetBridge { Value = value };

            Assert.AreEqual(value, bridge.Value);
        }
    }
}
#endif
